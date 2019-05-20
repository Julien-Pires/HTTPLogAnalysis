namespace Logs

module Computation =
    let rank key requests =
        requests
        |> Seq.groupBy key
        |> Seq.map (fun (key, requests) -> {
            Values = Map.ofList <| [
            ("Name", key)
            ("Count", (Seq.length requests) :> obj)] })
        |> Seq.sortByDescending (fun c -> c.Values.["Count"] :?> int)
        |> Seq.toList

    let count requests = [{
        Values = Map.ofList <| [("Count", (Seq.length requests) :> obj)] }]
        