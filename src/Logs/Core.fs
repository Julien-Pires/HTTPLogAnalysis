namespace Logs

open System
open System.Runtime.CompilerServices

/// <summary>
/// Represents a simple counter that decrement each time it is updated
/// </summary>
type Counter(target) =
    let locker = obj()
    let mutable remaining = target
    let mutable lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

    /// <summary>
    /// Indicates wether the counter has completed or not
    /// </summary>
    member __.IsCompleted with get() = remaining <= 0

    /// <summary>
    /// Updates the counter
    /// </summary>
    member __.Update () =
        lock locker (fun _ ->
            let tick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            remaining <- remaining - int(tick - lastTick)
            lastTick <- tick)

    /// <summary>
    /// Resets the counter to its target value
    /// </summary>
    member __.Reset () =
        lock locker (fun _ ->
            remaining <- target
            lastTick <- DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())

/// <summary>
/// Represents a set of operators function
/// </summary>
module Operator =
    /// <summary>
    /// Represents an equal operator
    /// </summary>
    let equal = (=)

    /// <summary>
    /// Represents a superior operator
    /// </summary>
    let superior = (>)

    /// <summary>
    /// Represents a superior or equal operator
    /// </summary>
    let superiorOrEqual = (>=)

    /// <summary>
    /// Represents an inferior operator
    /// </summary>
    let inferior = (<)

    /// <summary>
    /// Represents an inferior or equal operator
    /// </summary>
    let inferiorOrEqual = (<=)

/// <summary>
/// Contains extension methods for System.DateTime object
/// </summary>
[<Extension>]
type DateTime() =
    /// <summary>
    /// Returns the DateTime has a unix timestamp in second
    /// </summary>
    [<Extension>]
    static member inline ToTimestamp(date : System.DateTime) =
        DateTimeOffset(date).ToUnixTimeSeconds()