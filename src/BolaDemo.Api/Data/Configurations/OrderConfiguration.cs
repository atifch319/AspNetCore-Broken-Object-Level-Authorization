using BolaDemo.Api.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BolaDemo.Api.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();
        builder.Property(order => order.OrderNumber).HasMaxLength(32).IsRequired();
        builder.Property(order => order.CustomerUserId).HasMaxLength(64).IsRequired();
        builder.Property(order => order.Status).HasMaxLength(32).IsRequired();
        builder.Property(order => order.TotalAmount).HasPrecision(18, 2);
        builder.Property(order => order.InternalNotes).HasMaxLength(2000).IsRequired();

        builder.HasIndex(order => order.OrderNumber).IsUnique();
        builder.HasIndex(order => new { order.CustomerUserId, order.Id });

        builder.HasOne(order => order.Customer)
            .WithMany(user => user.Orders)
            .HasForeignKey(order => order.CustomerUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
