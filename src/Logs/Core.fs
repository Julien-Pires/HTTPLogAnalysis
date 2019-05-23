namespace Logs

open System
open System.Runtime.CompilerServices

type Counter(target) =
    let locker = obj()
    let mutable remaining = target
    let mutable lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

    member __.IsCompleted with get() = remaining <= 0

    member __.Update () =
        lock locker (fun _ ->
            let tick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            remaining <- remaining - int(tick - lastTick)
            lastTick <- tick)

    member __.Reset () =
        lock locker (fun _ ->
            remaining <- target
            lastTick <- DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())

module Operator =
    let equal = (=)
    let superior = (>)
    let superiorOrEqual = (>=)
    let inferior = (<)
    let inferiorOrEqual = (<=)

[<Extension>]
type DateTime() =
    [<Extension>]
    static member inline ToTimestamp(date : System.DateTime) =
        DateTimeOffset(date).ToUnixTimeSeconds()