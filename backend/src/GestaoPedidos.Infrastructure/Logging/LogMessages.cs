using Microsoft.Extensions.Logging;

namespace GestaoPedidos.Infrastructure.Logging;

internal static partial class LogMessages
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{Tipo} {Nome} concluído em {Milissegundos} ms")]
    public static partial void Concluido(ILogger logger, string tipo, string nome, double milissegundos);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Tipo} {Nome} recusado: {Motivo}")]
    public static partial void Recusado(ILogger logger, string tipo, string nome, string motivo);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Tipo} {Nome} falhou")]
    public static partial void Falhou(ILogger logger, Exception exception, string tipo, string nome);
}
