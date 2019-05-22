namespace Logs.Tests

open System
open Xunit
open Swensen.Unquote
open Logs

module LogParser_Tests =
    [<Theory>]
    [<InlineData("127.0.0.1 - - [10/Oct/2010:13:55:36 -0700] \"GET / HTTP/1.0\" 200 2326")>]
    [<InlineData("127.0.0.1 - - [10/Oct/2010:13:55:36 -0700] \"GET /apache_pb.gif HTTP/1.2\" 200 2326")>]
    [<InlineData("127.0.0.1 - frank [10/Oct/2000:13:55:36 -0700] \"GET /apache_pb.gif HTTP/1.0\" 200 2326")>]
    [<InlineData("127.0.0.1 - - [25/Oct/2016:14:49:34 +0200] \"GET /favicon.ico HTTP/2.1\" 404 571 \"http://localhost:8080/\" \"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36\"")>]
    [<InlineData("127.0.0.1 - frank [25/Oct/2016:14:49:34 +0200] \"GET /favicon.ico HTTP/2.1\" 404 571 \"http://localhost:8080/\" \"Mozilla/5.0 (Macintosh; Intel Mac OS X 10_12_0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/54.0.2840.59 Safari/537.36\"")>]
    let ``Parse should return a request when log entry is valid`` log =
        test <@ LogParser.parse log |> Option.isSome @>

    [<Theory>]
    [<InlineData("127.A.0.1 - - [10/Oct/2010:13:55:36 -0700] \"GET / HTTP/1.0\" 200 2326")>]
    [<InlineData("- - [10/Oct/2010:13:55:36 -0700] 127.0.0.1 \"GET / HTTP/1.0\" 200 2326")>]
    [<InlineData("127.0.0.1 - - [10/Oct/2010:13:55:36 -0700] GET / HTTP/1.0 200 2326")>]
    [<InlineData("127.0.0.1 - - [10/Oct/2010:13:55:36 -0700] \"GET / HTTP/1.0\" AAAA BBBB")>]
    let ``Parse should return none when log entry is invalid`` log =
        test <@ LogParser.parse log |> Option.isNone @>

    [<Theory>]
    [<InlineData([|
        "127.0.0.1 - - [10/Oct/2010:13:55:36 -0700] \"GET / HTTP/1.0\" 200 2326" :> obj
        "127.0.0.1" :> obj
        "-" :> obj
        "10/10/2010 13:55:36" :> obj
        "/" :> obj
        200 :> obj
        2326 :> obj|])>]
    [<InlineData([|
        "255.0.255.200 - John [10/Oct/2010:13:55:36 -0700] \"GET /Foo HTTP/1.0\" 200 2326" :> obj
        "255.0.255.200" :> obj
        "John" :> obj
        "10/10/2010 13:55:36" :> obj
        "/Foo" :> obj
        200 :> obj
        2326 :> obj|])>]
    let ``Parse should return expected values when log entry is valid`` log ip user date section code size = 
        let expected = {
            Address = ip
            User = user
            Date = DateTime.Parse(date)
            Sections = [section]
            HTTPCode = code
            ResponseSize = size }
        let result = LogParser.parse log |> fun c -> c.Value

        test <@ result = expected @>