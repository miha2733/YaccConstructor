﻿module Yard.Generators.GLL.Parser 
open Yard.Generators.GLL 
open System 
open System.Collections.Generic
open Yard.Generators.GLL
open Yard.Generators.Common.ASTGLL
open Yard.Generators.Common.DataStructures
open Microsoft.FSharp.Collections

[<Measure>] type vertexMeasure
[<Measure>] type nodeMeasure
[<Measure>] type labelMeasure

let inline packExtension left right : int64<extension> =  LanguagePrimitives.Int64WithMeasure ((int64 left <<< 32) ||| int64 right)
let inline getRightExtension (long : int64<extension>) = int <| ((int64 long) &&& 0xffffffffL)
let inline getLeftExtension (long : int64<extension>)  = int <| ((int64 long) >>> 32)

let inline packVertex level label: int64<vertexMeasure> =  LanguagePrimitives.Int64WithMeasure ((int64 level <<< 32) ||| int64 label)
let inline getIndex1Vertex (long : int64<vertexMeasure>)       = int <| ((int64 long) &&& 0xffffffffL)
let inline getIndex2Vertex (long : int64<vertexMeasure>)       = int <| ((int64 long) >>> 32)
 

let inline packLabel rule position = ((int rule <<< 16) ||| int position)*1<labelMeasure>
let inline getRule (packedValue : int<labelMeasure>)  = int packedValue >>> 16
let inline getPosition (packedValue : int<labelMeasure>) = int (int packedValue &&& 0xffff)

[<Struct>]
type Vertex =
    val Level            : int
    val Label            : int<labelMeasure>
    new (level, label) = {Level = level; Label = label}

[<Struct>]
type Context =
    val Index         : int
    val Label         : int<labelMeasure>
    val Vertex        : Vertex
    val Ast           : int<nodeMeasure>
    new (index, label, vertex, ast) = {Index = index; Label = label; Vertex = vertex; Ast = ast}

type ParseResult<'TokenType> =
    | Success of Tree<'TokenType>
    | Error of string

[<Struct>]
type LblNodePair =
    val lbl: int<labelMeasure>
    val node: int<nodeMeasure>
    new (l,n) = {lbl=l; node=n}


