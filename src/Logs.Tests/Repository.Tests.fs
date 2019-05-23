namespace Logs.Tests

open Xunit
open Swensen.Unquote
open Logs

module Repository_Tests =
    [<Fact>]
    let ``get should return empty list when nothing has been added`` () =
        let sut = Repository()
        let result = async { return sut.Get } |> Async.RunSynchronously

        test <@ result = [] @>

    [<Theory>]
    [<InlineData(1)>]
    [<InlineData(10)>]
    [<InlineData(100)>]
    let ``get should return a list of items when they have been added`` elementCount =
        let expected = [for _ in 0 .. elementCount do yield "Foo"]
        let sut = Repository()
        let result = async {
            for _ in 0 .. elementCount do 
                sut.Add ["Foo"]
            return sut.Get } |> Async.RunSynchronously

        test <@ result = expected @>

module KeyedRepository_Tests =
    [<Fact>]
    let ``get should return None when no element with the given key exists`` () =
        let sut = KeyedRepository()
        let result = async { return sut.Get "Foo" } |> Async.RunSynchronously

        test <@ result.IsNone @>

    [<Fact>]
    let ``get should return item associated to the given key when item has been added`` () =
        let sut = KeyedRepository()
        let result = async {
            sut.Add [("Foo", 10)]
            return sut.Get "Foo" } |> Async.RunSynchronously

        test <@ result = Some 10 @>