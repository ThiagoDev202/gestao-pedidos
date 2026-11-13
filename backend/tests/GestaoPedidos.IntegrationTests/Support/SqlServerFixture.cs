using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Respawn;
using Testcontainers.MsSql;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed partial class SqlServerFixture : IAsyncLifetime
{
    private const string Imagem = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string NomeDoBanco = "GestaoPedidos";

    private readonly MsSqlContainer _container = new MsSqlBuilder(Imagem).Build();
    private Respawner? _respawner;

    public string ConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var master = _container.GetConnectionString();
        await AplicarScriptAsync(master, "01-create-database.sql");
        await AplicarScriptAsync(master, "02-schema.sql");

        ConnectionString = new SqlConnectionStringBuilder(master)
        {
            InitialCatalog = NomeDoBanco,
            TrustServerCertificate = true,
        }.ConnectionString;

        await using var conexao = new SqlConnection(ConnectionString);
        await conexao.OpenAsync();
        _respawner = await Respawner.CreateAsync(conexao, new RespawnerOptions
        {
            DbAdapter = DbAdapter.SqlServer,
            WithReseed = true,
        });
    }

    public async Task LimparAsync()
    {
        await using var conexao = new SqlConnection(ConnectionString);
        await conexao.OpenAsync();
        await _respawner!.ResetAsync(conexao);
    }

    public string ConnectionStringInacessivel()
    {
        return new SqlConnectionStringBuilder(ConnectionString)
        {
            DataSource = "127.0.0.1,1",
            ConnectTimeout = 1,
        }.ConnectionString;
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    private static async Task AplicarScriptAsync(string connectionString, string arquivo)
    {
        var conteudo = await File.ReadAllTextAsync(Path.Combine(PastaDoBanco(), arquivo));
        await using var conexao = new SqlConnection(connectionString);
        await conexao.OpenAsync();
        foreach (var lote in SeparadorDeLotes().Split(conteudo).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            await using var comando = new SqlCommand(lote, conexao);
            await comando.ExecuteNonQueryAsync();
        }
    }

    private static string PastaDoBanco()
    {
        var diretorio = new DirectoryInfo(AppContext.BaseDirectory);
        while (diretorio is not null)
        {
            var candidato = Path.Combine(diretorio.FullName, "database");
            if (File.Exists(Path.Combine(candidato, "02-schema.sql")))
            {
                return candidato;
            }

            diretorio = diretorio.Parent;
        }

        throw new DirectoryNotFoundException("Pasta database/ não encontrada acima de " + AppContext.BaseDirectory);
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex SeparadorDeLotes();
}

[CollectionDefinition(Nome)]
public sealed class ColecaoApi : ICollectionFixture<SqlServerFixture>
{
    public const string Nome = "api";
}