let buildAst<'TokenType> (parser : ParserSourceGLL<'TokenType>) (tokens : seq<'TokenType>) : ParseResult<_>  = 
    let tokens = seq {yield! tokens; yield parser.EOF}
    let tokens = Seq.toArray tokens
    let inputLength = Seq.length tokens
    let nonTermsCountLimit = 1 + (Array.max parser.LeftSide)

    if inputLength = 0 || parser.IndexEOF = parser.TokenToNumber tokens.[0] then
      //  if parser.AcceptEmptyInput then
      //      let eps = new Nonnte
            //Success (new Tree<_>(null, getEpsilon startNonTerm, null))
     //   else
            Error ("This grammar does not accept empty input.")     
    else
        let slots = parser.Slots
        let setR = new Queue<Context>()   
        let setP = new Dictionary<(int*int<labelMeasure>), ResizeArray<int<nodeMeasure>>> ()
        //свернуть в 1 инт
        let setU = Array.zeroCreate<Dictionary<int<labelMeasure>, Dictionary<int64<vertexMeasure>, ResizeArray<int<nodeMeasure>>>>> (inputLength + 1)

        let currentIndex = ref 0
        let currentrule = parser.StartRule
        let currentLabel = ref <| packLabel currentrule 0
        let dummyGSSNode = new Vertex(!currentIndex, !currentLabel)
        let dummyAST = new TerminalNode(-1, packExtension -1 -1)
        let dummy = 0<nodeMeasure>
        let currentN = ref <| dummy
        let currentR = ref <| dummy

        let m = ref 0
        let resultAST = ref None
        let packedNodesReadCount = ref 0
        let packedNodesWriteCount = ref 0
        let packedNodesX = Array.zeroCreate<int> (inputLength + 1)
        let packedNodesY = Array.zeroCreate<int> (inputLength + 1)
        let packedNodesZ = Array.zeroCreate<int> (inputLength + 1)
        let packedNodes = Array.zeroCreate<Dictionary<int, Dictionary<int, ResizibleUsualOne<LblNodePair>>>> (inputLength + 1)

        let nonTerminalNodesReadCount = ref 0
        let nonTerminalNodesWriteCount = ref 0 
        let nonterminalNodesX = Array.zeroCreate parser.NonTermCount
        let nonterminalNodesY = Array.zeroCreate (inputLength + 1)
        let nonterminalNodesZ = Array.zeroCreate (inputLength + 1)        
        let nonTerminalNodes = Array.zeroCreate<Dictionary<int, Dictionary<int, int<nodeMeasure>>>> parser.NonTermCount

        let intermidiateNodesReadCount = ref 0
        let intermidiateNodesWriteCount = ref 0
        //we can use dictionary <extension, dict>
        let intermidiateNodes = new Dictionary<int, Dictionary<int, Dictionary<int<labelMeasure>, int<nodeMeasure>>>>()
 
 //посчитать размерв коллекций
        let edgesReadCount = ref 0
        let edgesWriteCount = ref 0
        let edges = Array2D.zeroCreate<Dictionary<int<nodeMeasure>, Dictionary<int<labelMeasure>, ResizeArray<int>>>> slots.Count (inputLength + 1)
        
        let terminalNodes = new BlockResizeArray<int<nodeMeasure>>()
        let epsilonNode = new TerminalNode(-1, packExtension 0 0)
        let sppfNodes = new BlockResizeArray<INode>()
        sppfNodes.Add(dummyAST)
        sppfNodes.Add(epsilonNode)


        let currentGSSNode = ref <| dummyGSSNode
        let currentContext = ref <| new Context(!currentIndex, !currentLabel, !currentGSSNode, dummy)
        
        let finalExtension = packExtension 0 (inputLength)

        let containsContext index (label : int<labelMeasure>) (vertex : Vertex) (ast : int<nodeMeasure>) =
            if index <= inputLength
            then
                let vertexKey = packVertex vertex.Level vertex.Label
                if setU.[index] <> Unchecked.defaultof<Dictionary<int<labelMeasure>, Dictionary<int64<vertexMeasure>, ResizeArray<int<nodeMeasure>>>>>
                then
                    let cond, current = setU.[index].TryGetValue(label) 
                    if  cond then
                       if current.ContainsKey(vertexKey) then
                            let trees = current.[vertexKey]
                            if not <| trees.Contains(ast)
                            then 
                                trees.Add(ast)
                                false
                            else
                                true
                        else 
                            let arr = new ResizeArray<int<nodeMeasure>>()
                            arr.Add(ast)
                            current.Add(vertexKey, arr)                    
                            false
                    else 
                        let dict = new Dictionary<int64<vertexMeasure>, ResizeArray<int<nodeMeasure>>>()
                        setU.[index].Add(label, dict)
                        let arr = new ResizeArray<int<nodeMeasure>>()
                        arr.Add(ast)
                        dict.Add(vertexKey, arr) 
                        false
                else 
                    let dict1 =  new Dictionary<int<labelMeasure>, Dictionary<int64<vertexMeasure>, ResizeArray<int<nodeMeasure>>>>()
                    setU.[index] <- dict1
                    let dict2 = new Dictionary<int64<vertexMeasure>, ResizeArray<int<nodeMeasure>>>()
                    dict1.Add(label, dict2)
                    let arr = new ResizeArray<int<nodeMeasure>>()
                    arr.Add(ast)
                    dict2.Add(vertexKey, arr)
                    false
            else true
                      

        let inline addContext index (label : int<labelMeasure>) vertex ast =
            if not <| containsContext index label vertex ast
            then
                setR.Enqueue(new Context(index, label, vertex, ast))

        let inline slotIsEnd (label : int<labelMeasure>) =
            (getPosition label) = Array.length (parser.rules.[getRule label])

        let findSppfNode label lExt rExt : int<nodeMeasure> =
            let isEnd = slotIsEnd label
            let nTerm = parser.LeftSide.[getRule label]
            if isEnd
            then
                let cur = nonTerminalNodes.[nTerm]
                if  cur <> Unchecked.defaultof<_> //<Dictionary<int64<extension>, int<nodeMeasure>>>
                then
                        let contains , d1 = cur.TryGetValue(lExt)
                        if contains
                        then
                            let contains, d2 = d1.TryGetValue(rExt)
                            if contains
                            then
                                d2
                            else
                                let newNode = new NonTerminalNode(nTerm, packExtension lExt rExt)
                                sppfNodes.Add(newNode)
                                let num = (sppfNodes.Count - 1)*1<nodeMeasure>
                                d1.Add(rExt, num)
                                num
                        else
                            let d2 = new Dictionary<int, int<nodeMeasure>>()
                            //let d1 = new Dictionary<int, Dictionary<int, int<nodeMeasure>>>()
                            let newNode = new NonTerminalNode(nTerm, packExtension lExt rExt)
                            sppfNodes.Add(newNode)
                            let num = (sppfNodes.Count - 1)*1<nodeMeasure>
                            d2.Add(rExt, num)
                            cur.Add(lExt, d2)
                            num
                        
                else
                    //let d = new Dictionary<int64<extension>, int<nodeMeasure>>()
                    let d2 = new Dictionary<int, int<nodeMeasure>>()
                    let d1 = new Dictionary<int, Dictionary<int, int<nodeMeasure>>>()
                    let newNode = new NonTerminalNode(nTerm, packExtension lExt rExt)
                    sppfNodes.Add(newNode)
                    let num = (sppfNodes.Count - 1)*1<nodeMeasure>
                    //d.Add(ext, num)
                    d2.Add(rExt, num)
                    d1.Add(lExt, d2)
                     
                    nonTerminalNodes.[nTerm] <- d1 
                    num
            else
                let contains, d1 = intermidiateNodes.TryGetValue lExt
                if contains
                then
                    let contains, d2 = d1.TryGetValue rExt
                    if contains
                    then
                        let contains, d3 = d2.TryGetValue(label)
                        if contains
                        then
                            d3
                        else
                            let newNode = new IntermidiateNode(int label, packExtension lExt rExt)
                            sppfNodes.Add(newNode)
                            let num = (sppfNodes.Count - 1)*1<nodeMeasure>
                            d2.Add(label, num)
                            num

                    else
                        let d2 = new Dictionary<int<labelMeasure>, int<nodeMeasure>>()
                        let newNode = new IntermidiateNode(int label, packExtension lExt rExt)
                        sppfNodes.Add(newNode)
                        let num = (sppfNodes.Count - 1)*1<nodeMeasure>
                        d2.Add(label, num)
                        d1.Add(rExt,d2)
                        num  
                else
                    let d1 = new Dictionary<int, Dictionary<int<labelMeasure>, int<nodeMeasure>>>()
                    let d2 = new Dictionary<int<labelMeasure>, int<nodeMeasure>>()
                    let newNode = new IntermidiateNode(int label, packExtension lExt rExt)
                    sppfNodes.Add(newNode)
                    let num = (sppfNodes.Count - 1)*1<nodeMeasure>
                    d2.Add(label, num)
                    d1.Add(rExt, d2)
                    intermidiateNodes.Add(lExt, d1)
                    num

        let findSppfPackedNode symbolNode label leftExtension rightExtension (left : INode) (right : INode) : int<nodeMeasure> = 
            let rule = getRule label
            let createNode () =
                let newNode = new PackedNode(rule, left, right)
                sppfNodes.Add(newNode)
                let num = (sppfNodes.Count - 1 )*1<nodeMeasure>
                match (sppfNodes.Item (int symbolNode)) with
                | :? NonTerminalNode as n ->
                    n.AddChild newNode
                | :? IntermidiateNode as i ->
                    i.AddChild newNode
                | _ -> ()
                num
            let i = getLeftExtension leftExtension
            let j = getRightExtension leftExtension
            let k = getRightExtension rightExtension
            
            let d =
                let d1 = packedNodes.[i]
                if d1 <> Unchecked.defaultof<_>
                then
                    let contains, d2 = d1.TryGetValue j
                    if contains
                    then
                        let contains, d3 = d2.TryGetValue k
                        if contains
                        then                            
                            match d3.TryFind (fun x -> x.lbl = label) with
                            | Some num -> num.node
                            | None -> 
                                let newNode = createNode()
                                d3.Add(new LblNodePair(label,newNode))
                                newNode
                        else
                            let newNode = createNode()
                            let d3 = new ResizibleUsualOne<_>(new LblNodePair(label,newNode))
                            d2.Add(k, d3)
                            newNode
                    else
                        let d2 = new Dictionary<int, ResizibleUsualOne<_>>()
                        let newNode = createNode()
                        let d3 = new ResizibleUsualOne<_>(new LblNodePair(label,newNode))
                        d2.Add(k, d3)
                        d1.Add(j, d2)
                        newNode
                else
                    let d1 = new Dictionary<int, Dictionary<int, ResizibleUsualOne<_>>>()
                    let d2 = new Dictionary<int, ResizibleUsualOne<_>>()
                    let newNode = createNode()
                    let d3 = new ResizibleUsualOne<_>(new LblNodePair(label,newNode))
                    d2.Add(k, d3)
                    d1.Add(j, d2)
                    packedNodes.[i] <- d1
                    newNode
            d
                
        let getNodeP (label : int<labelMeasure>) (left : int<nodeMeasure>) (right : int<nodeMeasure>) : int<nodeMeasure> =
            let currentRight = sppfNodes.Item (int right)
            let rightExt = 
                match currentRight with                    
                    | :? NonTerminalNode as nonTerm ->
                        nonTerm.Extension
                    | :? IntermidiateNode as interm ->
                        interm.Extension
                    | :? TerminalNode as term ->
                        term.Extension   
                    | _ -> failwith "Smth strange, Nastya"             
            if left <> dummy
            then
                let currentLeft = sppfNodes.Item (int left)
                let leftExt =
                    match currentLeft with                    
                    | :? NonTerminalNode as nonTerm ->
                        nonTerm.Extension
                    | :? IntermidiateNode as interm ->
                        interm.Extension
                    | :? TerminalNode as term ->
                        term.Extension 
                    | _ -> failwith "Smth strange, Nastya" 
                    
                let y = findSppfNode label (getLeftExtension leftExt) (getRightExtension rightExt)
                ignore <| findSppfPackedNode y label leftExt rightExt currentLeft currentRight
                y
            else
                let y = findSppfNode label (getLeftExtension rightExt) (getRightExtension rightExt)
                ignore <| findSppfPackedNode y label rightExt rightExt dummyAST currentRight 
                y
            
                
            
        let getNodeT index =
            if terminalNodes.Item index <> Unchecked.defaultof<int<nodeMeasure>>
            then
                terminalNodes.Item index
            else
                let t = new TerminalNode(index, packExtension index (index + 1))
                sppfNodes.Add t
                let res = sppfNodes.Count - 1
                terminalNodes.Set index ((sppfNodes.Count - 1)*1<nodeMeasure>)
                res * 1<nodeMeasure>
            
                     
        let containsEdge (b : Vertex) (e : Vertex) ast =
            let labelN = slots.[int b.Label]
            edgesReadCount := !edgesReadCount + 1
            let dict1 = edges.[labelN, b.Level]
            if dict1 <> Unchecked.defaultof<Dictionary<int<nodeMeasure>, Dictionary<int<labelMeasure>, ResizeArray<int>>>>
            then
                if dict1.ContainsKey(ast)
                then
                    let dict2 = dict1.[ast]
                    if dict2.ContainsKey(e.Label)
                    then
                        let t = dict2.[e.Label]
                        if t.Contains(e.Level) then true
                        else 
                            t.Add(e.Level) 
                            false
                    else
                        let arr = new ResizeArray<int>()
                        arr.Add(e.Level) 
                        dict2.Add(e.Label, arr)
                        false
                else
                    let d = new Dictionary<int<labelMeasure>, ResizeArray<int>>()
                    dict1.Add(ast, d)
                    let l = new ResizeArray<int>()
                    l.Add(e.Level)
                    d.Add(e.Label, l)
                    false
            else
                let newDict1 = new Dictionary<int<nodeMeasure>, Dictionary<int<labelMeasure>, ResizeArray<int>>>()
                let newDict2 = new Dictionary<int<labelMeasure>, ResizeArray<int>>()
                let newArr = new ResizeArray<int>()
                newArr.Add(e.Level)
                newDict2.Add(e.Label, newArr)
                newDict1.Add(ast, newDict2)
                edgesWriteCount := !edgesWriteCount + 1
                edges.[labelN, b.Level] <- newDict1
                false                    
        
        let getTreeExtension (node : int<nodeMeasure>) =
            match sppfNodes.Item (int node) with
                | :? TerminalNode as t ->
                    t.Extension
                | :? IntermidiateNode as i ->
                    i.Extension
                | :? NonTerminalNode as n ->
                    n.Extension
                | _ -> failwith "Bad type for tree node"

        
        let create index label (vertex : Vertex) ast = 
            let v = new Vertex(index, label)
            let vertexKey = (index, label)
            let temp = containsEdge v vertex ast
            if not <| temp //containsEdge v vertex ast
            then
                if setP.ContainsKey(vertexKey)
                then
                    let arr = setP.[vertexKey]
                    for tree in arr do
                        let y = getNodeP label ast tree
                        let index = getRightExtension <| getTreeExtension y 
                        addContext index label vertex y 
            v

        let pop (u : Vertex) (i : int) (z : int<nodeMeasure>) =
            if u <> dummyGSSNode
            then
                let vertexKey = (u.Level, u.Label)
                if setP.ContainsKey vertexKey
                then
                    setP.[vertexKey].Add(z)
                else
                    let newList = new ResizeArray<int<nodeMeasure>>()
                    newList.Add(z)
                    setP.Add(vertexKey, newList)
                let outEdges = edges.[slots.[ int u.Label], u.Level]
                edgesReadCount := !edgesReadCount + 1
                for edge in outEdges do
                    let sppfNodeOnEdge = edge.Key
                    for slotLevels in edge.Value do   
                         let slot = slotLevels.Key
                         for level in slotLevels.Value do
                            let resTree = getNodeP u.Label sppfNodeOnEdge z 
                            let newVertex = new Vertex(level, slot)
                            addContext i u.Label newVertex resTree

        let table = parser.Table
        
        let condition = ref false 
        let stop = ref false

        let rec dispatcher () =
            if setR.Count <> 0
            then
                currentContext := setR.Dequeue()
                currentIndex := currentContext.Value.Index
                currentGSSNode := currentContext.Value.Vertex
                currentLabel := currentContext.Value.Label
                let tempRule = getRule !currentLabel
                let tempPos = getPosition !currentLabel
                currentN := currentContext.Value.Ast 
                currentR := dummy
                condition := false
            else 
                stop := true  
                              
        and processing () =  
            condition := true
            let rule = getRule !currentLabel
            let position = getPosition !currentLabel
            if Array.length parser.rules.[rule] = 0 
            then
                let t = new TerminalNode(-1, packExtension !currentIndex !currentIndex)
                sppfNodes.Add t
                let res = sppfNodes.Count - 1
                currentR := res * 1<nodeMeasure>
                currentN := getNodeP !currentLabel !currentN !currentR  
                pop !currentGSSNode !currentIndex !currentN
            else
                if Array.length parser.rules.[rule] <> position
                then
                    let curSymbol = parser.rules.[rule].[position]
                    if !currentIndex < inputLength 
                    then
                        let curToken = parser.TokenToNumber tokens.[!currentIndex]
                        
                        if (parser.NumIsTerminal curSymbol || parser.NumIsLiteral curSymbol)
                        then
                            if curSymbol = curToken 
                            then
                                if !currentN = dummy
                                then currentN := getNodeT !currentIndex
                                else currentR := getNodeT !currentIndex
                                currentIndex := !currentIndex + 1
                                currentLabel := packLabel (rule) ((position) + 1)
                                if !currentR <> dummy
                                then 
                                    currentN := getNodeP !currentLabel !currentN !currentR
                                condition := false
                        else 
                            let getIndex nTerm term = 
                                let mutable index = nTerm
                                index <- (index * (parser.IndexatorFullCount - parser.NonTermCount))
                                index <- index + term - parser.NonTermCount
                                index

                            let index = getIndex curSymbol curToken
                            currentGSSNode := create !currentIndex (packLabel (rule) (position + 1)) !currentGSSNode  !currentN
                            if Array.length table.[index] <> 0 
                            then
                                let a rule = 
                                    let newLabel = packLabel rule 0
                                    addContext !currentIndex newLabel !currentGSSNode dummy 
                                table.[index] |>  Array.iter a
                    else
                        if parser.CanInferEpsilon.[curSymbol]
                        then
                            let curToken = parser.IndexEOF
                            let getIndex nTerm term = 
                                let mutable index = nTerm
                                index <- (index * (parser.IndexatorFullCount - parser.NonTermCount))
                                index <- index + term - parser.NonTermCount
                                index
                            let index = getIndex curSymbol curToken
                            currentGSSNode := create !currentIndex (packLabel (rule) (position + 1)) !currentGSSNode  !currentN
                            if Array.length table.[index] <> 0 
                            then
                                let a rule = 
                                    let newLabel = packLabel rule 0
                                    addContext !currentIndex newLabel !currentGSSNode dummy 
                                table.[index] |>  Array.iter a
                            condition := true
                        condition := true
                                    
                else
                    let curRight =  sppfNodes.Item <| int !currentN 
                    match curRight with
                        | :? TerminalNode as t ->
                            currentN := getNodeP !currentLabel !currentR !currentN
                            let r = (sppfNodes.Item <| int !currentN) :?> NonTerminalNode 
                            pop !currentGSSNode !currentIndex !currentN
                        | :? NonTerminalNode as r ->
                            if (r.Name = parser.LeftSide.[parser.StartRule]) && r.Extension = finalExtension
                            then 
                                resultAST := Some r 
                            pop !currentGSSNode !currentIndex !currentN

        let control () =
             while not !stop do
                if !condition then dispatcher() else processing()
        control()
          
//        let tempPrintArr (arr : array<int>) (name : string) (out : System.IO.StreamWriter) = 
//            out.WriteLine(name)
//           
//            for i = 0 to arr.Length - 1 do
//                if i % 20 = 0 then out.WriteLine()
//                out.Write(arr.[i].ToString() + " ")
//            out.WriteLine()
//            out.WriteLine("------------------------------------------")

        match !resultAST with
            | None -> Error ("String was not parsed")
            | Some res -> 
                    let r1 = new Tree<_> (tokens, res, parser.rules)
                    let path = @"../../../src/GLLApplication/out.txt"
                    printfn "!!!!!"
                    //packedNodes |> Array.iter(fun d -> d |> Seq.iter (fun kvp -> kvp.Value |> Seq.iter (fun kvp -> kvp.Value.Count |> printf "%A; " )))
//                    let out = new System.IO.StreamWriter (path) 
//                    out.WriteLine("packed nodes read count = " + (!packedNodesReadCount).ToString())
//                    out.WriteLine("packed nodes write count = " + (!packedNodesWriteCount).ToString()) 
//                    tempPrintArr packedNodesX "packed nodes X count" out 
//                    tempPrintArr packedNodesY "packed nodes Y count" out 
//                    tempPrintArr packedNodesZ "packed nodes Z count" out 
//
//                    out.WriteLine("nonterminal nodes read count = " + (!nonTerminalNodesReadCount).ToString())
//                    out.WriteLine("nonterminal nodes write count = " + (!nonTerminalNodesWriteCount).ToString())
//                    tempPrintArr nonterminalNodesX "nonterminal nodes X count"  out
//                    tempPrintArr nonterminalNodesY "nonterminal nodes Y count" out
//                    tempPrintArr nonterminalNodesZ "nonterminal nodes Z count" out
//
//                    out.WriteLine("intermidiate nodes read count = " + (!intermidiateNodesReadCount).ToString())
//                    out.WriteLine("intermidiate nodes write count = " + (!intermidiateNodesWriteCount).ToString())
//
//                    out.WriteLine("edges nodes read count = " + (!edgesReadCount).ToString())
//                    out.WriteLine("edges nodes write count = " + (!edgesWriteCount).ToString())
//                    out.Close()
                    Success (r1)   
                        