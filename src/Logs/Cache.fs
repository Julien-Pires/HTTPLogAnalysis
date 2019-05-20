namespace Logs

open System
open FSharpx.Collections

type CacheAction =
    | Add of Request
    | GetFrame of int64 * AsyncReplyChannel<Request list option>
    | GetRange of int64 * int64 * AsyncReplyChannel<Request list option>

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
                reply.Reply <| Map.tryFind time state.TimeFrames
                return! loop state
            | GetRange (startTime, endTime, reply) ->
                reply.Reply <| None
                return! loop state }
        loop { Requests = Queue.empty; TimeFrames = Map.empty })

    member __.Add request = agent.Post (Add request)
    member __.GetFrame time = agent.PostAndReply (fun c -> GetFrame(time, c))

module RequestCache =
    let getRequestsAt dateTime (cache : RequestCache) =
        let dateTime = DateTimeOffset(dateTime).ToUnixTimeSeconds()
        match cache.GetFrame dateTime with
        | Some x -> x
        | None -> []
    
    let getRequestsByNow timeSpan cache = []
        //let datetime = DateTime.Now.TrimMilliseconds().AddSeconds(-timeSpan)
        //cache |> Seq.takeWhile(fun c -> c.Date >= datetime)
