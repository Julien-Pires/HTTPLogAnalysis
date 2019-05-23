namespace Logs.Console

open System
open Logs

/// <summary>
/// Contains the configuration of the log monitoring console
/// </summary>
module Configuration =
    let defaultPath = @"/temp/access.log"

    let statistics = [
        {   Name = "most_section_hit"
            Computation = Statistics.rank (fun c -> c.Sections.Head :> obj)
            RequestsFilter = RequestCache.getRequestsByNow 10L
            Update = Tick 10000 }

        {   Name = "most_active_users"
            Computation = Statistics.rank (fun c -> c.User :> obj)
            RequestsFilter = RequestCache.getRequestsByNow 10L
            Update = Tick 10000 }

        {   Name = "requests_per_second"
            Computation = Statistics.count
            RequestsFilter = (fun c -> RequestCache.getRequestsAt (DateTime.Now.AddSeconds(-1.0)) c)
            Update = Tick 1000 }

        {   Name = "requests_per_last_ten_seconds"
            Computation = Statistics.count
            RequestsFilter = RequestCache.getRequestsByNow 10L
            Update = Tick 10000 }
            
        {   Name = "requests_with_errors"
            Computation = Statistics.countWith (fun c -> c.HTTPCode > 299)
            RequestsFilter = RequestCache.getRequestsByNow 10L
            Update = Tick 10000 } ]
    
    let alerts = [
        {   Name = "requests_limit"
            Statistic = "requests_per_second"
            Rule = AlertMonitoring.avgThresholdReached 120 10 Operator.superiorOrEqual "Count" }
            
        {   Name = "no_traffic_detected"
            Statistic = "requests_per_second"
            Rule = AlertMonitoring.avgThresholdReached 60 0 Operator.equal "Count" }]

    let display = Map.ofList [
        ("requests_per_second", Line {
            Text = "Number of request in the last sec: {0}"
            Parameters = fun c -> [|c.Head.Values.["Count"]|] })

        ("requests_per_last_ten_seconds", Line {
            Text = "Number of request in the last 10 sec: {0}"
            Parameters = fun c -> [|c.Head.Values.["Count"]|] })

        ("requests_with_errors", Line {
            Text = "Number of requests with errors in the last 10 sec: {0}"
            Parameters = fun c -> [|c.Head.Values.["Count"]|] })

        ("most_section_hit",  Table {
            Title = Some "Section with most hit in the last 10 sec"
            Headers = [
                "Section"
                "Total hit"]
            Columns = [
                (fun (c : Statistic) -> string c.Values.["Name"])
                (fun c -> string c.Values.["Count"])]
            ColumnWidth = 20 })

        ("most_active_users",  Table {
            Title = Some "Most active user in the last 10 sec"
            Headers = [
                "User"
                "Total hit"]
            Columns = [
                (fun (c : Statistic) -> string c.Values.["Name"])
                (fun c -> string c.Values.["Count"])]
            ColumnWidth = 20 }) ]