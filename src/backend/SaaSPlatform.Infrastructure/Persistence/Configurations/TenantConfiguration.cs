using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaaSPlatform.Domain.Features.Tenants;

namespace SaaSPlatform.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants", "public", table =>
        {
            table.HasCheckConstraint("ck_tenants_name_not_blank", "length(btrim(name)) > 0");
            table.HasCheckConstraint("ck_tenants_slug_not_blank", "length(btrim(slug)) > 0");
            table.HasCheckConstraint("ck_tenants_slug_format", "slug ~ '^[a-z0-9]+(?:-[a-z0-9]+)*$'");
        });

        builder.HasKey(tenant => tenant.Id).HasName("pk_tenants");

        builder.Property(tenant => tenant.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(tenant => tenant.Name)
            .HasColumnName("name")
            .HasMaxLength(Tenant.MaxNameLength)
            .IsRequired();

        builder.Property(tenant => tenant.Slug)
            .HasColumnName("slug")
            .HasMaxLength(Tenant.MaxSlugLength)
            .IsRequired();

        builder.Property(tenant => tenant.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(tenant => tenant.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(tenant => tenant.UpdatedAtUtc)
            .HasColumnName("updated_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(tenant => tenant.Slug)
            .HasDatabaseName("uq_tenants_slug")
            .IsUnique();
    }
}
