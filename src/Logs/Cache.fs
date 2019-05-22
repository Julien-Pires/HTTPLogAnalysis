namespace Logs

open System
open FSharpx.Collections

type CacheAction =
    | Add of Request
    | GetFrame of int64 * AsyncReplyChannel<Request seq>
    | GetRange of int64 * int64 * AsyncReplyChannel<Request seq>
    | Clear

type Cache = {
    TimeFrames : Map<int64, Request list> }

type RequestCache(lifetime) =
    let agent = Agent.Start <| fun inbox ->
        let rec loop (state : Cache) = async {
            let now = DateTimeOffset(DateTime.Now)

            let inline add request =
                let timestamp = now.ToUnixTimeSeconds()
                let requests =
                    match state.TimeFrames.TryFind timestamp with
                    | Some x -> request::x
                    | None -> [request]
                state.TimeFrames |> Map.add timestamp requests

            let inline getFrame time =
                match Map.tryFind time state.TimeFrames with
                | Some x -> x
                | None -> []

            let inline getRange startTime endTime = seq {
                for i in startTime ..endTime do
                    match Map.tryFind i state.TimeFrames with
                    | Some x -> yield! x
                    | None -> yield! [] }

            let clear () = 
                let thresholdDate = now.AddSeconds(-lifetime).ToUnixTimeSeconds()
                state.TimeFrames
                |> Seq.fold (fun acc c -> 
                    if c.Key <= thresholdDate then 
                        acc |> Map.remove c.Key
                    else 
                        acc) state.TimeFrames

            let! msg = inbox.Receive()
            match msg with
            | Add request -> 
                let requests = add request
                return! loop { TimeFrames = requests }
            | Clear -> 
                let requests = clear()
                return! loop { TimeFrames = requests }
            | GetFrame (time, reply) ->
                reply.Reply <| getFrame time
                return! loop state
            | GetRange (startTime, endTime, reply) ->
                reply.Reply <| getRange startTime endTime
                return! loop state }
        loop { TimeFrames = Map.empty }

    let clearOldRequests =
        let rec loop () = async {
            do! Async.Sleep 1000
            agent.Post Clear
            return! loop() }
        loop()

    do
        clearOldRequests |> Async.Start

    member __.Add request = agent.Post (Add request)
    member __.GetFrame time = agent.PostAndReply (fun c -> GetFrame(time, c))
    member __.GetRange startFrame endFrame = agent.PostAndReply (fun c -> GetRange(startFrame, endFrame, c))

module RequestCache =
    let getRequestsAt dateTime (cache : RequestCache) =
        cache.GetFrame <| DateTimeOffset(dateTime).ToUnixTimeSeconds()
    
    let getRequestsByNow timeSpan (cache : RequestCache) =
        let endFrame = DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()
        let startFrame = endFrame - timeSpan
        cache.GetRange startFrame endFrame
