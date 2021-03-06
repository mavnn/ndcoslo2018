module Tests

open System
open Expecto
open Expecto.ExpectoFsCheck
open FsCheck
open Rendering.Implementation

type Dict<'a, 'b> = System.Collections.Generic.Dictionary<'a, 'b>
type IDict<'a, 'b> = System.Collections.Generic.IDictionary<'a, 'b>
type KVP<'a, 'b> = System.Collections.Generic.KeyValuePair<'a, 'b>

type TaglessString(arbitraryString) =
    let rec removeOpenings (str : string) =
        let reduced = str.Replace("{{", "{")
        if reduced = str then
            str
        else
            removeOpenings reduced

    member __.Value =
        arbitraryString
        |> removeOpenings
        |> fun v ->
            if v.EndsWith "{" then
                v + "a"
            else
                v
                    
    override x.ToString() =
        "Tagless: " + x.Value

type ValidKey = ValidKey of string
    with
        member x.Value =
            match x with
            | ValidKey v -> v
        static member Openers =
            List.concat [['a'..'z'];['A'..'Z']]
        static member Remaining =
            List.concat [ValidKey.Openers;['0'..'9']]

type TemplateSituation =
    { Model : IDict<string, string>
      Template : string
      Expected : string }

type Generators() =
    static member KeyValuePairSeq() : Arbitrary<KVP<string, string> seq> =
        Arb.from<IDict<_,_>>
        |> Arb.convert
            (fun m ->
                m :> seq<KVP<ValidKey, NonNull<string>>>
                |> Seq.map (fun kv -> KVP(kv.Key.Value, kv.Value.Get)))
            (fun kvps ->
                kvps
                |> Seq.map (fun kv -> KVP(ValidKey kv.Key, NonNull kv.Value))
                |> Dict
                :> IDict<_, _>)

    static member TaglessString() : Arbitrary<TaglessString> =
        Arb.from<NonNull<string>>
        |> Arb.convert
            (fun nn -> TaglessString(nn.Get))
            (fun ts -> NonNull ts.Value)

    static member ValidKey() : Arbitrary<ValidKey> =
        gen {
            let! opener = Gen.elements ValidKey.Openers
            let! rest = Gen.arrayOf (Gen.elements ValidKey.Remaining)
            let allChars = Array.concat [[|opener|]; rest]
            return allChars |> String |> ValidKey
        } |> Arb.fromGen

    static member TemplateSituation() : Arbitrary<TemplateSituation> =
        gen {
            let! (modelData : KVP<string, string> seq) = Arb.generate
            let model = Dict(modelData)
            let taglessSection =
                gen {
                    let! (ts : TaglessString) = Arb.generate
                    return ts.Value, ts.Value
                }
            let taggedSection =
                gen {
                    if Seq.isEmpty modelData then
                        return "", ""
                    else
                        let! kv = Gen.elements modelData
                        return "{{" + kv.Key + "}}", kv.Value
                }
            let! sections =
                Gen.listOf (Gen.oneof [taglessSection;taggedSection])
            let before =
                sections
                |> List.map fst
                |> String.concat ""
            let after =
                sections
                |> List.map snd
                |> String.concat ""
            return
                { Model = model
                  Template = before
                  Expected = after }
        } |> Arb.fromGen

let checkConfig =
    { FsCheckConfig.defaultConfig with arbitrary = [typeof<Generators>] }

[<Tests>]
let tests =
    testList "samples" [
        testPropertyWithConfig checkConfig "Doesn't throw reprise" <|
            fun model content ->
                let result = renderText model content
                true

        testPropertyWithConfig checkConfig "Tagless strings don't change" <|
            fun model (ts : TaglessString) ->
                let result = renderText model ts.Value
                match result with
                | Ok rendered ->
                    ts.Value = rendered |@ "Invalid change"
                | Error err ->
                    false |@ sprintf "Rendering error: %A" err

        testPropertyWithConfig checkConfig "Tagged string do change" <|
            fun ts ->
                let result = renderText ts.Model ts.Template
                match result with
                | Ok rendered ->
                    Expect.equal rendered ts.Expected "Invalid change"
                | Error err ->
                    failtestf "Rendering error: %A" err
    ]
