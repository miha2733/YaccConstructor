
# 2 "Expr.yrd.fs"
module GLL.ParseExpr
#nowarn "64";; // From fsyacc: turn off warnings that type variables used in production annotations are instantiated to concrete type
open Yard.Generators.GLL.Parser
open Yard.Generators.GLL
open Yard.Generators.RNGLR.AST
type Token =
    | N of (int)
    | P of (int)
    | RNGLR_EOF of (int)


let genLiteral (str : string) (data : int) =
    match str.ToLower() with
    | x -> None
let tokenData = function
    | N x -> box x
    | P x -> box x
    | RNGLR_EOF x -> box x

let numToString = function
    | 0 -> "e"
    | 1 -> "error"
    | 2 -> "yard_start_rule"
    | 3 -> "N"
    | 4 -> "P"
    | 5 -> "RNGLR_EOF"
    | _ -> ""

let tokenToNumber = function
    | N _ -> 3
    | P _ -> 4
    | RNGLR_EOF _ -> 5

let isLiteral = function
    | N _ -> false
    | P _ -> false
    | RNGLR_EOF _ -> false

let isTerminal = function
    | N _ -> true
    | P _ -> true
    | RNGLR_EOF _ -> true
    | _ -> false

let numIsTerminal = function
    | 3 -> true
    | 4 -> true
    | 5 -> true
    | _ -> false

let numIsNonTerminal = function
    | 0 -> true
    | 1 -> true
    | 2 -> true
    | _ -> false

let numIsLiteral = function
    | _ -> false

let isNonTerminal = function
    | e -> true
    | error -> true
    | yard_start_rule -> true
    | _ -> false

let getLiteralNames = []
let mutable private cur = 0

let acceptEmptyInput = false

let leftSide = [|0; 0; 2|]
let table = [| [|1; 0|];[||];[||];[||];[||];[||];[|2|];[||];[||]; |]
let private rules = [|3; 0; 4; 0; 0|]
let private canInferEpsilon = [|false; true; false; false; false; false|]
let defaultAstToDot =
    (fun (tree : Yard.Generators.RNGLR.AST.Tree<Token>) -> tree.AstToDot numToString tokenToNumber leftSide)

let private rulesStart = [|0; 1; 4; 5|]
let startRule = 2
let indexatorFullCount = 6
let rulesCount = 3
let indexEOF = 5
let nonTermCount = 3
let termCount = 3
let termStart = 3
let termEnd = 5
let literalStart = 6
let literalEnd = 5
let literalsCount = 0


let private parserSource = new ParserSource2<Token> (tokenToNumber, genLiteral, numToString, tokenData, isLiteral, isTerminal, isNonTerminal, getLiteralNames, table, rules, rulesStart, leftSide, startRule, literalEnd, literalStart, termEnd, termStart, termCount, nonTermCount, literalsCount, indexEOF, rulesCount, indexatorFullCount, acceptEmptyInput,numIsTerminal, numIsNonTerminal, numIsLiteral, canInferEpsilon)
let buildAst : (seq<Token> -> ParseResult<_>) =
    buildAst<Token> parserSource


