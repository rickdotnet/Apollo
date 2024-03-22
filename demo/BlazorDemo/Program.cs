using BlazorDemo;

var app = 
    WebApplication.CreateBuilder(args)
        .ConfigureServices()
        .ConfigurePipeline();

app.Run();