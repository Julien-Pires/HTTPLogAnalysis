﻿namespace Logs

type ComputationItem = {
    Name : string 
    Value : string }

[<AbstractClass>]
type IComputation() =
    abstract member Compute : CacheContent -> ComputationItem list

type RankingComputation() =
    inherit IComputation()

    override __.Compute cache =
        cache
        |> RequestCache.getRequests 10.0
        |> Seq.groupBy (fun c -> c.Sections.Head)
        |> Seq.map (fun (section, requests) -> (section, requests, requests |> Seq.length))
        |> Seq.sortByDescending (fun (_, _, count) -> count)
        |> Seq.map (fun (section, _, count) -> { Name = section; Value = string count })
        |> Seq.toList