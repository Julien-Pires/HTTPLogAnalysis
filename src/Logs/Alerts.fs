namespace Logs

open System
open FSharpx.Collections

type AlertStatus =
    | Triggered
    | Cleared

type AlertMessage =
    | Update of (StatisticResult * AsyncReplyChannel<AlertStatus option>)

type AlertMonitoringState = {
    Values : Queue<int> 
    PreviousStatus : AlertStatus }

type AlertAgent(range, threshold) =
    let agent = Agent.Start(fun inbox ->
        let rec loop (state : AlertMonitoringState) = async {
            let! msg = inbox.Receive()
            match msg with
            | Update (stats, reply) ->
                let currentValues = state.Values
                let stat = stats.Result.Head.Values.["Count"] :?> int
                let newQueue = 
                    if currentValues.Length > range then 
                        currentValues.Tail.Conj stat
                    else
                        currentValues.Conj stat
                let newValue = (newQueue |> Seq.sum) / newQueue.Length
                let newStatus = if newValue > threshold then Triggered else Cleared
                if newStatus <> state.PreviousStatus then
                    reply.Reply <| Some newStatus
                else
                    reply.Reply None
                return! loop { Values = newQueue; PreviousStatus = newStatus }
            return! loop state }
        loop { Values = Queue.empty; PreviousStatus = Cleared })

    member __.Update stats = agent.PostAndReply (fun c -> Update(stats, c))

type AlertConfiguration = {
    Name : string 
    Rule : AlertAgent 
    StatisticName : string }

type AlertResponse = {
    Name : string
    Status : AlertStatus
    Date : DateTime }

type AlertMonitoring (alerts : AlertConfiguration list) =
    let alerts =
        alerts
        |> Seq.groupBy (fun c -> c.StatisticName)
        |> Seq.map (fun (key, values) -> (key, values |> Seq.toList))
        |> Map.ofSeq

    member __.Update (stats : StatisticResult list) =
        stats
        |> Seq.map (fun c -> (c, alerts |> Map.tryFind c.Name))
        |> Seq.choose (function | (x, Some y) -> Some (x, y) | _ -> None)
        |> Seq.collect (fun (stats, agents) -> 
            agents 
            |> Seq.map (fun c -> (c.Name, c.Rule.Update stats)))
            |> Seq.choose (fun (name, status) -> match status with | Some x -> Some (name, x) | None -> None)
        |> Seq.map (fun (name, status) -> {
            Name = name
            Status = status
            Date = DateTime.Now })
        |> Seq.toList