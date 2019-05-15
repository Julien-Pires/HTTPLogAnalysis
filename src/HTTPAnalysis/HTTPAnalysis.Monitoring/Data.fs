namespace HTTPAnalysis.Monitoring

open System

type RequestMethod =
    | POST
    | GET

type Request = {
    Address : string
    Method : RequestMethod
    Section : string 
    Date : DateTime
    StatusCode : int16
    ResponseSize : int }