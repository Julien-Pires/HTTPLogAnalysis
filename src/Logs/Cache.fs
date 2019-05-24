namespace Logs

open System
open FSharpx.Collections
open FSharpx.Control

/// <summary>
/// Represents a set of action that can be performed on a cache
/// </summary>
type CacheAction =
    | Add of Request
    | GetFrame of int64 * AsyncReplyChannel<Request seq>
    | GetRange of int64 * int64 * AsyncReplyChannel<Request seq>
    | Clear

/// <summary>
/// Represents the data of a cache
/// </summary>
type CacheData = {
    TimeFrames : Map<int64, Request list> }

/// <summary>
/// Represents a cache for HTTP access requests.
/// Requests are cached by datetime (timestamp in second) for faster search when using date.
/// The cache has clear mechanism that allows to trash items that are older than specified lifetime.
///</summary>
type RequestCache(lifetime) =
    /// <summary>
    /// Adds a new request to the cache
    /// </summary>
    let add request (frames : Map<int64, Request list>) =
        let timestamp = DateTime.Now.ToTimestamp()
        let requests =
            match frames.TryFind timestamp with
            | Some x -> request::x
            | None -> [request]
        frames |> Map.add timestamp requests
    
    /// <summary>
    /// Gets all requests for a specific datetime
    /// </summary>
    let getFrame time (frames : Map<int64, Request list>) =
        match frames.TryFind time with
        | Some x -> x
        | None -> []
    
    /// <summary>
    /// Gets all requests for datetime range represented by a start date and an end date
    /// </summary>
    let getRange startTime endTime (frames : Map<int64, Request list>) = seq {
        for i in startTime ..endTime do
            match frames.TryFind i with
            | Some x -> yield! x
            | None -> yield! [] }
    
    /// <summary>
    /// Clears items for which lifetime has expired
    /// </summary>
    let clear (frames : Map<int64, Request list>) =
        let thresholdDate = DateTime.Now.AddSeconds(-lifetime).ToTimestamp()
        frames
        |> Seq.fold (fun (acc : Map<int64, Request list>) c -> 
            if c.Key < thresholdDate then
                acc.Remove c.Key
            else
                acc) frames

    /// <summary>
    /// Represents an agent that process asynchronously cache action
    /// </summary>
    let agent = Agent.Start <| fun inbox ->
        let rec loop (state : CacheData) = async {
            let! msg = inbox.Receive()
            match msg with
            | Add request -> 
                let requests = add request state.TimeFrames
                return! loop { state with TimeFrames = requests }
            | Clear ->
                let requests = clear state.TimeFrames
                return! loop { state with TimeFrames = requests }
            | GetFrame (time, reply) ->
                reply.Reply <| getFrame time state.TimeFrames
                return! loop state
            | GetRange (startTime, endTime, reply) ->
                reply.Reply <| getRange startTime endTime state.TimeFrames
                return! loop state }
        loop {
            TimeFrames = Map.empty }

    /// <summary>
    /// Represents an async action that regularly check for requests lifetime
    /// </summary>
    let clearCache =
        let rec loop () = async {
            do! Async.Sleep 1000
            agent.Post Clear
            return! loop() }
        loop()

    do
        clearCache |> Async.Start

    /// <summary>
    /// Adds a request to the cache
    /// </summary>
    member __.Add request = agent.Post (Add request)

    /// <summary>
    /// Gets all requests for a specific datetime
    /// </summary>
    member __.GetFrame time = agent.PostAndReply (fun c -> GetFrame(time, c))

    /// <summary>
    /// Gets all requests for datetime range represented by a start date and an end date
    /// </summary>
    member __.GetRange startFrame endFrame = agent.PostAndReply (fun c -> GetRange(startFrame, endFrame, c))

/// <summary>
/// Contains helper methods to work with a cache
/// </summary>
module RequestCache =
    /// <summary>
    /// Gets all requests for the specified datetime
    /// </summary>
    let getRequestsAt (dateTime : DateTime) (cache : RequestCache) =
        cache.GetFrame <| dateTime.ToTimestamp()
    
    /// <summary>
    /// Gets all requests for the specified range of date starting from now
    /// </summary>
    let getRequestsByNow timeSpan (cache : RequestCache) =
        let endFrame = DateTime.Now.ToTimestamp()
        let startFrame = endFrame - timeSpan
        cache.GetRange startFrame endFrame
