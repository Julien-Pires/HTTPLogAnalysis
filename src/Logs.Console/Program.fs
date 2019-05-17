// Learn more about F# at http://fsharp.org

open System
open FSharp.Control
open HTTPAnalysis.Monitoring

[<EntryPoint>]
let main argv =
    File.readContinuously "C:\Users\Takumi\Desktop\Foo.txt" 
    |> AsyncSeq.iter (fun c -> Console.WriteLine(LogParser.parse c))
    |> Async.Start

    Console.ReadLine() |> ignore
    0 // return an integer exit code
