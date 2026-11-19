using BolaDemo.Api.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolaDemo.Api.Data.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasMaxLength(64).ValueGeneratedNever();
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.Property(user => user.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(user => user.Role).HasMaxLength(32).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
    }
}
