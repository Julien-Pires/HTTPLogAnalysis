namespace Logs

open System
open FSharpx.Collections

type AlertStatus =
    | Triggered
    | Cleared

type AlertMessage = StatisticResult * AsyncReplyChannel<AlertStatus option>

type AlertMonitoringState = {
    Values : Queue<int> 
    PreviousStatus : AlertStatus }

type AlertConfiguration = {
    Name : string
    Statistic : string
    Rule : Agent<AlertMessage> -> Async<unit> }

type AlertAgent = {
    Name : string
    Statistic : string
    Agent : Agent<AlertMessage> }

type AlertResponse = {
    Name : string
    Status : AlertStatus
    Date : DateTime }

module AlertMonitoring =
    let thresholdReached timeRange threshold key = (fun (inbox : Agent<AlertMessage>) ->
        let rec loop (state : AlertMonitoringState) = async {
            let updateValues newValue =
                if state.Values.Length > timeRange then 
                    state.Values.Tail.Conj newValue
                else
                    state.Values.Conj newValue

            let! (statistic, reply) = inbox.Receive()

            let newValues = updateValues (statistic.Result.Head.Values.[key] :?> int)
            let average = (newValues |> Seq.sum) / newValues.Length
            let newStatus = 
                if average > threshold then
                    Triggered 
                else
                    Cleared
            if newStatus <> state.PreviousStatus then
                reply.Reply <| Some newStatus
            else
                reply.Reply None
            return! loop { Values = newValues; PreviousStatus = newStatus } }
        loop { Values = Queue.empty; PreviousStatus = Cleared })

type AlertMonitoring (alerts : AlertConfiguration list) =
    let alertAgents =
        alerts
        |> Seq.map (fun c -> {
            Name = c.Name
            Statistic = c.Statistic
            Agent = new Agent<AlertMessage>(c.Rule) })
        |> Seq.groupBy (fun c -> c.Statistic)
        |> Seq.map (fun (key, values) -> (key, values |> Seq.toList))
        |> Map.ofSeq

    let findAgents (statistic : StatisticResult) =
        match alertAgents |> Map.tryFind statistic.Name with
        | Some agents -> Some (statistic, agents)
        | None -> None

    let updateAgent statistic agent =
        let response = agent.Agent.PostAndReply (fun c -> statistic, c)
        match response with
        | Some x -> Some {
            Name = agent.Name 
            Status = x
            Date = DateTime.Now }
        | None -> None

    do
        alertAgents
        |> Seq.collect (fun c -> c.Value)
        |> Seq.iter (fun c -> c.Agent.Start())

    member __.Update (stats : StatisticResult list) =
        stats
        |> Seq.choose findAgents
        |> Seq.collect (fun (stats, agents) -> agents |> Seq.map (updateAgent stats))
        |> Seq.choose id
        |> Seq.toList