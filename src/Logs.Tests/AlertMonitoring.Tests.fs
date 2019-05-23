namespace Logs.Tests

open Xunit
open Swensen.Unquote
open Logs

module AlertMonitoring_Tests =
    [<Fact>]
    let ``thresholdReached should return None when threshold is not reached`` () =
        let result = async {
            let sut = AlertMonitoring.avgThresholdReached 60 20 (>) "Count" |> Agent.Start
            let responses = [
                for _ in 0 .. 60 do
                    let stat = {
                        Name = "Foo"
                        Result = [{ Values = Map.ofList <| [("Count", 10 :> obj)] }] }
                    yield sut.PostAndReply (fun c -> (stat, c)) ]
            return responses } |> Async.RunSynchronously

        test <@ result |> List.forall Option.isNone @>

    [<Fact>]
    let ``thresholdReached should return Triggered when threshold is reached`` () =
        let result = async {
            let sut = AlertMonitoring.avgThresholdReached 60 10 (>) "Count" |> Agent.Start
            let responses = [
                for _ in 0 .. 60 do
                    let stat = {
                        Name = "Foo"
                        Result = [{ Values = Map.ofList <| [("Count", 20 :> obj)] }] }
                    yield sut.PostAndReply (fun c -> (stat, c)) ]
            return responses } |> Async.RunSynchronously

        test <@ result |> List.contains (Some Triggered) @>

    [<Fact>]
    let ``thresholdReached should return Cleared when threshold is no more reached`` () =
        let result = async {
            let sut = AlertMonitoring.avgThresholdReached 60 10 (>) "Count" |> Agent.Start
            let responses = [
                for _ in 0 .. 60 do
                    let stat = {
                        Name = "Foo"
                        Result = [{ Values = Map.ofList <| [("Count", 20 :> obj)] }] }
                    yield sut.PostAndReply (fun c -> (stat, c))
                for _ in 0 .. 60 do
                    let stat = {
                        Name = "Foo"
                        Result = [{ Values = Map.ofList <| [("Count", 5 :> obj)] }] }
                    yield sut.PostAndReply (fun c -> (stat, c)) ]
            return responses } |> Async.RunSynchronously

        test <@ result |> List.choose id
                       |> (=) [Triggered; Cleared] @>