using Apollo.Messaging.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Messaging.WebApi;

public static class Setup
{
    public static IEndpointConventionBuilder MapNatsEndpoints(this IEndpointRouteBuilder routeBuilder, string baseRoute = "/endpoints")
    {
        var endpointRegistry = routeBuilder.ServiceProvider.GetRequiredService<IEndpointRegistry>();
        var endpoints = endpointRegistry.GetEndpointRegistrations(x => x.Config.IsRemoteEndpoint);

        var group = routeBuilder.MapGroup(baseRoute);
        foreach (var endpoint in endpoints)
        {
            foreach (var (subject, messageType) in endpoint.SubjectMapping)
            {
                group.MapPost(subject, async (HttpRequest request, IPublisherFactory publisherFactory, CancellationToken cancellationToken) =>
                {
                    if (!request.HasJsonContentType())
                        return Results.BadRequest("Unsupported content type.");

                    var publisher = publisherFactory.CreatePublisher(subject);
                    var content = await request.ReadFromJsonAsync(messageType, cancellationToken);
                    if (content == null)
                        return Results.BadRequest("Invalid request content.");

                    try
                    {
                        if (messageType.IsRequest())
                        {
                            var response = await publisher.SendRequestAsync(subject, content, cancellationToken);
                            return Results.Ok(response);
                        }

                        await publisher.SendObjectAsync(subject, content, cancellationToken);
                        return Results.Accepted();
                    }
                    catch (Exception ex)
                    {
                        return Results.Problem(ex.Message);
                    }
                });
            }
        }

        return group;
    }

    private static bool HasJsonContentType(this HttpRequest request) 
        => request.ContentType?.StartsWith("application/json", StringComparison.OrdinalIgnoreCase) ?? false;
}