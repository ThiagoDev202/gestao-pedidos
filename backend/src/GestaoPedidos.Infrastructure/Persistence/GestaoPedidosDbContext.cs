using GestaoPedidos.Domain.Clientes;
using GestaoPedidos.Domain.Pedidos;
using GestaoPedidos.Domain.Produtos;
using Microsoft.EntityFrameworkCore;

namespace GestaoPedidos.Infrastructure.Persistence;

public sealed class GestaoPedidosDbContext(DbContextOptions<GestaoPedidosDbContext> options) : DbContext(options)
{
    public DbSet<Cliente> Clientes => Set<Cliente>();

    public DbSet<Produto> Produtos => Set<Produto>();

    public DbSet<Pedido> Pedidos => Set<Pedido>();

    public DbSet<RegistroIdempotenciaEntity> Idempotencias => Set<RegistroIdempotenciaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cliente>(builder =>
        {
            builder.ToTable("Clientes");
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Nome).HasMaxLength(200);
            builder.Property(c => c.Email).HasMaxLength(254);
        });

        modelBuilder.Entity<Produto>(builder =>
        {
            builder.ToTable("Produtos");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Nome).HasMaxLength(200);
            builder.Property(p => p.Sku).HasMaxLength(50);
            builder.Property(p => p.Preco).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Pedido>(builder =>
        {
            builder.ToTable("Pedidos");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.DataPedido).HasColumnType("datetime2(3)");
            builder.Property(p => p.ValorTotal).HasPrecision(18, 2);
            builder.HasMany(p => p.Itens).WithOne().HasForeignKey(i => i.PedidoId).OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(p => p.Itens).HasField("_itens").UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ItemPedido>(builder =>
        {
            builder.ToTable("ItensPedido");
            builder.HasKey(i => i.Id);
            builder.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
            builder.Ignore(i => i.Subtotal);
        });

        modelBuilder.Entity<RegistroIdempotenciaEntity>(builder =>
        {
            builder.ToTable("IdempotenciaPedidos");
            builder.HasKey(r => r.Chave);
            builder.Property(r => r.HashCorpo).HasMaxLength(64).IsFixedLength();
            builder.Property(r => r.CriadoEm).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()").ValueGeneratedOnAdd();
        });
    }
}
