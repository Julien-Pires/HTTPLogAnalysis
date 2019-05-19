namespace Logs

[<AbstractClass>]
type IComputation() =
    abstract member Compute : Request seq -> Statistic list

type RankingComputation(key : Request -> obj) =
    inherit IComputation()

    override __.Compute requests =
        requests
        |> Seq.groupBy key
        |> Seq.map (fun (key, requests) -> {
            Values = Map.ofList <| [
            ("Name", key)
            ("Count", (Seq.length requests) :> obj)] })
        |> Seq.sortByDescending (fun c -> c.Values.["Count"] :?> int)
        |> Seq.toList

type CountComputation() =
    inherit IComputation()

    override __.Compute requests = [{
        Values = Map.ofList <| [("Count", (Seq.length requests) :> obj)] }]
        