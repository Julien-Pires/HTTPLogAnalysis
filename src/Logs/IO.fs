namespace rec Logs

open System.IO
open FSharp.Control
open FSharpx.Control

type FileAction =
    | Created
    | Deleted

module File =
    let private readLines (stream : StreamReader) =
        let rec loop acc =
            match stream.ReadLine() with
            | null -> acc
            | x -> loop (x::acc)
        loop [] |> List.rev

    let checkFileStatus directory file =
        let watcher = new FileSystemWatcher(directory)
        watcher.Filter <- file
        watcher.EnableRaisingEvents <- true
        let created = watcher.Created |> Observable.map (fun c -> Created)
        let deleted = watcher.Deleted |> Observable.map (fun c -> Deleted)
        Observable.merge created deleted

    let read path =
        let rec loop (stream : StreamReader) = asyncSeq {
            for i in readLines stream do
                yield i
            do! Async.Sleep 100
            yield! loop stream }

        asyncSeq {
            let file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            file.Seek(file.Length, SeekOrigin.Begin) |> ignore
            let reader = new StreamReader(file)
            yield! loop reader }

    let readContinuously path =
        let rec loop () = async {
            if File.Exists path then
                return read path
            else
                let file = Path.GetFileName path
                let directory = Path.GetDirectoryName path
                let! _ = checkFileStatus directory file |> Async.AwaitObservable
                return! loop() }
        loop()

    let writeContinuously path (source : AsyncSeq<string>) = async {
        let file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
        let writer = new StreamWriter(file)
        writer.AutoFlush <- true
        for i in source do 
            writer.WriteLine(i) }