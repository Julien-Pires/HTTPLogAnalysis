namespace Logs

type RepositoryOperation =
    | Get of (string * AsyncReplyChannel<StatisticResult option>)
    | Update of StatisticResult list

type StatisticsRepository() =
    let data = ref (Map.empty : Map<string, StatisticResult>)

    member __.Get name =
        !data |> Map.tryFind name

    member __.Update (results : StatisticResult list) = 
        data := 
            results
            |> List.fold (fun acc c -> acc |> Map.add c.Name c) !data