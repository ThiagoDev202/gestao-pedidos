using FluentValidation;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Application.Pedidos.Commands.CriarPedido;
using GestaoPedidos.Application.Pedidos.Dtos;
using GestaoPedidos.Application.Pedidos.Queries.ListarPedidos;
using GestaoPedidos.Application.Pedidos.Queries.ObterPedido;
using GestaoPedidos.Application.Relatorios.Queries.TopClientes;
using Microsoft.Extensions.DependencyInjection;

namespace GestaoPedidos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, ServiceLifetime.Singleton);

        services.AddScoped<ICommandHandler<CriarPedidoCommand, CriarPedidoResult>, CriarPedidoHandler>();
        services.AddScoped<IQueryHandler<ListarPedidosQuery, PedidosPaginadosDto>, ListarPedidosHandler>();
        services.AddScoped<IQueryHandler<ObterPedidoQuery, PedidoDetalheDto>, ObterPedidoHandler>();
        services.AddScoped<IQueryHandler<TopClientesQuery, IReadOnlyList<TopClienteDto>>, TopClientesHandler>();

        services.Decorate(typeof(ICommandHandler<,>), typeof(ValidationCommandDecorator<,>));
        services.Decorate(typeof(IQueryHandler<,>), typeof(ValidationQueryDecorator<,>));
        return services;
    }
}
