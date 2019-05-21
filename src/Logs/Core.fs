namespace Logs

open System

type ObservableSource<'a>() =
    let subscribers = ref (Map.empty : Map<int, IObserver<'a>>)
    let count = ref 0

    let obs = {
        new IObservable<'a> with 
            member __.Subscribe(obs) =
                let key = 
                    lock subscribers (fun () -> 
                        let key = !count
                        count := !count + 1
                        subscribers := subscribers.Value.Add(key, obs)
                        key)
                { new IDisposable with
                    member __.Dispose() = 
                        lock subscribers (fun () ->
                            subscribers := subscribers.Value.Remove(key)) } }

    member __.AsObservable = obs

    member __.OnNext value =
        !subscribers
        |> Seq.iter (fun (KeyValue(_, sub)) ->
            sub.OnNext(value))

type Timer(target) =
    let locker = obj()
    let mutable remaining = target
    let mutable lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()

    member __.IsCompleted with get() = remaining <= 0

    member __.Update () =
        lock locker (fun _ ->
            let tick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            remaining <- remaining - int(tick - lastTick)
            lastTick <- tick)

    member __.Reset () =
        lock locker (fun _ ->
            remaining <- target
            lastTick <- DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())