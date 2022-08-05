## Dependencies
dotnet add package AWSSDK.S3 --version 3.7.9.24
dotnet add package Microsoft.Extensions.Configuration.Json
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.AspNetCore

## run
dotnet run -r win-x64

## build
dotnet publish -c Release -r win-x64