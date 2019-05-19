namespace Logs.Console

open System
open System.Text
open Logs

type TableConfiguration = {
    Title : string option 
    Headers : string list
    Columns : (Statistic -> string) list
    ColumnWidth : int }

type DisplayConfiguration =
    | Table of TableConfiguration

module TableFormatting = 
    let appendSeparator (separator : char) repeat (builder : StringBuilder) =
        builder.Append(separator, repeat)
               .AppendLine()

    let appendTitle (title : string option) (builder : StringBuilder) =
        match title with
        | Some x -> builder.Append(x).AppendLine()
        | None -> builder

    let appendHeaders (headers : string list) width (builder : StringBuilder) =
        headers
        |> List.fold (fun (acc : StringBuilder) c -> 
            acc.Append(c)
               .Append(' ', width - c.Length)) builder
        |> fun c -> c.AppendLine()

    let appendLine item (columns : ('a -> string) list) width (builder : StringBuilder) =
        columns
        |> List.fold(fun (acc : StringBuilder) selector ->
            let value = selector item
            acc.Append(value)
               .Append(' ', width - value.Length)) builder
        |> fun c -> c.AppendLine()

    let appendLines items columns width builder =
        items
        |> List.fold (fun acc c -> appendLine c columns width acc) builder

    let output items configuration builder =
        let count = configuration.Headers |> List.length
        builder
        |> appendTitle configuration.Title
        |> appendSeparator '-' (count * configuration.ColumnWidth)
        |> appendHeaders configuration.Headers configuration.ColumnWidth
        |> appendSeparator '-' (count * configuration.ColumnWidth)
        |> appendLines items configuration.Columns configuration.ColumnWidth

type ConsoleFormat() =
    let builder = StringBuilder()

    member __.WriteTable statistics configuration =
        TableFormatting.output statistics configuration builder |> ignore

    member __.Write () =
        Console.Clear()
        Console.WriteLine(builder.ToString())
        builder.Clear() |> ignore