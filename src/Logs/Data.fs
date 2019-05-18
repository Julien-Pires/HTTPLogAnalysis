namespace Logs

open System

type Agent<'a> = MailboxProcessor<'a>

type RequestMethod =
    | POST
    | GET

type Request = {
    Address : string
    Date : DateTime
    User : string
    Method : RequestMethod
    Sections : string list
    StatusCode : int16
    ResponseSize : int }