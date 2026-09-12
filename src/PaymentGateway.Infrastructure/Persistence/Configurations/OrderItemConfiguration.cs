using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Domain.Orders;

namespace PaymentGateway.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Quantity).IsRequired();

        // Money é um Value Object (não tem Id próprio), então usamos OwnsOne: o EF Core
        // cria as colunas UnitPrice_Amount e UnitPrice_Currency DENTRO da própria tabela
        // OrderItems, em vez de criar uma tabela separada pra Money.
        builder.OwnsOne(i => i.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("UnitPrice_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("UnitPrice_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Total também é calculado (UnitPrice * Quantity), não existe coluna pra ele.
        builder.Ignore(i => i.Total);
    }
}
