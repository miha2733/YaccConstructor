﻿module YC.GLL.GSS

open Yard.Generators.GLL.ParserCommon
open Yard.Generators.Common.DataStructures
open AbstractAnalysis.Common
open QuickGraph
open QuickGraph.Graphviz
open System.Collections.Generic.Customized

[<Struct>]
type PoppedData =
    val posInInput: int<positionInInput>
    val data: ParseData

    new (pos, d) = {posInInput = pos; data = d}

[<Measure>] type compressedPosInInputAndGrammar

type PopSet () = 
    let setP = new ResizeArray<PoppedData>() 

    member this.SetP = setP
    member this.Add x = 
        if (setP.Contains x) |> not
        then 
            setP.Add x
            true
        else
            false

[<AllowNullLiteral>]
type GSSVertex (nonterm: int<positionInGrammar>, posInInput: int<positionInInput>) =
    let setU = new System.Collections.Generic.Dictionary<int64<compressedPosInInputAndGrammar>,HashSet<ParseData>>()
    let setP = new PopSet() 
    
    override this.Equals y =
        y :? GSSVertex 
        && (let y = y :?> GSSVertex 
            this.Nonterm = y.Nonterm
            && this.PositionInInput = y.PositionInInput)

    override this.GetHashCode() = hash (this.Nonterm, this.PositionInInput)

    member this.U = setU
    member this.P with get () = setP
    member this.AddP d = 
        setP.Add d
    member this.PositionInInput = posInInput
    member this.Nonterm = nonterm
    member this.GetUncompressetPositions (compressedPos: int64<compressedPosInInputAndGrammar>) =
        (CommonFuns.getLeft (int64 compressedPos)) * 1<positionInInput>
        , (CommonFuns.getRight (int64 compressedPos)) * 1<positionInGrammar>

    /// Checks for existing of context in SetU. If not adds it to SetU.
    member this.ContainsContext (posInInput: int<positionInInput>) (posInGrammar : int<positionInGrammar>) (newData : ParseData)=
        let compressedPositions =
            CommonFuns.pack posInInput posInGrammar 
            |> FSharp.Core.LanguagePrimitives.Int64WithMeasure  
        let cond, data = setU.TryGetValue compressedPositions
        if cond
        then not <| data.Add newData
        else 
            setU.Add(compressedPositions, new HashSet<_>([|newData|]))
            false
    override this.ToString () = sprintf "Nonterm: %i, Index: %i" this.Nonterm this.PositionInInput

[<AllowNullLiteral>]
type GSSVertexCF (nonterm: int<positionInGrammar>, posInInput: int<positionInInput>, vertex: GSSVertex) =
    inherit GSSVertex (nonterm, posInInput)
    
    member this.Vertex = vertex
    override this.Equals y =
        y :? GSSVertexCF 
           && (let y = y :?> GSSVertexCF 
               this.Nonterm = y.Nonterm
               && this.PositionInInput = y.PositionInInput
               && this.Vertex = y.Vertex)
    override this.GetHashCode() = hash (this.Nonterm, this.PositionInInput, this.Vertex)
    override this.ToString () = 
        let vertex = this.Vertex
        let vertexStr = 
            if vertex = null 
            then "null" 
            else sprintf "(N: %i, I: %i)" vertex.Nonterm vertex.PositionInInput
        sprintf 
            "Nonterm: %i, Index: %i, Vertex: %s" 
            this.Nonterm 
            this.PositionInInput
            vertexStr

type GSSVertexInstanceHolder() =
    let instanceHolder = new System.Collections.Generic.Dictionary<_,_>()

    member this.Get(nonterm: int<positionInGrammar>, posInInput: int<positionInInput>) =
        let newInst = new GSSVertex(nonterm, posInInput) 
        let packed = (int nonterm <<< 16) ||| int posInInput
        let cond, value = instanceHolder.TryGetValue(packed)
        if cond
        then
            value
        else
            instanceHolder.Add(packed,newInst)
            newInst

type GSSVertexCFInstanceHolder() =
    let instanceHolder = new System.Collections.Generic.Dictionary<_,_>()

    member this.Get(nonterm: int<positionInGrammar>, posInInput: int<positionInInput>, vertex: GSSVertex) = 
        let newInst = new GSSVertexCF(nonterm, posInInput, vertex) 
//        let hashed = hash(nonterm,posInInput,vertex)
        let cond, value = instanceHolder.TryGetValue(newInst)
        if cond
        then
            value
        else 
            let newInst = new GSSVertexCF(nonterm, posInInput, vertex)
            instanceHolder.Add(newInst, newInst)
            newInst

[<Struct>]
type GSSEdgeLbl =
    val StateToContinue: int<positionInGrammar>
    val Data: ParseData
    new (stateToContinue, len) = {StateToContinue = stateToContinue; Data = len}

type GSS () =
    inherit AdjacencyGraph<GSSVertex, TaggedEdge<GSSVertex, GSSEdgeLbl>>(true)
    /// Checks for existing of edge in gss edges set. If not adds it to edges set.
    member this.ContainsVertexAndEdge (startVertex:GSSVertex, endVertex:GSSVertex, stateToContinue : int<positionInGrammar>, data : ParseData) =
        let cond, edges = this.TryGetOutEdges startVertex
        let edges = 
            if cond
            then
                edges
                |> Seq.tryFind (fun e -> e.Target = endVertex && e.Tag.Data = data && e.Tag.StateToContinue = stateToContinue)               
            else None
        if not cond
        then
            this.AddVertex(startVertex) |> ignore
        if edges.IsNone
        then 
            this.AddEdge(new QuickGraph.TaggedEdge<_,_>(startVertex, endVertex, new GSSEdgeLbl(stateToContinue, data))) |> ignore
        cond, edges.IsSome

    member this.ToDot fileName =
        let getStrFromVertex (v: GSSVertex) = 
            let edgeOfInput = CommonFuns.getEdge v.PositionInInput
            let posOnEdgeOfInput = CommonFuns.getPosOnEdge v.PositionInInput
            sprintf "St:%i;Edg:%i;Pos:%i" v.Nonterm edgeOfInput posOnEdgeOfInput

        let printer = GraphvizAlgorithm(this)
        printer.CommonVertexFormat.Shape <- Dot.GraphvizVertexShape.Ellipse
        printer.FormatEdge.Add(fun (e:FormatEdgeEventArgs<_,_>) -> e.EdgeFormatter.Label.Value <- sprintf "ContSt:%i,Data:%A" e.Edge.Tag.StateToContinue e.Edge.Tag.Data)
        printer.FormatVertex.Add(fun (v:FormatVertexEventArgs<_>) -> v.VertexFormatter.Label <- getStrFromVertex v.Vertex)  
        let str = printer.Generate()        
            
        System.IO.File.WriteAllText(fileName, str)


