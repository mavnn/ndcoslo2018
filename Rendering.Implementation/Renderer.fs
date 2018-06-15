module Rendering.Implementation

open System
open System.Collections.Generic
open System.Text.RegularExpressions

let renderText model content =
    let fold str (key, value : string) =
        let regex = Regex(sprintf """\{\{\s?%s\s?\}\}""" key)
        regex.Replace(str, value)
    model
    |> Seq.map (fun (kv : KeyValuePair<_,_>) -> kv.Key, kv.Value)
    |> Seq.fold fold content
