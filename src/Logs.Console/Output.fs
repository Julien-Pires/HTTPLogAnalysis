namespace Logs.Console

open Logs

module Output =
    let console = ConsoleFormat()

    let display configuration statistics = 
        for i in statistics do
            match Map.tryFind i.Name configuration with
            | Some conf ->
                match conf with
                | Table x -> console.WriteTable i.Result x
            | None -> ()
        console.Write()