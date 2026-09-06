using System.Globalization;
using SaaSPlatform.Domain.Features.Tenants;

namespace SaaSPlatform.UnitTests.Features.Tenants;

public sealed class SlugNormalizerTests
{
    [Theory]
    [InlineData("Acme Workspace", "acme-workspace")]
    [InlineData("  My   Project  ", "my-project")]
    [InlineData("Test---Workspace", "test-workspace")]
    [InlineData("--Hello\t\r\nWorld--", "hello-world")]
    [InlineData("A!@_B / C", "ab-c")]
    [InlineData("  Project 123  ", "project-123")]
    [InlineData("a-!-b", "a-b")]
    [InlineData("", "")]
    [InlineData("---中文!", "")]
    public void Normalize_WhenInputContainsFormatting_ShouldReturnCanonicalSlug(string input, string expected)
    {
        var normalized = SlugNormalizer.Normalize(input);

        Assert.Equal(expected, normalized);
        Assert.Equal(normalized, SlugNormalizer.Normalize(normalized));
    }

    [Fact]
    public void Normalize_WhenInputIsNull_ShouldRejectInput()
    {
        Assert.Throws<ArgumentNullException>("slug", () => SlugNormalizer.Normalize(null!));
    }

    [Fact]
    public void Normalize_WhenCultureChanges_ShouldReturnSameAsciiSlug()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var expected = SlugNormalizer.Normalize("İstanbul I Workspace");

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            var actual = SlugNormalizer.Normalize("İstanbul I Workspace");

            Assert.Equal(expected, actual);
            Assert.Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$", actual);
            Assert.EndsWith("-i-workspace", actual);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
