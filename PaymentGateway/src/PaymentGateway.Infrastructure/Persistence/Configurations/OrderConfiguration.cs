using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentGateway.Domain.Orders;

namespace PaymentGateway.Infrastructure.Persistence.Configurations;

/// <summary>
/// Aqui a gente ensina o EF Core a mapear o agregado Order pra tabela SQL, sem precisar
/// mudar NADA na classe Order do Domain (ela continua 100% "pura", sem atributos de
/// EF Core espalhados nela). Essa separação é um dos maiores ganhos do DDD.
///
/// Alguns pontos que merecem atenção aqui porque o Order foi desenhado com
/// encapsulamento forte (list privada, construtor privado):
/// </summary>
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerEmail)
            .IsRequired()
            .HasMaxLength(256);

        // O Status é um enum no C#, mas salvamos como texto no banco (ex: "Paid" em vez
        // de "2"). Fica muito mais fácil de ler direto no banco quando for debugar.
        builder.Property(o => o.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.ProcessedAt);
        builder.Property(o => o.DeclinedReason).HasMaxLength(500);

        // TotalAmount e DomainEvents são propriedades CALCULADAS (não existe coluna
        // pra elas no banco), então dizemos explicitamente ao EF pra ignorá-las.
        builder.Ignore(o => o.TotalAmount);
        builder.Ignore(o => o.DomainEvents);

        // O campo _items é privado (list interna), e a propriedade pública Items só
        // devolve uma cópia somente-leitura dela. Por isso configuramos o relacionamento
        // apontando direto pro campo privado "_items", em vez da propriedade pública.
        builder.HasMany<OrderItem>("_items")
            .WithOne()
            .HasForeignKey("OrderId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation("_items").UsePropertyAccessMode(PropertyAccessMode.Field);

        // A propriedade pública "Items" também precisa ser ignorada, senão o EF tenta
        // mapeá-la como se fosse outro relacionamento (e dá conflito com o de cima).
        builder.Ignore(o => o.Items);
    }
}
