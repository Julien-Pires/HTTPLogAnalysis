﻿namespace Logs

open System

type Agent<'a> = MailboxProcessor<'a>

type Request = {
    Address : string
    Date : DateTime
    User : string
    Sections : string List }

type Statistic = {
    Values : Map<string, obj> }

type StatisticResult = {
    Name : string
    Result : Statistic list }