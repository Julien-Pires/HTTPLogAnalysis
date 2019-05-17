namespace Logs.Server

open System
open System.Globalization
open FSharp.Control

module LogFactory =
    let addresses = [|
        "127.0.0.1"
        "148.254.0.1"
        "192.168.0.1"
        "100.10.10.1"|]

    let users = [|
        "Gandalf"
        "Sauron"
        "Bilbo"
        "Saruman"|]

    let sections = [|
        "/resources/map.png"
        "/resources/photo.png"
        "/pages/spells.html"
        "/pages/army.html"
        "/quest.pdf"|]

    let Logs =
        let rec loop () = asyncSeq {
            do! Async.Sleep 100
            let rnd = Random()
            let address = rnd.Next(0, addresses.Length)
            let user = rnd.Next(0, users.Length)
            let section = rnd.Next(0, sections.Length)
            let date = DateTime.Now.ToString("dd/MMM/yyyy:HH:mm:ss zz", CultureInfo.InvariantCulture)
            let log = sprintf "%s - %s [%s] \"GET %s HTTP/1.0\" 200 2326" addresses.[address] users.[user] date sections.[section]
            yield log
            yield! loop() }
        loop