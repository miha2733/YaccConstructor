﻿namespace Yard.Generators.CYKGenerator

open System.Collections.Generic

type CYKCoreForGPU() =

    // правила грамматики, инициализируются в Recognize
    let mutable rules : array<rule> = [||]

    let mutable recTable:_[,] = null

    let mutable lblNameArr = [||]

    [<Literal>]
    let noLbl = 0uy

    let lblString lbl = 
        match lblNameArr with
        | [||] -> "0" 
        | _ -> 
                match lbl with 
                | 0uy -> "0"
                | _ -> lblNameArr.[(int lbl) - 1]

    // возвращает нетерминал A правила A->BC, правило из i-го элемента массива указанной ячейки
    let getCellRuleTop (cellData:CellData) =
        let curRuleNum,_,_,_ = getData cellData.rData
        let rule = getRuleStruct rules.[int curRuleNum]
        rule.RuleName

    // возвращает координаты дочерних ячеек 
    // i l - координаты текущей ячейки
    // k - число, определяющее координаты
    let getSubsiteCoordinates i l k =
        (i,k),(k+i+1,l-k-1)

    let recognitionTable (_,_) (s:uint16[]) weightCalcFun =

        let nTermsCount = 
            rules
            |> Array.map(fun r -> 
                            let rule = getRuleStruct r
                            rule.RuleName)
            |> Set.ofArray
            |> Set.count

        recTable <- Microsoft.FSharp.Collections.Array2D.init s.Length s.Length (fun _ _ -> Array.init nTermsCount (fun _ -> None))

        let chooseNewLabel (ruleLabel:uint8) (lbl1:byte) (lbl2:byte) lState1 lState2 =
            let conflictLbl = (noLbl,LblState.Conflict)
            
            if lState1 = LblState.Conflict then conflictLbl
            elif lState2 = LblState.Conflict then conflictLbl
            elif lState1 = LblState.Undefined && lState2 = LblState.Undefined && ruleLabel = noLbl then noLbl,LblState.Undefined
            else
                let notEmptyLbls = [| noLbl; noLbl; noLbl |]
                let mutable realLblCount = 0
                if lbl1 <> noLbl
                then 
                    notEmptyLbls.[0] <- lbl1
                    realLblCount <- realLblCount + 1
                if lbl2 <> noLbl
                then
                    notEmptyLbls.[realLblCount] <- lbl2
                    realLblCount <- realLblCount + 1
                if ruleLabel <> noLbl
                then 
                    notEmptyLbls.[realLblCount] <- ruleLabel
                    realLblCount <- realLblCount + 1

                if realLblCount = 1
                then notEmptyLbls.[0],LblState.Defined
                elif (realLblCount = 2 && notEmptyLbls.[1] = notEmptyLbls.[0]) ||
                    (realLblCount = 3 && notEmptyLbls.[1] = notEmptyLbls.[0] && notEmptyLbls.[2] = notEmptyLbls.[0])
                then notEmptyLbls.[0],LblState.Defined
                else noLbl,LblState.Conflict
                
        let processRule rule ruleIndex i k l =
            let rule = getRuleStruct rule
            if rule.R2 <> 0us then
                let leftCell = recTable.[i, k]
                let rightCell = recTable.[k+i+1, l-k-1]

                for m in 0..leftCell.Length - 1 do
                    if leftCell.[m].IsSome && getCellRuleTop leftCell.[m].Value = rule.R1
                    then
                        let cellData1 = getCellDataStruct leftCell.[m].Value
                        for n in 0..rightCell.Length - 1 do
                            if rightCell.[n].IsSome && getCellRuleTop rightCell.[n].Value = rule.R2
                            then
                                let cellData2 = getCellDataStruct rightCell.[n].Value
                                let newLabel,newlState = chooseNewLabel rule.Label cellData1.Label cellData2.Label cellData1.LabelState cellData2.LabelState
                                let newWeight = weightCalcFun rule.Weight cellData1.Weight cellData2.Weight
                                let currentElem = buildData ruleIndex newlState newLabel newWeight
                                recTable.[i, l].[int rule.RuleName - 1] <- new CellData(currentElem, uint32 k) |> Some

        let elem i l = rules |> Array.iteri (fun ruleIndex rule -> for k in 0..(l-1) do processRule rule ruleIndex i k l)

        let fillTable () =
          [|1..s.Length-1|]
          |> Array.iter (fun l ->
                [|0..s.Length-1-l|]
                |> Array.Parallel.iter (fun i -> elem i l))
        
        rules
        |> Array.iteri 
            (fun ruleIndex rule ->
                for k in 0..(s.Length-1) do
                    let rule = getRuleStruct rule               
                    if rule.R2 = 0us && rule.R1 = s.[k] then
                        let lState =
                            match rule.Label with
                            | 0uy -> LblState.Undefined
                            | _   -> LblState.Defined
                        let currentElem = buildData ruleIndex lState rule.Label rule.Weight
                        recTable.[k,0].[int rule.RuleName - 1] <- new CellData(currentElem,0u) |> Some)
    
        fillTable ()
        recTable

    let recognize ((grules, start) as g) s weightCalcFun =
        let recTable = recognitionTable g s weightCalcFun
        
        let printTbl () =
            for i in 0..s.Length-1 do
                for j in 0..s.Length-1 do
                    let cd = recTable.[i,j] |> Array.filter (fun x -> x.IsSome) |> fun a-> a.Length
                    printf "! %s !" (string cd)
                printfn " "
            printfn "" 

        //printfn "%A" recTable
        //printTbl ()

        let getString state lbl weight = 
            let stateString = 
                match state with
                |LblState.Defined -> "defined"
                |LblState.Undefined -> "undefined"
                |LblState.Conflict -> "conflict"
                |_ -> ""

            String.concat " " [stateString; ":"; "label ="; lblString lbl; "weight ="; string weight]
            
        let rec out i last =
            let cellDatas = recTable.[0, s.Length-1]
            if i <= last 
            then 
                if cellDatas.[i].IsSome 
                then
                    let cellData = getCellDataStruct (cellDatas.[i].Value)
                    if i = last
                    then [getString cellData.LabelState cellData.Label cellData.Weight]
                    else getString cellData.LabelState cellData.Label cellData.Weight :: out (i+1) last
                else "" :: out (i+1) last
            else [""]

        let lastIndex = (recTable.[0,s.Length-1]).Length - 1
        
        out 0 lastIndex

    let print lblValue weight leftI rightL leftL =
        let out = String.concat " " ["label ="; lblString lblValue; "weight ="; string weight; 
                    "left ="; string leftI; "right ="; string (leftI + rightL + leftL + 1)]
        printfn "%s" out

    let rec trackLabel i l (cell:CellData)  flag =
        let ruleInd,_,curL,curW = getData cell.rData
        let rule = getRuleStruct rules.[int ruleInd]
        let (leftI,leftL),(rightI,rightL) = getSubsiteCoordinates i l (int cell._k)
        if l = 0
        then if curL <> noLbl
             then print curL curW leftI rightL leftL
        else 
            let left =
                recTable.[leftI,leftL]
                |> Array.tryFind (fun (x:Option<CellData>) -> 
                                        match x with
                                        | Some x ->
                                            let ind,lSt,lbl,_ = getData x.rData
                                            let rule = getRuleStruct rules.[int ind]
                                            rule.RuleName = rule.R1
                                        | None -> false)
            let right = 
                recTable.[rightI,rightL]
                |> Array.tryFind (fun (x:Option<CellData>) -> 
                                        match x with
                                        | Some x -> 
                                            let ind,lSt,lbl,_ = getData x.rData
                                            let rule = getRuleStruct rules.[int ind]
                                            rule.RuleName = rule.R2
                                        | None -> false)

            match right with
            | Some (Some right) ->
                match left with 
                | Some (Some left) ->
                    let _,_,lLbl,_ = getData left.rData
                    let _,_,rLbl,_ = getData right.rData
                    if curL <> noLbl && lLbl = noLbl && rLbl = noLbl
                    then print curL curW leftI rightL leftL
                    else
                        trackLabel leftI leftL left  true
                        trackLabel rightI rightL right  true
                | _ -> ()
            | _ ->
                if flag && rule.Label <> noLbl
                then print curL curW leftI rightL leftL
            
    let labelTracking lastInd = 
        let i,l = 0,lastInd
        Array.iteri (fun k x ->
                    x |> Option.iter(fun x ->
                        let out = "derivation #" + string (k + 1)
                        printfn "%s" out
                        trackLabel i l x false)
        ) recTable.[i, l]
            
    
    member this.Recognize ((grules, start) as g) s weightCalcFun lblNames = 
        rules <- grules
        lblNameArr <- lblNames
        // Info about dialects of derivation in format: "<lblState> <lblName> <weight>"
        // If dialect undefined or was conflict lblName = "0" 
        let out = recognize g s weightCalcFun |> List.filter ((<>)"") |> String.concat "\n"
        match out with
        | "" -> "Строка не выводима в заданной грамматике."
        | _ -> 
            labelTracking (s.Length - 1)
            out
