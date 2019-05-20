namespace Logs.Console

open System
open Logs

module Configuration =
    let defaultPath = "/temp/access.log"

    let statistics = [
        {   Name = "most_section_hit"
            Computation = Computation.rank (fun c -> c.Sections.Head :> obj)
            RequestsFilter = RequestCache.getRequestsByNow 10.0
            Update = Tick 10000 }

        {   Name = "requests_per_second"
            Computation = Computation.count
            RequestsFilter = (fun c -> RequestCache.getRequestsAt (DateTime.Now.AddSeconds(-1.0)) c)
            Update = Tick 1000 }]
    
    let alerts = [
        {   Name = "requests_limit"
            Rule = AlertAgent(120, 10)
            StatisticName = "requests_per_second" }]

    let display = Map.ofList [
        ("most_section_hit",  Table {
            Title = Some "Section with most hit (last 10 sec)"
            Headers = [
                "Section"
                "Total hit"]
            Columns = [
                (fun (c : Statistic) -> string c.Values.["Name"])
                (fun c -> string c.Values.["Count"])]
            ColumnWidth = 20 })

        ("requests_per_second", Table {
            Title = Some "Number of requests (last 1 sec)"
            Headers = ["Total"]
            Columns = [(fun c -> string c.Values.["Count"])]
            ColumnWidth = 20 })]