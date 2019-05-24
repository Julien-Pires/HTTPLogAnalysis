namespace Logs

open System

type Agent<'a> = MailboxProcessor<'a>

/// <summary>
/// Represents an HTTP access request
/// </summary>
type Request = {
    Address : string
    Date : DateTime
    User : string
    Sections : string List
    HTTPCode : int
    ResponseSize : int }

/// <summary>
/// Represents a collection of values for a single statistic
/// </summary>
type Statistic = {
    Values : Map<string, obj> }

/// <summary>
/// Represents a set of statistics
/// </summary>
type StatisticResult = {
    Name : string
    Result : Statistic list }