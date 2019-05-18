namespace Logs.Console

open System
open Logs

module Console =
    let writeTable (statistic : ComputationItem list) =
        statistic
        |> Seq.iter (fun c -> printfn "%s %s" c.Name c.Value)

    let print = function
        | Multiple x -> writeTable x