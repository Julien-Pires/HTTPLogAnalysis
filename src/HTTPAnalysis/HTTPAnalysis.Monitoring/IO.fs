namespace HTTPAnalysis.Monitoring

open System.IO
open FSharp.Control

module File =
    let private readLines (stream : StreamReader) =
        let rec loop acc =
            match stream.ReadLine() with
            | null -> acc
            | x -> loop (x::acc)
        loop [] |> List.rev

    let readContinuously path =
        let rec read (stream : StreamReader) = asyncSeq {
            for i in readLines stream do
                yield i
            do! Async.Sleep 1000 
            yield! read stream }
        let stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
        let reader = new StreamReader(stream)
        read reader