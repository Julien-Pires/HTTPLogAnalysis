namespace Logs

[<AbstractClass>]
type IComputation() =
    abstract member Compute : Request seq -> StatisticItem list

type RankingComputation(key : Request -> obj) =
    inherit IComputation()

    override __.Compute requests =
        requests
        |> Seq.groupBy key
        |> Seq.map (fun (key, requests) -> (key, requests, requests |> Seq.length))
        |> Seq.sortByDescending (fun (_, _, count) -> count)
        |> Seq.map (fun (key, _, count) -> { Name = string key; Value = string count })
        |> Seq.toList