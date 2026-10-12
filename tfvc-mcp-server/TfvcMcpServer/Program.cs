using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TfvcMcpServer;

// Stdio (default) is launched by the MCP client. HTTP is enabled with --http or Mcp__Transport=http (Docker).
var useHttp = args.Contains("--http") ||
    string.Equals(Environment.GetEnvironmentVariable("Mcp__Transport"), "http", StringComparison.OrdinalIgnoreCase);

if (!useHttp)
{
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    });
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
    builder.Services.Configure<AzureDevOpsOptions>(builder.Configuration.GetSection("AzureDevOps"));
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
    builder.Services.AddSingleton<TfvcClient>();
    builder.Services.AddHttpClient<AzureDevOpsClient>();
    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly();
    await builder.Build().RunAsync();
    return;
}

var web = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args.Where(a => a != "--http").ToArray(),
    ContentRootPath = AppContext.BaseDirectory
});
web.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false);
web.Configuration.AddEnvironmentVariables();
web.Services.Configure<AzureDevOpsOptions>(web.Configuration.GetSection("AzureDevOps"));
web.Services.AddSingleton<TfvcClient>();
web.Services.AddHttpClient<AzureDevOpsClient>();
web.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

// Keys: Mcp:ApiKeys:0.. in config, or comma-separated Mcp__ApiKeys env var.
var apiKeys = ApiKeyMiddleware.ParseKeys(
    web.Configuration.GetSection("Mcp:ApiKeys").GetChildren().Select(c => c.Value)
        .Append(web.Configuration["Mcp:ApiKeys"]));
if (apiKeys.Length == 0)
    throw new InvalidOperationException("HTTP mode requires at least one API key. Set Mcp__ApiKeys (comma-separated) or Mcp:ApiKeys in appsettings.Local.json.");

var app = web.Build();
app.UseMiddleware<ApiKeyMiddleware>((IReadOnlyCollection<string>)apiKeys);
app.MapGet("/health", () => Results.Ok("ok"));
app.MapMcp("/mcp");
await app.RunAsync();
