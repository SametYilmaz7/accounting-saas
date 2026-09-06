using SaaSPlatform.Domain.Common;

namespace SaaSPlatform.Domain.Features.Tenants;

/// <summary>
/// An organization or workspace using the SaaS platform.
/// </summary>
public sealed class Tenant : AuditableEntity
{
    public const int MaxNameLength = 200;
    public const int MaxSlugLength = 100;

    private Tenant(Guid id, string name, string slug)
    {
        Id = id;
        Name = name;
        Slug = slug;
        IsActive = true;
    }

    public string Name { get; }

    public string Slug { get; }

    public bool IsActive { get; }

    public static Tenant Create(Guid id, string name, string slug)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A tenant identifier must not be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);

        var trimmedName = name.Trim();
        if (trimmedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Tenant name must not exceed {MaxNameLength} characters.", nameof(name));
        }

        var normalizedSlug = SlugNormalizer.Normalize(slug);
        if (normalizedSlug.Length == 0)
        {
            throw new ArgumentException("Tenant slug must contain ASCII letters or digits.", nameof(slug));
        }

        if (normalizedSlug.Length > MaxSlugLength)
        {
            throw new ArgumentException($"Tenant slug must not exceed {MaxSlugLength} characters.", nameof(slug));
        }

        return new Tenant(id, trimmedName, normalizedSlug);
    }
}
