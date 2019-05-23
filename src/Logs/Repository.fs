namespace Logs

/// <summary>
/// Represents a set of operation for a repository
/// </summary>
type RepositoryOperation<'a> =
    | Add of 'a seq
    | Get of AsyncReplyChannel<'a list>

/// <summary>
/// Represents a synchronized repository that stores values as a single list
/// </summary>
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

    /// <summary>
    /// Gets the values of the repository
    /// </summary>
    member __.Get = agent.PostAndReply Get

    /// <summary>
    /// Adds a new value to the repository
    /// </summary>
    member __.Add values = agent.Post <| Add values

/// <summary>
/// Represents a set of operation for a keyed repository
/// </summary>
type KeyedRepositoryOperation<'a> =
    | Add of (string * 'a) seq
    | Get of (string * AsyncReplyChannel<'a option>)

/// <summary>
/// Represents a synchronized repository that stores values as a key value pair
/// </summary>
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

    /// <summary>
    /// Gets a value stored in the repository for the given key
    /// </summary>
    member __.Get key = agent.PostAndReply (fun c -> Get(key, c))

    /// <summary>
    /// Adds a new value to the repository
    /// </summary>
    member __.Add values = agent.Post <| Add values