using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed class IpRemotoFixoStartupFilter(IPAddress ip) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, proximo) =>
            {
                context.Connection.RemoteIpAddress = ip;
                await proximo(context);
            });
            next(app);
        };
    }
}
