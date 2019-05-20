namespace Logs.Console

open Logs

module Output =
    let console = ConsoleFormat()

    let display configuration statistics alerts = 
        for i in statistics do
            match Map.tryFind i.Name configuration with
            | Some conf ->
                match conf with
                | Table x -> console.WriteTable i.Result x
            | None -> ()
        for i in alerts do
            console.WriteAlert i
        console.Write()