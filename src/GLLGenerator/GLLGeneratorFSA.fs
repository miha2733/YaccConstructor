﻿namespace Yard.Generators.GLL

open Mono.Addins
open Yard.Core
open IL
open Constraints
open Yard.Generators.Common
open InitialConvert
open Yard.Generators.Common.FSAGrammar
open Yard.Generators.GLL
open PrintTable
open Yard.Generators.GLL.TranslatorPrinter2
open Option


[<assembly:Addin>]
[<assembly:AddinDependency ("YaccConstructor", "1.0")>]
do()
[<Extension>]
type GLLFSA() = 
    inherit Generator()
        override this.Name = "GLLGeneratorFSA"
        override this.Constraints = [|noMeta; noBrackets; singleModule|]
        override this.Generate (definition, args) =
            let start = System.DateTime.Now
            let args = args.Split([|' ';'\t';'\n';'\r'|]) |> Array.filter ((<>) "")
            let pairs = Array.zeroCreate <| args.Length / 2
            for i = 0 to pairs.Length - 1 do
                pairs.[i] <- args.[i * 2], args.[i * 2 + 1]
            let getOption name either f =
                match definition.options.TryFind name with
                | Some v -> f v
                | None -> either
            let getBoolOption opt either =
                getOption opt either <| function
                    | "true" -> true
                    | "false" -> false
                    | x -> failwithf "Option %s expected values true or false, but %s found." opt x
            let mapFromType t = Map.ofList ["_", Some t]
            let mutable moduleName = getOption "module" "" id

            let mutable tokenType = getOption "token" definition.tokens mapFromType
            let mutable fullPath = getBoolOption "fullpath" false
            let mutable positionType = getOption "pos" "" id
            let mutable needTranslate = getBoolOption "translate" false
            let mutable light = getBoolOption "light" true
            let mutable printInfiniteEpsilonPath = getOption "infEpsPath" "" id
            let mutable isAbstract = getBoolOption "abstract" true
            let withoutTree = ref <| getBoolOption "withoutTree" true
            //let mutable caseSensitive = getBoolOption "caseSensitive" true
            let mutable output =
                let fstVal = getOption "output" (definition.info.fileName + ".fs") id
                getOption "o" fstVal id
            let getBoolValue name = function
                    | "true" -> true
                    | "false" -> false
                    | value -> failwithf "Unexpected %s value %s" name value

            for opt, value in pairs do
                match opt with
                | "-module" -> moduleName <- value
                | "-token" -> tokenType <- mapFromType value
                | "-pos" -> positionType <- value
                | "-o" -> if value.Trim() <> "" then output <- value
                | "-output" -> if value.Trim() <> "" then output <- value
                //| "-caseSensitive" -> caseSensitive <- getBoolValue "caseSensitive" value
                | "-fullpath" -> fullPath <- getBoolValue "fullPath" value
                | "-translate" -> needTranslate <- getBoolValue "translate" value
                | "-light" -> light <- getBoolValue "light" value
                | "-infEpsPath" -> printInfiniteEpsilonPath <- value
                | "-abstract" -> isAbstract <- getBoolValue "abstract" value
                | "-withoutTree" -> withoutTree := getBoolValue "withoutTree" value
                | value -> failwithf "Unexpected %s option" value
                 
            //let newDefinition = initialConvert definition
            let grammar = new FSAGrammar(definition.grammar.[0].rules)

            
            (*
            use out = new System.IO.StreamWriter (output)
            let res = new System.Text.StringBuilder()
            let dummyPos = char 0
            let println (x : 'a) =
                Printf.kprintf (fun s -> res.Append(s).Append "\n" |> ignore) x
            //let print (x : 'a) =
            //    Printf.kprintf (fun s -> res.Append(s) |> ignore) x
            let _class  =
                match moduleName with
                | "" -> if isAbstract then "AbstractParse" else "Parse"
                | s when s.Contains "." -> s.Split '.' |> Array.rev |> (fun a -> String.concat "." a.[1..])
                | s -> s
  
            let printHeaders moduleName fullPath light (output: string) isAbstract =
                let n = output.Substring(0, output.IndexOf("."))
                let mName = 
                    if isAbstract then
                        "GLL.AbstractParse." + n
                    else
                        "GLL.Parse"

                let fsHeaders() = 
                    println "%s" <| getPosFromSource fullPath dummyPos (defaultSource output)
                    println "module %s"
                    <|  match moduleName with
                        | "" -> mName
                        | s -> s
                    if not light then
                        println "#light \"off\""
                    println "#nowarn \"64\";; // From fsyacc: turn off warnings that type variables used in production annotations are instantiated to concrete type"
                    if isAbstract
                    then
                        //println "open Yard.Generators.GLL.AbstractParser"
                        println "open AbstractAnalysis.Common"
                    else
                        if !withoutTree
                        then
                            //println "open Yard.Generators.GLL.AbstractParserWithoutTree"
                            println "open AbstractAnalysis.Common"
                        else
                            println "open Yard.Generators.GLL.Parser"
                    println "open Yard.Generators.GLL"
                    println "open Yard.Generators.Common.ASTGLL"
                    println "open Yard.Generators.GLL.ParserCommon"

                    match definition.head with
                    | None -> ()
                    | Some (s : Source.t) ->
                        println "%s" <| getPosFromSource fullPath dummyPos s
                        println "%s" <| s.text + getPosFromSource fullPath dummyPos (defaultSource output)
                
                fsHeaders()

            printHeaders moduleName fullPath light output isAbstract
            let table = printTableGLL grammar table moduleName tokenType res _class positionType caseSensitive isAbstract !withoutTree
            let res = table
            let res = 
                match definition.foot with
                | None -> res
                | Some (s : Source.t) ->
                    res + (getPosFromSource fullPath dummyPos s + "\n"
                                + s.text + getPosFromSource fullPath dummyPos (defaultSource output) + "\n")
            let res =
                let init = res.Replace("\r\n", "\n")
                let curLine = ref 1// Must be 2, but there are (maybe) some problems with F# compiler, causing to incorrect warning
                let res = new System.Text.StringBuilder(init.Length * 2)
                for c in init do
                    match c with
                    | '\n' -> incr curLine; res.Append System.Environment.NewLine
                    | c when c = dummyPos -> res.Append (string !curLine)
                    | x -> res.Append x
                    |> ignore
                res.ToString()
            out.WriteLine res
            out.Flush()
            out.Close()
            eprintfn "Generation time: %A" <| System.DateTime.Now - start
            *)
            box ()
        override this.Generate definition = this.Generate (definition, "")