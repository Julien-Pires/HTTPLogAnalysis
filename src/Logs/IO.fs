namespace Logs

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
            do! Async.Sleep 50 
            yield! read stream }
        asyncSeq {
            use file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            use reader = new StreamReader(file)
            yield! read reader }

    let writeContinuously path (source : AsyncSeq<string>) = async {
        let file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
        let writer = new StreamWriter(file)
        writer.AutoFlush <- true
        for i in source do 
            writer.WriteLine(i) }