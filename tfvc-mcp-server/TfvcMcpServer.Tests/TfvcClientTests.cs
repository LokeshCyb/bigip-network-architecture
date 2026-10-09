using Microsoft.Extensions.Options;
using TfvcMcpServer;

namespace TfvcMcpServer.Tests;

public class TfvcClientTests
{
    private const string Root = "$/MG-Grp/Source/Main-Dotnet-Upgrade";

    private static TfvcClient Create(string pat = "test-pat", string root = Root) =>
        new(Options.Create(new AzureDevOpsOptions { Pat = pat, RootPath = root }));

    [Fact]
    public void Resolve_NullOrEmpty_ReturnsRoot()
    {
        var c = Create();
        Assert.Equal(Root, c.Resolve(null));
        Assert.Equal(Root, c.Resolve("  "));
    }

    [Fact]
    public void Resolve_RelativePath_IsPrefixedWithRoot() =>
        Assert.Equal(Root + "/src/App.cs", Create().Resolve("src/App.cs"));

    [Fact]
    public void Resolve_BackslashesAndTrailingSlash_AreNormalized() =>
        Assert.Equal(Root + "/src", Create().Resolve("src\\"));

    [Fact]
    public void Resolve_AbsolutePathUnderRoot_IsAllowed() =>
        Assert.Equal(Root + "/a", Create().Resolve(Root + "/a"));

    [Theory]
    [InlineData("$/MG-Grp/Source/Other")]
    [InlineData("$/MG-Grp/Source/Main-Dotnet-Upgrade-Evil")]
    [InlineData("$/")]
    public void Resolve_OutsideRoot_Throws(string path) =>
        Assert.Throws<ArgumentException>(() => Create().Resolve(path));

    [Theory]
    [InlineData("../x")]
    [InlineData("a/../../b")]
    [InlineData(Root + "/../Other")]
    public void Resolve_Traversal_Throws(string path) =>
        Assert.Throws<ArgumentException>(() => Create().Resolve(path));

    [Theory]
    [InlineData("")]
    [InlineData("<set-in-appsettings.Local.json>")]
    public void Constructor_MissingPat_Throws(string pat) =>
        Assert.Throws<InvalidOperationException>(() => Create(pat));
}
