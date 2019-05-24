# HTTP Log Monitoring

## Description

The solution is splitted into four projects:

- Logs: This project contains modules and methods used by the log monitoring application. Those modules are:
  - IO: Read and write to a file continuously
  - Parser: Contains methods to parse and transform string HTTP log to object representation
  - Cache: Contains a cache for storing request and managing their lifetimes.
  - Statistics: Contains an agent that will periodically computes statistics.
  - Alerts: Contains an agent that monitor the statistics and raise alerts according to some rules.
  
- Logs.Console: This project is the console application that display information about the monitoring of the access.log file. By default, the application monitor following elements:
  - Statistics:
    - Number of request for the previous elapsed second
    - Number of request for the last 10 seconds
    - Number of request with error for the last 10 seconds (HTTP code > 299)
    - Most hit section for the last 10 seconds
    - Most active user for the last 10 seconds
  - Alerts:
    - Traffic exceeded: Monitor the average number of requests for the last 2 minutes and checks that it didn't go above 10
    - No traffic detected: Monitor the average number of requests for the last minute and checks that it didn't equal to 0
- Logs.Tests: This project contains unit tests.
  
- Logs.Server: This project is a console application that simulates request. It fills the access.log file with new request at a specified rate. This console can be used if you don't have any server with real log. The default path is 'temp/access.log' for writing logs but you can override it:

    ``` bash
    dotnet Logs.Server.dll -f:/user/logs/temp/access.log
    ```

## How to use

To run the application you must execute the project "Logs.Console". This project is a .NET Core project so you must have the .NET Core runtime installed on your machine. To launch the project you can use the following command from a command prompt (you must be in the folder that contains the project):

``` bash
dotnet Logs.Console.dll
```

or

``` bash
dotnet Logs.Console.dll -f:/user/logs/temp/access.log
```

The parameter '-f:' allow to override the path for the access.log file. By default it's '/temp/access.log'.

A docker image is also available in the folder 'src'. You can run following commands to execute the application:

``` bash
docker build -t logmonitor .
docker run -t -v c:/temp/access.log:/temp/access.log --rm logmonitor
```

## Improvements

There are many ways the application can be improved:

- Logs project:
  - I/O: Better handles errors when a file or folder doesn't exist in a more transparent way for the consumer of read and write method.

  - Parser:
    - The parser actually parse a single line of HTTP request in a access.log file. The parser could be improved to extract more information (e.g: User agent).
    - Performance of the parser must be improved, when testing with a huge number of requests, the parser was the hot point for performance drop.

  - Statistic agent: It computes statistics at a specified refresh rate (default: 1sec). To do this, the agent performs asynchronous task and sleep for the specified refresh rate. It would be better to have a more passive way for waiting. For example, the statistic agent is notified when new requests are available for the previous second. It would avoid it to try computing something even if no requests has been received.

  - Alerts agent: The alert agent listens for new computed statistics to check for alerts. It would be interesting to make an alert agent able to check for alert on statistics or requests or both. This will extend possibility on what to alert on.

- Logs console project:
  
  - Configuration: Statistics computation, alerts and display is customizable through the Configuration.fs file. A good improvement would be to read this configuration from a file (e.g: JSON, XML,...). This will allow to avoid compiling and publishing the application each time we want to add or remove something. We will only have to edit the file and run the application.

  - Display: It could be good to have some colors, especially for the alerts. It will help to faster catch triggered alerts.