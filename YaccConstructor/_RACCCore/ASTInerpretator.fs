﻿//  TableInterpretator.fs contains AST interpreter which  use for in action code calculation
//
//  Copyright 2009,2010 Semen Grigorev <rsdpisuy@gmail.com>
//
//  This file is part of YaccConctructor.
//
//  YaccConstructor is free software:you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace Yard.Generators._RACCGenerator

open Yard.Generators._RACCGenerator.AST


module ASTInterpretator = 
    let rec interp (ruleToActon:System.Collections.Generic.IDictionary<_,_>) tree = 
        let reast = RegExpAST()
        match tree with        
        | Node (childs,name,value) ->
            List.map (interp ruleToActon) childs
            |> (Set.minElement value.trace.Head |> reast.BuildREAST)
            |> fun x -> 
                   match x with 
                   | (t,_,_) -> ruleToActon.[name] t                  
        | Leaf (name,value)        -> 
            match value.value with
            | LeafV(v) -> box v
            | _        -> failwith "AST is incorrect. Leaf contains NodeV value."

        
