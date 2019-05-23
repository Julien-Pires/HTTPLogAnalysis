// ----------------------------------------------------------------------------
// F# async extensions (Observable.fs)
// (c) Tomas Petricek, Phil Trelford, and Ryan Riley, 2011-2012, Available under Apache 2.0 license.
// ----------------------------------------------------------------------------
#nowarn "40"
namespace FSharpx.Control

open System
open System.Threading

// ----------------------------------------------------------------------------

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

module Async =
    let synchronize f = 
      let ctx = System.Threading.SynchronizationContext.Current 
      f (fun g ->
        let nctx = System.Threading.SynchronizationContext.Current 
        if ctx <> null && ctx <> nctx then ctx.Post((fun _ -> g()), null)
        else g() )

    let AwaitObservable(observable : IObservable<'T1>) =
        let removeObj : IDisposable option ref = ref None
        let removeLock = new obj()
        let setRemover r = 
            lock removeLock (fun () -> removeObj := Some r)
        let remove() =
            lock removeLock (fun () ->
                match !removeObj with
                | Some d -> removeObj := None
                            d.Dispose()
                | None   -> ())
        synchronize (fun f ->
        let workflow =
            Async.FromContinuations((fun (cont,econt,ccont) ->
                let rec finish cont value =
                    remove()
                    f (fun () -> cont value)
                setRemover <|
                    observable.Subscribe
                        ({ new IObserver<_> with
                            member x.OnNext(v) = finish cont v
                            member x.OnError(e) = finish econt e
                            member x.OnCompleted() =
                                let msg = "Cancelling the workflow, because the Observable awaited using AwaitObservable has completed."
                                finish ccont (new System.OperationCanceledException(msg)) })
                () ))
        async {
            let! cToken = Async.CancellationToken
            let token : CancellationToken = cToken
            use registration = token.Register((fun _ -> remove()), null)
            return! workflow
        })