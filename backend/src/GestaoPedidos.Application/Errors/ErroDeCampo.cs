namespace GestaoPedidos.Application.Errors;

public sealed record ErroDeCampo(string Campo, string Code, string Mensagem);
