﻿//  Copyright 2010-2011 by Konstantin Ulitin
//
//  This file is part of YaccConctructor.
//
//  YaccConstructor is free software: you can redistribute it and/or modify
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

module Yard.Core.FrontendsManager

let private frontendsCollection: ResizeArray<IFrontend> = 
    new ResizeArray<IFrontend>(ComponentsLoader.LoadComponents(typeof<IFrontend>) |> Seq.map (fun x -> x :?> IFrontend))

let AvailableFrontends = Seq.map (fun (x:IFrontend) -> x.Name) frontendsCollection

let Frontend name = frontendsCollection.Find (fun frontend -> frontend.Name = name)
let Register (frontend) = frontendsCollection.Add (frontend)

let GetByExtension = function
    | "yrd" -> Some(Frontend "YardFrontend")
    | "g"   -> Some(Frontend "AntlrFrontend")
    | _     -> None

