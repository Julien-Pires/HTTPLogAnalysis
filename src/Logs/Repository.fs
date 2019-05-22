namespace Logs

type RepositoryOperation<'a> =
    | Get of (string * AsyncReplyChannel<'a option>)
    | Add of (string * 'a) seq

type Repository<'a>() =
    let agent = Agent.Start(fun inbox ->
        let rec loop data = async {
            let! msg = inbox.Receive()
            match msg with
            | Get (key, reply) ->
                data |> Map.tryFind key |> reply.Reply
                return! loop data
            | Add values ->
                let newData =
                    values |> Seq.fold (fun acc (key, value) -> acc |> Map.add key value) data
                return! loop newData }

        loop Map.empty<string, 'a>)

    member __.Get key = agent.PostAndReply (fun c -> Get(key, c))

    member __.Add results = agent.Post <| Add results