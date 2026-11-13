using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace GestaoPedidos.Infrastructure.Persistence;

internal static class ErrosDeBanco
{
    private static readonly int[] NumerosDeIndisponibilidade = [-2, -1, 0, 2, 53, 64, 233, 10053, 10054, 10060, 10061];

    public static bool EhIndisponibilidade(Exception exception)
    {
        for (var atual = exception; atual is not null; atual = atual.InnerException!)
        {
            if (atual is RetryLimitExceededException)
            {
                return true;
            }

            if (atual is SqlException sql && (sql.IsTransient || sql.Errors.Cast<SqlError>().Any(e => NumerosDeIndisponibilidade.Contains(e.Number) || e.Class >= 20)))
            {
                return true;
            }

            if (atual.InnerException is null)
            {
                break;
            }
        }

        return false;
    }
}
