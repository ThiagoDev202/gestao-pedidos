using GestaoPedidos.Application.Errors;
using GestaoPedidos.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Polly.Registry;

namespace GestaoPedidos.Infrastructure.Resilience;

public sealed class ConsultaSql(string connectionString, ResiliencePipelineProvider<string> pipelines)
{
    public const string NomeDoPipeline = "leitura-sql";

    public async Task<T> ExecutarAsync<T>(Func<SqlConnection, CancellationToken, Task<T>> consulta, CancellationToken cancellationToken)
    {
        var pipeline = pipelines.GetPipeline(NomeDoPipeline);
        try
        {
            return await pipeline.ExecuteAsync(
                async token =>
                {
                    await using var conexao = new SqlConnection(connectionString);
                    await conexao.OpenAsync(token);
                    return await consulta(conexao, token);
                },
                cancellationToken);
        }
        catch (Exception exception) when (ErrosDeBanco.EhIndisponibilidade(exception))
        {
            throw new BancoIndisponivelException(exception);
        }
    }
}
