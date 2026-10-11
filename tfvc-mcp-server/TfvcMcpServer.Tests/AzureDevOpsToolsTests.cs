using System.Net;
using System.Reflection;
using Microsoft.Extensions.Options;
using TfvcMcpServer;

namespace TfvcMcpServer.Tests;

public class AzureDevOpsToolsTests
{
    [Theory]
    [InlineData(nameof(AzureDevOpsTools.QueryWorkItems), "query_work_items")]
    [InlineData(nameof(AzureDevOpsTools.GetWorkItem), "get_work_item")]
    [InlineData(nameof(AzureDevOpsTools.ListWorkItemTypes), "list_work_item_types")]
    [InlineData(nameof(AzureDevOpsTools.CreateWorkItem), "create_work_item")]
    [InlineData(nameof(AzureDevOpsTools.UpdateWorkItem), "update_work_item")]
    [InlineData(nameof(AzureDevOpsTools.ListRepositories), "list_repositories")]
    [InlineData(nameof(AzureDevOpsTools.GetRepository), "get_repository")]
    public void Tool_HasExpectedMcpName(string methodName, string expectedName)
    {
        var method = typeof(AzureDevOpsTools).GetMethod(methodName)!;
        var toolAttribute = method.GetCustomAttributesData()
            .Single(attribute => attribute.AttributeType.Name == "McpServerToolAttribute");
        var name = toolAttribute.NamedArguments.Single(argument => argument.MemberName == "Name").TypedValue.Value;

        Assert.Equal(expectedName, name);
    }

    [Fact]
    public async Task Tools_ForwardArgumentsToAzureDevOpsClient()
    {
        var handler = new RecordingHandler();
        var client = new AzureDevOpsClient(
            Options.Create(new AzureDevOpsOptions { Pat = "test-pat" }),
            new HttpClient(handler));
        var tools = new AzureDevOpsTools(client);

        await tools.QueryWorkItems("SELECT [System.Id] FROM WorkItems");
        Assert.EndsWith("/wit/wiql", handler.Uri!.AbsolutePath);

        await tools.GetWorkItem(12);
        Assert.EndsWith("/wit/workitems/12", handler.Uri!.AbsolutePath);

        await tools.ListWorkItemTypes();
        Assert.EndsWith("/wit/workitemtypes", handler.Uri!.AbsolutePath);

        await tools.CreateWorkItem("Task", "title", "description");
        Assert.EndsWith("/wit/workitems/$Task", handler.Uri!.AbsolutePath);

        await tools.UpdateWorkItem(12, """[{"op":"add","path":"/fields/System.Title","value":"updated"}]""");
        Assert.EndsWith("/wit/workitems/12", handler.Uri!.AbsolutePath);

        await tools.ListRepositories();
        Assert.EndsWith("/git/repositories", handler.Uri!.AbsolutePath);

        await tools.GetRepository("repo");
        Assert.EndsWith("/git/repositories/repo", handler.Uri!.AbsolutePath);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Uri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
