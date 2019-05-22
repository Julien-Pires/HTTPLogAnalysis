namespace Logs

type RepositoryOperation<'a> =
    | Add of 'a seq
    | Get of AsyncReplyChannel<'a list>

type Repository<'a>() =
    let agent = Agent.Start(fun inbox ->
        let rec loop data = async {
            let! msg = inbox.Receive()
            match msg with
            | Get reply ->
                reply.Reply data
                return! loop data
            | Add values ->
                let newData = values |> Seq.fold (fun acc value -> value::acc) data
                return! loop newData }

        loop List.empty<'a>)

    member __.Get = agent.PostAndReply Get
    member __.Add values = agent.Post <| Add values

type KeyedRepositoryOperation<'a> =
    | Add of (string * 'a) seq
    | Get of (string * AsyncReplyChannel<'a option>)

type KeyedRepository<'a>() =
    let agent = Agent.Start(fun inbox ->
        let rec loop data = async {
            let! msg = inbox.Receive()
            match msg with
            | Get (key, reply) ->
                data |> Map.tryFind key |> reply.Reply
                return! loop data
            | Add values ->
                let newData = values |> Seq.fold (fun acc (key, value) -> acc |> Map.add key value) data
                return! loop newData }

        loop Map.empty<string, 'a>)

    member __.Get key = agent.PostAndReply (fun c -> Get(key, c))
    member __.Add values = agent.Post <| Add values