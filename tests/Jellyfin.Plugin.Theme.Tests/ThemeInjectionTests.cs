using Jellyfin.Plugin.Theme.Services;
using Xunit;

namespace Jellyfin.Plugin.Theme.Tests;

public class ThemeInjectionTests
{
    [Theory]
    [InlineData("/web")]
    [InlineData("/web/")]
    [InlineData("/web/index.html")]
    [InlineData("/jellyfin/web/")]
    [InlineData("/jellyfin/web/index.html")]
    [InlineData("/WEB/INDEX.HTML")]
    public void IsIndexRequest_MatchesTheAppShell(string path)
    {
        Assert.True(ThemeInjection.IsIndexRequest(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("/web/main.js")]
    [InlineData("/web/index.html.map")]
    [InlineData("/Items/abc")]
    [InlineData("/webhook")]
    public void IsIndexRequest_IgnoresEverythingElse(string? path)
    {
        Assert.False(ThemeInjection.IsIndexRequest(path));
    }

    [Fact]
    public void TryInject_InsertsBeforeTheClosingBodyTag()
    {
        const string Html = "<html><body><div id=\"app\"></div></body></html>";

        Assert.True(ThemeInjection.TryInject(Html, "BLOCK", out var result));
        Assert.Equal("<html><body><div id=\"app\"></div>BLOCK\n</body></html>", result);
    }

    [Fact]
    public void TryInject_IsIdempotent()
    {
        var block = ThemeInjection.BuildBlock(string.Empty, "1.2.3.4");
        const string Html = "<html><body></body></html>";

        Assert.True(ThemeInjection.TryInject(Html, block, out var once));
        Assert.False(ThemeInjection.TryInject(once, block, out var twice));
        Assert.Equal(once, twice);
    }

    [Fact]
    public void TryInject_LeavesDocumentsWithoutABodyTagAlone()
    {
        const string Html = "<html><p>no body close</p></html>";

        Assert.False(ThemeInjection.TryInject(Html, "BLOCK", out var result));
        Assert.Equal(Html, result);
    }

    [Fact]
    public void TryInject_UsesTheLastClosingBodyTag()
    {
        const string Html = "<html><body><pre>&lt;/body&gt;</pre></body></html>";

        Assert.True(ThemeInjection.TryInject(Html, "BLOCK", out var result));
        Assert.EndsWith("</pre>BLOCK\n</body></html>", result, System.StringComparison.Ordinal);
    }

    [Fact]
    public void BuildBlock_PointsAtTheServerRootWhenThereIsNoBaseUrl()
    {
        var block = ThemeInjection.BuildBlock(string.Empty, "1.2.3.4");

        Assert.Contains("href=\"/Theme/theme.css?v=1.2.3.4\"", block, System.StringComparison.Ordinal);
        Assert.Contains("src=\"/Theme/theme.js?v=1.2.3.4\"", block, System.StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("/jellyfin")]
    [InlineData("/jellyfin/")]
    public void BuildBlock_KeepsTheBaseUrlPrefix(string pathBase)
    {
        var block = ThemeInjection.BuildBlock(pathBase, "1.2.3.4");

        Assert.Contains("href=\"/jellyfin/Theme/theme.css?v=1.2.3.4\"", block, System.StringComparison.Ordinal);
    }
}
