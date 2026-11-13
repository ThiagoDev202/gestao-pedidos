using System.Data.Common;
using System.Reflection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GestaoPedidos.IntegrationTests.Support;

public sealed class FalhaTransitoriaInterceptor(int numeroDoErro) : DbCommandInterceptor
{
    private int _falhas;

    public int Falhas => _falhas;

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        FalharAntesSeForBaixaDeEstoque(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        FalharAntesSeForBaixaDeEstoque(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override ValueTask<int> NonQueryExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (EhBaixaDeEstoque(command))
        {
            FalharUmaVez();
        }

        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void FalharAntesSeForBaixaDeEstoque(DbCommand command)
    {
        if (EhBaixaDeEstoque(command))
        {
            FalharUmaVez();
        }
    }

    private void FalharUmaVez()
    {
        if (Interlocked.CompareExchange(ref _falhas, 1, 0) == 0)
        {
            throw SqlExceptionFactory.Criar(numeroDoErro);
        }
    }

    private static bool EhBaixaDeEstoque(DbCommand command)
    {
        var texto = command.CommandText;
        return texto.Contains("UPDATE", StringComparison.OrdinalIgnoreCase)
            && texto.Contains("Produtos", StringComparison.OrdinalIgnoreCase)
            && texto.Contains("Estoque", StringComparison.OrdinalIgnoreCase);
    }
}

public static class SqlExceptionFactory
{
    public static SqlException Criar(int numero)
    {
        var construtorDoErro = typeof(SqlError)
            .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length)
            .First();
        var argumentos = construtorDoErro.GetParameters().Select(p => Argumento(p, numero)).ToArray();
        var erro = (SqlError)construtorDoErro.Invoke(argumentos);

        var colecao = (SqlErrorCollection)Activator.CreateInstance(typeof(SqlErrorCollection), nonPublic: true)!;
        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(colecao, [erro]);

        var criar = typeof(SqlException)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == "CreateException"
                && m.GetParameters().Select(p => p.ParameterType).SequenceEqual([typeof(SqlErrorCollection), typeof(string)]));
        return (SqlException)criar.Invoke(null, [colecao, "16.0.0"])!;
    }

    private static object? Argumento(ParameterInfo parametro, int numero)
    {
        if (parametro.ParameterType == typeof(int))
        {
            return parametro.Position == 0 ? numero : 0;
        }

        if (parametro.ParameterType == typeof(byte))
        {
            return parametro.Name == "errorClass" ? (byte)13 : (byte)0;
        }

        if (parametro.ParameterType == typeof(uint))
        {
            return 0u;
        }

        if (parametro.ParameterType == typeof(string))
        {
            return parametro.Name == "errorMessage" ? "Falha transitória simulada." : string.Empty;
        }

        return null;
    }
}
