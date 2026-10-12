using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using TfvcMcpServer;

namespace TfvcMcpServer.Tests;

public class AzureDevOpsClientTests
{
    private const string Organization = "example-org";
    private const string Project = "example project";

    [Fact]
    public async Task QueryWorkItems_PostsWiqlAndUsesProjectApi()
    {
        var handler = new RecordingHandler();
        var client = Create(handler);

        await client.QueryWorkItemsAsync("SELECT [System.Id] FROM WorkItems", default);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Contains("/example%20project/_apis/wit/wiql", handler.Uri!.AbsoluteUri);
        Assert.Contains("api-version=7.1", handler.Uri.Query);
        Assert.Equal("application/json", handler.ContentType);
        Assert.Contains("\"query\":\"SELECT [System.Id] FROM WorkItems\"", handler.Body);
        Assert.Equal("Basic", handler.AuthorizationScheme);
    }

    [Fact]
    public async Task GetWorkItem_RequestsExpandedWorkItem()
    {
        var handler = new RecordingHandler();

        await Create(handler).GetWorkItemAsync(42, default);

        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.Contains("/wit/workitems/42", handler.Uri!.AbsoluteUri);
        Assert.DoesNotContain("$expand", handler.Uri.Query);
    }

    [Fact]
    public async Task ListWorkItemTypes_UsesWorkItemTypesEndpoint()
    {
        var handler = new RecordingHandler();

        await Create(handler).ListWorkItemTypesAsync(default);

        Assert.Contains("/wit/workitemtypes", handler.Uri!.AbsoluteUri);
    }

    [Fact]
    public async Task PipelineAndBuildCalls_UseExpectedEndpoints()
    {
        var handler = new RecordingHandler();
        var client = Create(handler);

        await client.ListPipelinesAsync(default);
        Assert.Contains("/_apis/pipelines?", handler.Uri!.AbsoluteUri);
        await client.ListPipelineRunsAsync(7, default);
        Assert.Contains("/pipelines/7/runs", handler.Uri!.AbsoluteUri);
        await client.GetPipelineRunAsync(7, 9, default);
        Assert.Contains("/pipelines/7/runs/9", handler.Uri!.AbsoluteUri);
        await client.ListBuildsAsync(500, default);
        Assert.Contains("/build/builds", handler.Uri!.AbsoluteUri);
        Assert.Contains("$top=100", handler.Uri.Query);
        await client.ListBranchesAsync("repo", default);
        Assert.Contains("/git/repositories/repo/refs", handler.Uri!.AbsoluteUri);
    }

    [Fact]
    public async Task CreateWorkItem_PostsTitleAndDescriptionAsJsonPatch()
    {
        var handler = new RecordingHandler();

        await Create(handler).CreateWorkItemAsync("User Story", "A title", "Details", default);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Contains("/wit/workitems/$User%20Story", handler.Uri!.AbsoluteUri);
        Assert.Equal("application/json-patch+json", handler.ContentType);
        Assert.Contains("\"path\":\"/fields/System.Title\",\"value\":\"A title\"", handler.Body);
        Assert.Contains("\"path\":\"/fields/System.Description\",\"value\":\"Details\"", handler.Body);
    }

    [Fact]
    public async Task CreateWorkItem_OmitsEmptyDescription()
    {
        var handler = new RecordingHandler();

        await Create(handler).CreateWorkItemAsync("Task", "A title", null, default);

        Assert.Contains("\"/fields/System.Title\"", handler.Body);
        Assert.DoesNotContain("System.Description", handler.Body);
    }

    [Fact]
    public async Task UpdateWorkItem_PatchesRequestedItem()
    {
        var handler = new RecordingHandler();

        await Create(handler).UpdateWorkItemAsync(42, """[{"op":"add","path":"/fields/System.Title","value":"Updated"}]""", default);

        Assert.Equal(HttpMethod.Patch, handler.Method);
        Assert.Contains("/wit/workitems/42", handler.Uri!.AbsoluteUri);
        Assert.Equal("application/json-patch+json", handler.ContentType);
        Assert.Contains("\"Updated\"", handler.Body);
    }

    [Fact]
    public async Task UpdateWorkItem_RejectsNonArrayPatch()
    {
        var client = Create(new RecordingHandler());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            client.UpdateWorkItemAsync(42, """{"op":"add"}""", default));
    }

    [Fact]
    public async Task UpdateWorkItem_RejectsMalformedJson()
    {
        var client = Create(new RecordingHandler());

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            client.UpdateWorkItemAsync(42, "{", default));

        Assert.Contains("not valid JSON", error.Message);
    }

    [Fact]
    public async Task ListAndGetRepositories_UseGitEndpoints()
    {
        var handler = new RecordingHandler();
        var client = Create(handler);

        await client.ListRepositoriesAsync(default);
        Assert.Contains("/git/repositories", handler.Uri!.AbsoluteUri);

        await client.GetRepositoryAsync("repo-id", default);
        Assert.Contains("/git/repositories/repo-id", handler.Uri!.AbsoluteUri);

        await client.GetRepositoryAsync("folder/repo name", default);
        Assert.Contains("folder%2Frepo%20name", handler.Uri!.AbsoluteUri);
    }

    [Fact]
    public async Task FailedRequest_IncludesStatusAndResponseBody()
    {
        var handler = new RecordingHandler(HttpStatusCode.Forbidden, "access denied");

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(handler).ListRepositoriesAsync(default));

        Assert.Contains("403", error.Message);
        Assert.Contains("access denied", error.Message);
    }

    [Fact]
    public async Task FailedRequest_TruncatesLongResponseBody()
    {
        var handler = new RecordingHandler(HttpStatusCode.InternalServerError, new string('x', 501));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(handler).ListRepositoriesAsync(default));

        Assert.EndsWith("...[truncated]", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("<set-in-appsettings.Local.json>")]
    public async Task Request_WithoutConfiguredPat_FailsClearly(string pat)
    {
        var handler = new RecordingHandler();
        var client = Create(handler, pat);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.ListRepositoriesAsync(default));

        Assert.Contains("AzureDevOps:Pat is not configured", error.Message);
        Assert.Null(handler.Uri);
    }

    private static AzureDevOpsClient Create(RecordingHandler handler, string pat = "test-pat") =>
        new(Options.Create(new AzureDevOpsOptions
        {
            Organization = Organization,
            Project = Project,
            Pat = pat
        }), new HttpClient(handler));

    private sealed class RecordingHandler(HttpStatusCode status = HttpStatusCode.OK, string responseBody = "{}") : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? Uri { get; private set; }
        public string? ContentType { get; private set; }
        public string Body { get; private set; } = "";
        public string? AuthorizationScheme { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            Uri = request.RequestUri;
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            AuthorizationScheme = request.Headers.Authorization?.Scheme;

            return new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
