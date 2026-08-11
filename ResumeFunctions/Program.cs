using Azure.Core.Serialization;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ResumeFunctions.Auth.Cookies;
using ResumeFunctions.Auth.Identity;
using ResumeFunctions.Auth.Middleware;
using ResumeFunctions.Auth.Security;
using ResumeFunctions.Auth.Seeding;
using ResumeFunctions.Auth.Storage;
using ResumeFunctions.Auth.Tokens;
using System.Text.Json;

var builder = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(workerBuilder =>
    {
        workerBuilder.UseMiddleware<JwtAuthenticationMiddleware>();
        workerBuilder.UseMiddleware<CsrfProtectionMiddleware>();
    })
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorkerOptions>(options =>
        {
            options.Serializer = new JsonObjectSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        });

        // Reuses the storage account the Functions host already provisions for itself
        // (AzureWebJobsStorage) instead of adding a second, paid resource just for user data.
        services.AddSingleton(_ =>
            new TableServiceClient(context.Configuration["AzureWebJobsStorage"] ?? "UseDevelopmentStorage=true"));

        // Same storage account/connection string as TableServiceClient above (#28's "no new
        // Azure resource" reasoning, reused for #29's blob container).
        services.AddSingleton(_ =>
            new BlobServiceClient(context.Configuration["AzureWebJobsStorage"] ?? "UseDevelopmentStorage=true"));

        services.AddSingleton<IUserStore, TableUserStore>();
        services.AddSingleton<ITestimonialStore, TableTestimonialStore>();
        services.AddSingleton<IResumeStore, TableResumeStore>();
        services.AddSingleton<IResumeBlobStore, BlobResumeStore>();
        services.AddSingleton<IProjectListingStore, TableProjectListingStore>();
        services.AddSingleton<ISiteConfigStore, TableSiteConfigStore>();
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IIdentityProvider, LocalPasswordIdentityProvider>();
        services.AddSingleton<IAuthTokenService, JwtAuthTokenService>();
        services.AddSingleton<AuthCookieService>();
        services.AddHostedService<AdminAccountSeeder>();
    });

builder.Build().Run();
