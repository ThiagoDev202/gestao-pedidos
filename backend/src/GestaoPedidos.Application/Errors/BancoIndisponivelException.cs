namespace GestaoPedidos.Application.Errors;

public sealed class BancoIndisponivelException(Exception causa)
    : Exception("O banco de dados está indisponível.", causa);
