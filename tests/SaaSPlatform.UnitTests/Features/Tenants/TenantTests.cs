using SaaSPlatform.Domain.Common;
using SaaSPlatform.Domain.Features.Tenants;

namespace SaaSPlatform.UnitTests.Features.Tenants;

public sealed class TenantTests
{
    [Fact]
    public void Create_WhenValuesAreValid_ShouldCreateTenant()
    {
        var id = Guid.NewGuid();

        var tenant = Tenant.Create(id, "Acme Workspace", "Acme Workspace");

        Assert.Equal(id, tenant.Id);
        Assert.Equal("Acme Workspace", tenant.Name);
        Assert.Equal("acme-workspace", tenant.Slug);
        Assert.IsAssignableFrom<AuditableEntity>(tenant);
    }

    [Fact]
    public void Create_WhenNameHasSurroundingWhitespace_ShouldTrimName()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), " \tAcme Workspace\r\n", "acme");

        Assert.Equal("Acme Workspace", tenant.Name);
    }

    [Fact]
    public void Create_WhenIdIsEmpty_ShouldRejectId()
    {
        Assert.Throws<ArgumentException>("id", () => Tenant.Create(Guid.Empty, "Acme", "acme"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    public void Create_WhenNameIsBlank_ShouldRejectName(string name)
    {
        Assert.Throws<ArgumentException>("name", () => Tenant.Create(Guid.NewGuid(), name, "acme"));
    }

    [Fact]
    public void Create_WhenNameIsNull_ShouldRejectName()
    {
        Assert.Throws<ArgumentNullException>("name", () => Tenant.Create(Guid.NewGuid(), null!, "acme"));
    }

    [Fact]
    public void Create_WhenNameExceedsMaximumLength_ShouldRejectName()
    {
        Assert.Throws<ArgumentException>("name", () => Tenant.Create(Guid.NewGuid(), new string('a', 201), "acme"));
    }

    [Fact]
    public void Create_WhenNormalizedValuesAreAtMaximumLengths_ShouldAcceptValues()
    {
        var name = new string('a', 200);
        var slug = new string('b', 100);

        var tenant = Tenant.Create(Guid.NewGuid(), $" {name} ", $"-- {slug} --");

        Assert.Equal(name, tenant.Name);
        Assert.Equal(slug, tenant.Slug);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" \t\r\n")]
    [InlineData("---")]
    [InlineData("!@#$")]
    [InlineData("中文")]
    public void Create_WhenNormalizedSlugIsEmpty_ShouldRejectSlug(string slug)
    {
        Assert.Throws<ArgumentException>("slug", () => Tenant.Create(Guid.NewGuid(), "Acme", slug));
    }

    [Fact]
    public void Create_WhenSlugIsNull_ShouldRejectSlug()
    {
        Assert.Throws<ArgumentNullException>("slug", () => Tenant.Create(Guid.NewGuid(), "Acme", null!));
    }

    [Fact]
    public void Create_WhenNormalizedSlugExceedsMaximumLength_ShouldRejectSlug()
    {
        Assert.Throws<ArgumentException>("slug", () => Tenant.Create(Guid.NewGuid(), "Acme", new string('a', 101)));
    }

    [Fact]
    public void Create_WhenTenantIsNew_ShouldBeActive()
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Acme", "acme");

        Assert.True(tenant.IsActive);
    }

    [Theory]
    [InlineData("İstanbul Workspace")]
    [InlineData("Çığ Öşü Workspace 中文 🚀")]
    public void Create_WhenSlugContainsNonAsciiCharacters_ShouldProduceAsciiSlug(string slug)
    {
        var tenant = Tenant.Create(Guid.NewGuid(), "Workspace", slug);

        Assert.Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$", tenant.Slug);
    }
}
