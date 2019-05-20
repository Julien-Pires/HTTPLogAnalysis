namespace Logs.Server

open System
open System.Text
open System.Globalization
open FSharp.Control

module LogFactory =
    let Logs interval count =
        let rec loop (builder : StringBuilder) = asyncSeq {
            do! Async.Sleep interval
            for _ in 1 .. count do
                builder.Clear() |> ignore
                let rnd = Random()
                let address = rnd.Next(0, Data.addresses.Length)
                let user = rnd.Next(0, Data.users.Length)
                let section = rnd.Next(0, Data.sections.Length)
                let date = DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss zz", CultureInfo.InvariantCulture)
                let log =
                    builder.Append(Data.addresses.[address])
                           .Append(" - ")
                           .Append(Data.users.[user])
                           .Append(" [")
                           .Append(date)
                           .Append("] ")
                           .Append("\"GET ")
                           .Append(Data.sections.[section])
                           .Append(" HTTP/1.0\" 200 2326")
                           .ToString()
                yield log
            yield! loop builder }
        loop <| StringBuilder()