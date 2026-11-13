using Microsoft.Extensions.DependencyInjection;

namespace GestaoPedidos.Application.Abstractions;

public static class ServiceCollectionDecoratorExtensions
{
    public static IServiceCollection Decorate(this IServiceCollection services, Type serviceOpenGeneric, Type decoratorOpenGeneric)
    {
        var descriptors = services
            .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == serviceOpenGeneric)
            .ToList();

        foreach (var original in descriptors)
        {
            var decoratorType = decoratorOpenGeneric.MakeGenericType(original.ServiceType.GetGenericArguments());
            var index = services.IndexOf(original);
            services[index] = ServiceDescriptor.Describe(
                original.ServiceType,
                provider =>
                {
                    var inner = CriarInterno(provider, original);
                    return ActivatorUtilities.CreateInstance(provider, decoratorType, inner);
                },
                original.Lifetime);
        }

        return services;
    }

    private static object CriarInterno(IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationFactory is not null)
        {
            return descriptor.ImplementationFactory(provider);
        }

        if (descriptor.ImplementationType is not null)
        {
            return ActivatorUtilities.CreateInstance(provider, descriptor.ImplementationType);
        }

        return descriptor.ImplementationInstance!;
    }
}
