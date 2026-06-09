using Chubb.PolicyManagement.Domain.Entities;
using Chubb.PolicyManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Chubb.PolicyManagement.Infrastructure.Persistence.Configurations;

public sealed class PolicyEntityConfiguration : IEntityTypeConfiguration<Policy>
{
    // Maps AAndH <-> "A&H" so the DB value matches the seeded data
    private static readonly ValueConverter<LineOfBusiness, string> LineOfBusinessConverter = new(
        lob => lob == LineOfBusiness.AAndH ? "A&H" : lob.ToString(),
        s => s == "A&H" ? LineOfBusiness.AAndH : Enum.Parse<LineOfBusiness>(s));

    public void Configure(EntityTypeBuilder<Policy> entity)
    {
        entity.ToTable("Policies");

        entity.HasKey(p => p.Id);

        entity.Property(p => p.Id)
            .ValueGeneratedOnAdd();

        entity.Property(p => p.PolicyNumber)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(p => p.PolicyholderName)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(p => p.LineOfBusiness)
            .IsRequired()
            .HasConversion(LineOfBusinessConverter)
            .HasMaxLength(50);

        entity.Property(p => p.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        entity.Property(p => p.PremiumAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        entity.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10);

        entity.Property(p => p.EffectiveDate)
            .IsRequired();

        entity.Property(p => p.ExpiryDate)
            .IsRequired();

        entity.Property(p => p.Region)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(p => p.Underwriter)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(p => p.FlaggedForReview)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(p => p.CreatedAt)
            .IsRequired();

        entity.Property(p => p.UpdatedAt)
            .IsRequired();

        entity.HasIndex(p => p.Status);
        entity.HasIndex(p => p.LineOfBusiness);
        entity.HasIndex(p => p.Region);
        entity.HasIndex(p => p.EffectiveDate);
        entity.HasIndex(p => p.ExpiryDate);
        entity.HasIndex(p => p.FlaggedForReview);
        entity.HasIndex(p => p.PolicyNumber).IsUnique();
    }
}
