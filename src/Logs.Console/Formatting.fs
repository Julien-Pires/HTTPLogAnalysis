namespace Logs.Console

open System.Text
open Logs

/// <summary>
/// Represents the configuration to display statistic as a table
/// </summary>
type TableConfiguration = {
    Title : string option 
    Headers : string list
    Columns : (Statistic -> string) list
    ColumnWidth : int }

/// <summary>
/// Contains helper methods to display table on the console
/// </summary>
module TableFormatting = 
    let private noData = "No Data Available"

    let private appendSeparator (separator : char) repeat (builder : StringBuilder) =
        builder.Append(separator, repeat).AppendLine()

    let private appendTitle (title : string option) (builder : StringBuilder) =
        match title with
        | Some x -> builder.Append(x).AppendLine()
        | None -> builder

    let private appendHeaders (headers : string list) width (builder : StringBuilder) =
        headers
        |> List.fold (fun (acc : StringBuilder) c -> 
            acc.Append(c)
               .Append(' ', width - c.Length)) builder
        |> fun c -> c.AppendLine()

    let private appendLine item (columns : ('a -> string) list) width (builder : StringBuilder) =
        columns
        |> List.fold(fun (acc : StringBuilder) selector ->
            let value = selector item
            acc.Append(value)
               .Append(' ', width - value.Length)) builder
        |> fun c -> c.AppendLine()

    let private appendLines items (columns : ('a -> string) list) width (builder : StringBuilder) =
        match items with
        | [] -> 
            builder.Append(' ', (columns.Length * width) / 2 - (noData.Length / 2))
                   .Append(noData)
                   .Append(' ', (columns.Length * width) / 2 - (noData.Length / 2))
        | _ ->
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
        |> fun c -> c.AppendLine()