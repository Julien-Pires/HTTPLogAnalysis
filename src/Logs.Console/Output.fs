namespace Logs.Console

open System
open System.Text
open Logs

type LineConfiguration = {
    Text : string 
    Parameters : Statistic list -> obj array }

type DisplayConfiguration =
    | Line of LineConfiguration
    | Table of TableConfiguration

module Output =
    let private builder = StringBuilder()

    let private writeTitle (title: string) =
        builder.AppendLine()
               .Append('-', (Console.BufferWidth / 2) - title.Length)
               .Append(title)
               .Append('-', (Console.BufferWidth / 2) - title.Length)
               .AppendLine() |> ignore

    let private writeTable statistics configuration =
         TableFormatting.output statistics configuration builder
         |> fun c -> c.AppendLine() |> ignore

    let private writeLine statistic configuration =
        let parameters = configuration.Parameters statistic
        builder.AppendFormat(configuration.Text, parameters)
               .AppendLine() |> ignore

    let private writeAlert (alert : AlertResponse) =
        let status =
            match alert.Status with
            | Triggered -> "triggered"
            | Cleared -> "cleared"
        builder.Append("Alert ")
               .Append(alert.Name)
               .Append(" has been ")
               .Append(status)
               .Append(" at ")
               .Append(alert.Date)
               .AppendLine() |> ignore

    let private writeBuffer () =
         Console.Clear()
         Console.WriteLine(builder.ToString())
         builder.Clear() |> ignore

    let display configuration statistics alerts = 
        writeTitle("Statistic")
        for i in statistics do
            match Map.tryFind i.Name configuration with
            | Some conf ->
                match conf with
                | Table x -> writeTable i.Result x
                | Line x -> writeLine i.Result x
            | None -> ()
        writeTitle("Alerts Historic")
        for i in alerts do
            writeAlert i
        writeBuffer()