namespace Logs

/// <summary>
/// Represents a set of operation for a repository
/// </summary>
type RepositoryOperation<'a> =
    | Add of 'a
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
            | Add value ->
                return! loop (value::data) }

        loop List.empty<'a>)

    /// <summary>
    /// Gets the values of the repository
    /// </summary>
    member __.Get = agent.PostAndReply Get

    /// <summary>
    /// Adds a new value to the repository
    /// </summary>
    member __.Add value = agent.Post <| Add value

/// <summary>
/// Represents a set of operation for a keyed repository
/// </summary>
type KeyedRepositoryOperation<'a> =
    | Add of string * 'a
    | Get of string * AsyncReplyChannel<'a option>

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
            | Add (key, value) ->
                return! loop (data |> Map.add key value) }

        loop Map.empty<string, 'a>)

    /// <summary>
    /// Gets a value stored in the repository for the given key
    /// </summary>
    member __.Get key = agent.PostAndReply (fun c -> Get(key, c))

    /// <summary>
    /// Adds a new value to the repository
    /// </summary>
    member __.Add value = agent.Post <| Add value