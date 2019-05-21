namespace Logs

open System
open FSharpx.Collections

type CacheAction =
    | Add of Request
    | GetFrame of int64 * AsyncReplyChannel<Request seq>
    | GetRange of int64 * int64 * AsyncReplyChannel<Request seq>

type Cache = {
    Requests : Queue<Request>
    TimeFrames : Map<int64, Request list> }

type RequestCache() =
    let agent = Agent.Start(fun inbox ->
        let rec loop (state : Cache) = async {
            let! msg = inbox.Receive()
            match msg with
            | Add request ->
                let timestamp = DateTimeOffset(request.Date).ToUnixTimeSeconds()
                let requests =
                    match state.TimeFrames.TryFind timestamp with
                    | Some x -> request::x
                    | None -> [request]
                return! loop {
                    Requests = state.Requests.Conj request
                    TimeFrames = state.TimeFrames |> Map.add timestamp requests }
            | GetFrame (time, reply) ->
                let requests =
                    match Map.tryFind time state.TimeFrames with
                    | Some x -> x
                    | None -> []
                reply.Reply requests
                return! loop state
            | GetRange (startTime, endTime, reply) ->
                let requests = seq {
                    for i in startTime ..endTime do
                        match Map.tryFind i state.TimeFrames with
                        | Some x -> yield! x
                        | None -> yield! [] }
                reply.Reply requests
                return! loop state }
        loop { Requests = Queue.empty; TimeFrames = Map.empty })

    member __.Add request = agent.Post (Add request)
    member __.GetFrame time = agent.PostAndReply (fun c -> GetFrame(time, c))
    member __.GetRange startFrame endFrame = agent.PostAndReply (fun c -> GetRange(startFrame, endFrame, c))

module RequestCache =
    let getRequestsAt dateTime (cache : RequestCache) =
        let dateTime = DateTimeOffset(dateTime).ToUnixTimeSeconds()
        cache.GetFrame dateTime
    
    let getRequestsByNow timeSpan (cache : RequestCache) =
        let endFrame = DateTimeOffset(DateTime.Now).ToUnixTimeSeconds()
        let startFrame = endFrame - timeSpan
        cache.GetRange startFrame endFrame
