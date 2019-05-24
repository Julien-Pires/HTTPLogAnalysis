namespace Logs

open System
open System.Globalization
open FParsec
open FSharpx.Collections

type UserState = unit
type Parser<'a> = Parser<'a, UserState>

/// <summary>
/// Represents a parser that transform string HTTP access request to request object
/// </summary>
module LogParser =
    let private ws = spaces
    let private noValue = pstring "-"
    let private ipDelimiter = skipChar '.'
    let private dateDelimiter = skipChar '/'
    let private timeDelimiter = skipChar ':'
    let private sectionDelimiter = skipChar '/'

    /// <summary>
    /// Executes eache parser in chain and execute the provided function with collected values
    /// </summary>
    let private pipe7 p1 p2 p3 p4 p5 p6 p7 f =
        p1 >>= fun x1 ->
         p2 >>= fun x2 ->
          p3 >>= fun x3 ->
           p4 >>= fun x4 ->
            p5 >>= fun x5 ->
             p6 >>= fun x6 ->
              p7 >>= fun x7 -> preturn (f x1 x2 x3 x4 x5 x6 x7)

    /// <summary>
    /// Parses a date with the format 'dd/MMM/yyyy'
    /// </summary>
    let private parseDate : Parser<_> =
        pipe3 pint32 
              (dateDelimiter >>. many1Chars (noneOf "/") .>> dateDelimiter) 
              pint32
              (fun day month year -> 
                (day, DateTime.ParseExact(month, "MMM", CultureInfo.InvariantCulture).Month, year))

    /// <summary>
    /// Parses a date with the format 'hh/mm/ss'
    /// </summary>
    let private parseTime : Parser<_> =
        pipe3 (pint32 .>> timeDelimiter)
              (pint32 .>> timeDelimiter)
              pint32
              (fun hours minutes seconds -> (hours, minutes, seconds))
        
    /// <summary>
    /// Parses a timezone with the format '+XX'/'-XX'
    /// </summary>
    let private parseTimeZone : Parser<_> = 
        anyOf "+-" .>>. pint32
        
    /// <summary>
    /// Parses a datetime with the format dd/MMM/yyy:hh/mm/ss -/+XX
    /// </summary>
    let private parseDateTime : Parser<_> =
        pipe3 parseDate 
              (timeDelimiter >>. parseTime)
              (spaces >>. parseTimeZone)
              (fun (day, month, year) (hours, minutes, seconds) (_, _) ->
                DateTime(year, month, day, hours, minutes, seconds))

    /// <summary>
    /// Parses an IPV4 address
    /// </summary>
    let private parseIP : Parser<_> =
        pipe4 (pint32 .>> ipDelimiter) 
              (pint32 .>> ipDelimiter) 
              (pint32 .>> ipDelimiter) 
              pint32
              (fun p1 p2 p3 p4 -> String.Format("{0}.{1}.{2}.{3}", p1, p2, p3, p4))
            
    /// <summary>
    /// Parses HTTP method
    /// </summary>
    let private parseMethod : Parser<_> = 
        skipString "GET" <|> skipString "POST"
            
    /// <summary>
    /// Parses a list of section
    /// </summary>
    let private parseSection : Parser<_> =
        many (sectionDelimiter >>. manyChars (noneOf "/ "))
            
    /// <summary>
    /// Parses HTTP version
    /// </summary>
    let private parseHTTPVersion : Parser<_> =
        skipString "HTTP/" >>. pint32 >>. skipChar '.' >>. pint32
            
    /// <summary>
    /// Parses an HTTP ident
    /// </summary>
    let private parseHTTPIdent : Parser<_> =
        noValue <|> manyChars (noneOf " ")

    /// <summary>
    /// Parses a username
    /// </summary>
    let private parseHTTPUser : Parser<_> =
        noValue <|> manyChars (noneOf " ")

    /// <summary>
    /// Parses an HTTP datetime
    /// </summary>
    let private parseHTTPDateTime : Parser<_> = 
        between (skipChar '[') (skipChar ']') parseDateTime

    /// <summary>
    /// Parses an HTTP query
    /// </summary>
    let private parseHTTPQuery : Parser<_> = 
        between (skipChar '"') (skipChar '"') (
            parseMethod
            >>. (ws >>. parseSection .>> ws) 
            .>> parseHTTPVersion)

    /// <summary>
    /// Parses an HTTP access request
    /// </summary>
    let private parseLog : Parser<_> =
        pipe7 (parseIP) 
              (ws >>. parseHTTPIdent .>> ws) 
              (ws >>. parseHTTPUser .>> ws) 
              (ws >>. parseHTTPDateTime .>> ws) 
              (ws >>. parseHTTPQuery .>> ws)
              (ws >>. pint32 .>> ws)
              (ws >>. pint32 .>> ws)
              (fun ip _ user date sections code size -> (ip, user, date, sections, code, size))

    /// <summary>
    /// Transforms an HTTP access request string to a request object
    /// </summary>
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