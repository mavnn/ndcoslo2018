module Rendering.Implementation

open System
open System.Collections.Generic
open System.Text.RegularExpressions

type RenderingError =
    | NullModel
    | NullContent
    | NullWithinModel
    | InvalidKeySupplied

let private renderTextLogic model content =
    let fold str (key, value : string) =
        let regex = Regex(sprintf """\{\{\s?%s\s?\}\}""" key)
        regex.Replace(str, value)
    model
    |> Seq.map (fun (kv : KeyValuePair<_,_>) -> kv.Key, kv.Value)
    |> Seq.fold fold content

let private isNullWithinModel model =
    model
    |> Seq.exists (fun (kv : KeyValuePair<_,_>) -> isNull kv.Key || isNull kv.Value)

let private allKeysValid model =
    let isValidKey k =
        not (String.IsNullOrWhiteSpace k)
        && Char.IsLetter k.[0]
        && (k.[1..] |> Seq.forall Char.IsLetterOrDigit)
    model
    |> Seq.forall (fun (kv : KeyValuePair<_,_>) -> isValidKey kv.Key)

let renderText model content =
    match model, content with
    | m, _ when isNull m ->
        Error NullModel
    | _, c when isNull c ->
        Error NullContent
    | m, _ when isNullWithinModel m ->
        Error NullWithinModel
    | m, _ when not (allKeysValid m) ->
        Error InvalidKeySupplied
    | _ ->
        renderTextLogic model content
        |> Ok
