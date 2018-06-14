module Rendering.Implementation

open System
open System.Collections.Generic
open System.Text.RegularExpressions

type RenderingError =
    | NullModel
    | NullContent
    | NullWithinModel
    | InvalidKeySupplied
    | MalformedTag of int
    | MissingKey

let private peek str i =
    if i >= 0 && i < String.length str then
        Some str.[i]
    else
        None

let private consumeIf f str i =
    match peek str i with
    | Some c when f c -> (Some c), i + 1
    | _ -> None, i

let rec private skipWhiteSpace (str : string) index =
    match consumeIf Char.IsWhiteSpace str index with
    | _, newIndex -> newIndex

let rec private readRestIdentifier (str : string) index idChars =
    match consumeIf Char.IsLetterOrDigit str index with
    | Some c, i ->
        readRestIdentifier str i (Array.append idChars [| c |])
    | None, i ->
        String idChars, i

let private readIdentifier (model : IDictionary<string, string>) (str : string) index =
    match consumeIf Char.IsLetter str index with
    | Some c, i ->
        let key, newIndex = readRestIdentifier str i [| c |]
        match model.TryGetValue key with
        | true, v -> Ok (v, newIndex)
        | false, _ -> Error MissingKey
    | None, _ ->
        Error <| MalformedTag index

let private readTag model str index =
    let index = skipWhiteSpace str index
    match readIdentifier model str index with
    | Ok (result, index) ->
        let index = skipWhiteSpace str index
        match peek str index, peek str (index + 1) with
        | Some '}', Some '}' ->
            Ok (result, index + 2)
        | _ ->
            Error <| MalformedTag index
    | Error err ->
        Error err

let private tagNext str index =
    match peek str index, peek str (index + 1) with
    | Some '{', Some '{' ->
        true, index + 2
    | _ ->
        false, index

let rec private readNonTag str index (chars : char []) =
    match tagNext str index with
    | true, _ ->
        String chars, index
    | false, i ->
        match consumeIf (fun _ -> true) str i with
        | Some c, newIndex ->
            readNonTag str newIndex (Array.append chars [| c |])
        | None, newIndex ->
            String chars, newIndex

let rec private buildSections model str index sections =
    match tagNext str index with
    | true, i ->
        match readTag model str (index + 2) with
        | Ok (result, index) ->
            buildSections model str index (result::sections)
        | Error err ->
            Error err
    | false, i ->
        if String.length str = index then
            sections
            |> List.rev
            |> String.concat ""
            |> Ok
        else
            let result, index = readNonTag str index [||]
            buildSections model str index (result::sections)

let private renderTextLogic (model : KeyValuePair<string, string> seq) content =
    if String.length content = 0 then
        Ok ""
    else
        let d = dict (model |> Seq.map (fun kv -> kv.Key, kv.Value))
        buildSections d content 0 []

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
