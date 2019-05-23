namespace rec Logs

open System.IO
open FSharp.Control
open FSharpx.Control

/// <summary>Represents an action that has been performed on a file</summary>
type FileAction =
    | Created
    | Deleted

/// <summary>Contains I/O operations to work with files</summary>
module File =
    /// <summary>Reads all lines in a stream until the end is reached</summary>
    let private readLines (stream : StreamReader) =
        let rec loop acc =
            match stream.ReadLine() with
            | null -> acc
            | x -> loop (x::acc)
        loop [] |> List.rev

    /// <summary>Reads a file continuously returning each line through an async sequence</summary>
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

    /// <summary>Gets an observable that is triggered when a specified file is either created or deleted</summary>
    let checkFileStatus directory file =
        let watcher = new FileSystemWatcher(directory)
        watcher.Filter <- file
        watcher.EnableRaisingEvents <- true
        let created = watcher.Created |> Observable.map (fun c -> Created)
        let deleted = watcher.Deleted |> Observable.map (fun c -> Deleted)
        Observable.merge created deleted

    /// <summary>Reads a file continuously if it exists or when it has been created</summary>
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

    /// <summary>Writes continuously to a file with the value provided by the input async sequence</summary>
    let writeContinuously path (source : AsyncSeq<string>) = async {
        let file = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)
        let writer = new StreamWriter(file)
        writer.AutoFlush <- true
        for i in source do 
            writer.WriteLine(i) }