namespace Logs

open System
open FSharpx.Collections
open FSharpx.Control

type CacheAction =
    | Add of Request
    | GetFrame of int64 * AsyncReplyChannel<Request seq>
    | GetRange of int64 * int64 * AsyncReplyChannel<Request seq>
    | Clear

type Cache = {
    TimeFrames : Map<int64, Request list> }

type RequestCache(lifetime) =
    let add request (frames : Map<int64, Request list>) =
        let timestamp = DateTime.Now.ToTimestamp()
        let requests =
            match frames.TryFind timestamp with
            | Some x -> request::x
            | None -> [request]
        frames |> Map.add timestamp requests
    
    let getFrame time (frames : Map<int64, Request list>) =
        match frames.TryFind time with
        | Some x -> x
        | None -> []
    
    let getRange startTime endTime (frames : Map<int64, Request list>) = seq {
        for i in startTime ..endTime do
            match frames.TryFind i with
            | Some x -> yield! x
            | None -> yield! [] }
    
    let clear (frames : Map<int64, Request list>) =
        let thresholdDate = DateTime.Now.AddSeconds(-lifetime).ToTimestamp()
        frames
        |> Seq.fold (fun (acc : Map<int64, Request list>) c -> 
            if c.Key < thresholdDate then
                acc.Remove c.Key
            else
                acc) frames

    let agent = Agent.Start <| fun inbox ->
        let rec loop (state : Cache) = async {
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

    let clearCache =
        let rec loop () = async {
            do! Async.Sleep 1000
            agent.Post Clear
            return! loop() }
        loop()

    do
        clearCache |> Async.Start

    member __.Add request = agent.Post (Add request)
    member __.GetFrame time = agent.PostAndReply (fun c -> GetFrame(time, c))
    member __.GetRange startFrame endFrame = agent.PostAndReply (fun c -> GetRange(startFrame, endFrame, c))

module RequestCache =
    let getRequestsAt (dateTime : DateTime) (cache : RequestCache) =
        cache.GetFrame <| dateTime.ToTimestamp()
    
    let getRequestsByNow timeSpan (cache : RequestCache) =
        let endFrame = DateTime.Now.ToTimestamp()
        let startFrame = endFrame - timeSpan
        cache.GetRange startFrame endFrame
