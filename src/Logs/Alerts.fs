namespace Logs

open System
open FSharpx.Collections

/// <summary>
/// Represents the status of an alert
/// </summary>
type AlertStatus =
    | Triggered
    | Cleared

/// <summary>
/// Represents a message sent to alert agent
/// </summary>
type AlertMessage = StatisticResult * AsyncReplyChannel<AlertStatus option>

/// <summary>
/// Represents a configuration that define an alert agent
/// </summary>
type AlertConfiguration = {
    Name : string
    Statistic : string
    Rule : Agent<AlertMessage> -> Async<unit> }

/// <summary>
/// Represents an alert agent
/// </summary>
type AlertAgent = {
    Name : string
    Statistic : string
    Agent : Agent<AlertMessage> }

/// <summary>
/// Represents a response when an alert agent state has changed
/// </summary>
type AlertResponse = {
    Name : string
    Status : AlertStatus
    Date : DateTime }

/// <summary>
/// Represent an alert agent aggregator that update all alert agents when new statistics are available
/// </summary>
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

    /// <summary>
    /// Finds all agents that listen for the specified statistic
    /// <summary>
    let findAgents (statistic : StatisticResult) =
        match alertAgents |> Map.tryFind statistic.Name with
        | Some agents -> Some (statistic, agents)
        | None -> None

    /// <summary>
    /// Updats all agents with new statistic result
    /// </summary>
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

    /// <summary>
    /// Updats all agents with new statistic result and returns a response when alert states have changed
    /// </summary>
    member __.Update (stats : StatisticResult list) =
        stats
        |> Seq.choose findAgents
        |> Seq.collect (fun (stats, agents) -> agents |> Seq.map (updateAgent stats))
        |> Seq.choose id
        |> Seq.toList

/// <summary>
/// Represents the state of an alert agent
/// </summary>
type AlertMonitoringState = {
    Values : Queue<int> 
    PreviousStatus : AlertStatus }

/// <summary>
/// Contains different kind of alert rules
/// </summary>
module AlertMonitoring =
    let private updateValues (values : Queue<_>) newValue max =
        if values.Length > max then 
            values.Tail.Conj newValue
        else
            values.Conj newValue
    
    /// <summary>
    /// Gets an agent that monitor the average value for the given number of element.
    /// Each time a new value is added by the agent, it compares it with the threshold value
    /// with the help of the compare lambda. If the result is true the alert is triggered.
    /// </summary>
    let avgThresholdReached elementCount threshold compare key = (fun (inbox : Agent<AlertMessage>) ->
        let rec loop (state : AlertMonitoringState) = async {
            let! (statistic, reply) = inbox.Receive()
            let value = (statistic.Result.Head.Values.[key] :?> int)
            let newValues = updateValues state.Values value elementCount
            let average = (newValues |> Seq.sum) / newValues.Length
            let newStatus =
                match compare average threshold with
                | true -> Triggered 
                | false -> Cleared
            if newStatus <> state.PreviousStatus then
                reply.Reply <| Some newStatus
            else
                reply.Reply None
            return! loop { Values = newValues; PreviousStatus = newStatus } }
        loop { Values = Queue.empty; PreviousStatus = Cleared })