namespace HTTPAnalysis.Monitoring

open System
open System.Globalization
open FParsec

type UserState = unit
type Parser<'a> = Parser<'a, UserState>

module LogParser =
    let private ws = spaces
    let private ipDelimiter = skipChar '.'
    let private dateDelimiter = skipChar '/'
    let private timeDelimiter = skipChar ':'
    let private sectionDelimiter = skipChar '/'

    let private parseDate : Parser<_> =
        pipe3 pint32 (dateDelimiter >>. many1Chars (noneOf "/") .>> dateDelimiter) pint32 (
            fun day month year -> 
                (day, DateTime.ParseExact(month, "MMM", CultureInfo.InvariantCulture).Month, year))

    let private parseTime : Parser<_> =
        pipe3 (timeDelimiter >>. pint32) (timeDelimiter >>. pint32 .>> timeDelimiter) pint32 (
            fun hours minutes seconds -> 
                (hours, minutes, seconds))
        
    let private parseTimeZone : Parser<_> = 
        anyOf "+-" .>>. pint32
        
    let private parseDateTime : Parser<_> =
        pipe3 parseDate parseTime (spaces >>. parseTimeZone) (
            fun (day, month, year) (hours, minutes, seconds) (diff, zone) ->
                DateTime(year, month, day, hours, minutes, seconds))

    let private parseIP : Parser<_> =
        pipe4 (pint32 .>> ipDelimiter) (pint32 .>> ipDelimiter) (pint32 .>> ipDelimiter) pint32 (
            fun p1 p2 p3 p4 -> 
                String.Format("{0}.{1}.{2}.{3}", p1, p2, p3, p4))
            
    let private parseMethod : Parser<_> = 
        skipString "GET" <|> skipString "POST"
            
    let private parseSection : Parser<_> =
        many (sectionDelimiter >>. many1Chars (noneOf "/ "))
            
    let private parseHTTPVersion : Parser<_> =
        pstring "HTTP/" >>. pint32 >>. pchar '.' >>. pint32
        
    let private parseQuery : Parser<_> =
        pipe3 parseMethod (ws >>. parseSection .>> ws) parseHTTPVersion (
            fun _ sections _ -> sections)
            
    let private parseHTTPIdent : Parser<_> =
        pchar '-'

    let private parseHTTPUser : Parser<_> =
        manyChars (noneOf " ")

    let private parseHTTPDateTime : Parser<_> = 
        between (pchar '[') (pchar ']') parseDateTime

    let private parseHTTPQuery : Parser<_> = 
        between (pchar '"') (pchar '"') parseQuery

    let private parseLog : Parser<_> =
        pipe5 (parseIP .>> ws) (parseHTTPIdent .>> ws) (parseHTTPUser .>> ws) (parseHTTPDateTime .>> ws) parseHTTPQuery (
            fun ip _ user date sections ->
                (ip, user, date, sections))

    let parse logEntry =
        run parseLog logEntry