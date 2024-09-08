using ConsoleDemo.Demo;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(LogEventLevel.Debug,
        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .CreateLogger();

//await OriginalDemo.Demo();
//await Direct.Demo();
await HostDemo.Demo(true);
//await AsbDemo.Demo();