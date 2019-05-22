namespace Logs

open System
open System.Globalization
open FParsec
open FSharpx.Collections

type UserState = unit
type Parser<'a> = Parser<'a, UserState>

module LogParser =
    let private ws = spaces
    let private noValue = pstring "-"
    let private ipDelimiter = skipChar '.'
    let private dateDelimiter = skipChar '/'
    let private timeDelimiter = skipChar ':'
    let private sectionDelimiter = skipChar '/'

    let pipe7 p1 p2 p3 p4 p5 p6 p7 f =
        p1 >>= fun x1 ->
         p2 >>= fun x2 ->
          p3 >>= fun x3 ->
           p4 >>= fun x4 ->
            p5 >>= fun x5 ->
             p6 >>= fun x6 ->
              p7 >>= fun x7 -> preturn (f x1 x2 x3 x4 x5 x6 x7)

    let private parseDate : Parser<_> =
        pipe3 pint32 
              (dateDelimiter >>. many1Chars (noneOf "/") .>> dateDelimiter) 
              pint32
              (fun day month year -> 
                (day, DateTime.ParseExact(month, "MMM", CultureInfo.InvariantCulture).Month, year))

    let private parseTime : Parser<_> =
        pipe3 (timeDelimiter >>. pint32) 
              (timeDelimiter >>. pint32 .>> timeDelimiter) 
              pint32
              (fun hours minutes seconds -> (hours, minutes, seconds))
        
    let private parseTimeZone : Parser<_> = 
        anyOf "+-" .>>. pint32
        
    let private parseDateTime : Parser<_> =
        pipe3 parseDate 
              parseTime 
              (spaces >>. parseTimeZone)
              (fun (day, month, year) (hours, minutes, seconds) (_, _) ->
                DateTime(year, month, day, hours, minutes, seconds))

    let private parseIP : Parser<_> =
        pipe4 (pint32 .>> ipDelimiter) 
              (pint32 .>> ipDelimiter) 
              (pint32 .>> ipDelimiter) 
              pint32
              (fun p1 p2 p3 p4 -> String.Format("{0}.{1}.{2}.{3}", p1, p2, p3, p4))
            
    let private parseMethod : Parser<_> = 
        skipString "GET" <|> skipString "POST"
            
    let private parseSection : Parser<_> =
        many (sectionDelimiter >>. manyChars (noneOf "/ "))
            
    let private parseHTTPVersion : Parser<_> =
        pstring "HTTP/" >>. pint32 >>. pchar '.' >>. pint32
        
    let private parseQuery : Parser<_> =
        pipe3 parseMethod 
              (ws >>. parseSection .>> ws) 
              parseHTTPVersion 
              (fun _ sections _ -> sections)
            
    let private parseHTTPIdent : Parser<_> =
        noValue <|> manyChars (noneOf " ")

    let private parseHTTPUser : Parser<_> =
        noValue <|> manyChars (noneOf " ")

    let private parseHTTPDateTime : Parser<_> = 
        between (pchar '[') (pchar ']') parseDateTime

    let private parseHTTPQuery : Parser<_> = 
        between (pchar '"') (pchar '"') parseQuery

    let private parseHTTPCode : Parser<_> =
        pint32

    let private parseResponseSize : Parser<_> =
        pint32

    let private parseLog : Parser<_> =
        pipe7 (parseIP) 
              (ws >>. parseHTTPIdent .>> ws) 
              (ws >>. parseHTTPUser .>> ws) 
              (ws >>. parseHTTPDateTime .>> ws) 
              (ws >>. parseHTTPQuery .>> ws)
              (ws >>. parseHTTPCode .>> ws)
              (ws >>. parseResponseSize .>> ws)
              (fun ip _ user date sections code size -> (ip, user, date, sections, code, size))

    let parse logEntry =
        match run parseLog logEntry with
        | Success ((ip, user, date, sections, code, size), _, _) -> Some {
            Address = String.Intern(ip)
            User = String.Intern(user)
            Date = date
            Sections = sections |> List.map (fun c -> String.Intern("/" + c))
            HTTPCode = code
            ResponseSize = size }
        | _ -> None