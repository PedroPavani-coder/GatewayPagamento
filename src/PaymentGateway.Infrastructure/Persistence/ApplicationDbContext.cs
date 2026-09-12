using Microsoft.EntityFrameworkCore;
using PaymentGateway.Domain.Orders;

namespace PaymentGateway.Infrastructure.Persistence;

/// <summary>
/// O DbContext é a "porta de entrada" do Entity Framework Core pro banco de dados.
/// Cada DbSet&lt;T&gt; representa uma tabela.
///
/// Repare que essa classe só conhece tipos do Domain (Order) — ela não devolve DTOs
/// nem nada da Application. Isso é proposital: a Infrastructure serve ao Domain/Application,
/// nunca o contrário.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public DbSet<Order> Orders => Set<Order>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Isso faz o EF Core procurar automaticamente todas as classes que implementam
        // IEntityTypeConfiguration<> nesse assembly (OrderConfiguration, OrderItemConfiguration)
        // e aplicar cada uma, em vez de eu ter que registrar uma por uma manualmente aqui.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
