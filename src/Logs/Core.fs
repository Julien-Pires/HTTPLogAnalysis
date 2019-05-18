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