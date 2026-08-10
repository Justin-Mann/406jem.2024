using Azure.Core.Serialization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Security.Claims;

namespace ResumeFunctions.Tests.Helpers;

public static class TestFunctionContextFactory
{
    /// <summary>
    /// A FunctionContext with a JSON serializer registered (needed by WriteAsJsonAsync /
    /// ReadFromJsonAsync in tests) and, optionally, an authenticated user pre-populated the
    /// same way JwtAuthenticationMiddleware would after validating a bearer token.
    /// </summary>
    public static FunctionContext Create(ClaimsPrincipal? authenticatedUser = null)
    {
        var services = new ServiceCollection();
        services.Configure<WorkerOptions>(opts => opts.Serializer = new JsonObjectSerializer());
        var context = Substitute.For<FunctionContext>();
        context.InstanceServices.Returns(services.BuildServiceProvider());

        var items = new Dictionary<object, object>();
        if (authenticatedUser is not null)
        {
            items[ResumeFunctions.Auth.Middleware.JwtAuthenticationMiddleware.ContextItemKey] = authenticatedUser;
        }
        context.Items.Returns(items);

        return context;
    }

    public static ClaimsPrincipal CreateUser(string username, string role)
    {
        var identity = new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Name, username), new Claim(ClaimTypes.Role, role) },
            authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }
}
