using Apollo;
using Apollo.Configuration;
using Apollo.Messaging;
using Apollo.Messaging.WebApi;
using Apollo.Messaging.NATS;
using BlazorDemo.Components;
using BlazorDemo.Endpoints;

namespace BlazorDemo;

public static class Setup
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var config = ApolloConfig.Default;
        builder.Configuration.Bind(config);

        builder.Services.AddApollo(
            config,
            apollo =>
            {
                apollo.UseNats();
                apollo.WithEndpoints(endpoints => endpoints.AddEndpoint<TestEndpoint>());
            });

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.MapNatsEndpoints();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();
        return app;
    }
}