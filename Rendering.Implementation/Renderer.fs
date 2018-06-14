module Rendering.Implementation

open System
open System.Collections.Generic
open System.Text.RegularExpressions

type RenderingError =
    | NullModel
    | NullContent

let private renderTextLogic model content =
    let fold str (key, value : string) =
        let regex = Regex(sprintf """\{\{\s?%s\s?\}\}""" key)
        regex.Replace(str, value)
    model
    |> Seq.map (fun (kv : KeyValuePair<_,_>) -> kv.Key, kv.Value)
    |> Seq.fold fold content

let renderText model content =
    match model, content with
    | m, _ when isNull m ->
        Error NullModel
    | _, c when isNull c ->
        Error NullContent
    | _ ->
        renderTextLogic model content
        |> Ok
