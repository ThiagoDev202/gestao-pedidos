using System.Net;
using Microsoft.Extensions.DependencyInjection;

namespace GestaoPedidos.IntegrationTests.Support;

[Collection(ColecaoApi.Nome)]
public abstract class TesteDeIntegracao(SqlServerFixture sql) : IAsyncLifetime
{
    private readonly List<ApiFactory> _fabricas = [];

    protected SqlServerFixture Sql { get; } = sql;

    protected Banco Banco { get; } = new(sql.ConnectionString);

    public virtual async Task InitializeAsync()
    {
        await Sql.LimparAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var fabrica in _fabricas)
        {
            await fabrica.DisposeAsync();
        }
    }

    protected ApiFactory CriarApi(
        IReadOnlyDictionary<string, string?>? configuracoes = null,
        Action<IServiceCollection>? servicos = null,
        IPAddress? ipRemoto = null,
        string? connectionString = null)
    {
        var fabrica = new ApiFactory(connectionString ?? Sql.ConnectionString, configuracoes, servicos, ipRemoto);
        _fabricas.Add(fabrica);
        return fabrica;
    }

    protected HttpClient CriarCliente(
        IReadOnlyDictionary<string, string?>? configuracoes = null,
        Action<IServiceCollection>? servicos = null,
        IPAddress? ipRemoto = null)
    {
        return CriarApi(configuracoes, servicos, ipRemoto).CreateClient();
    }
}
