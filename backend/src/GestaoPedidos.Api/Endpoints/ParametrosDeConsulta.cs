using System.Globalization;
using GestaoPedidos.Application.Errors;

namespace GestaoPedidos.Api.Endpoints;

internal sealed class ParametrosDeConsulta
{
    public List<ErroDeCampo> Erros { get; } = [];

    public HashSet<string> CamposComErroDeFormato { get; } = [];

    public int Inteiro(string? texto, int padrao, string campo, string code, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return padrao;
        }

        if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor))
        {
            return valor;
        }

        Registrar(campo, code, mensagem);
        return padrao;
    }

    public int? InteiroOpcional(string? texto, string campo, string code, string mensagem)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (int.TryParse(texto, NumberStyles.Integer, CultureInfo.InvariantCulture, out var valor))
        {
            return valor;
        }

        Registrar(campo, code, mensagem);
        return null;
    }

    public DateOnly? Data(string? texto, string campo)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return null;
        }

        if (DateOnly.TryParseExact(texto, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var valor))
        {
            return valor;
        }

        Registrar(campo, "data_invalida", $"A data '{campo}' deve estar no formato yyyy-MM-dd.");
        return null;
    }

    public IReadOnlyList<ErroDeCampo> Combinar(IReadOnlyList<ErroDeCampo> errosDoValidador) =>
        [.. Erros, .. errosDoValidador.Where(e => !CamposComErroDeFormato.Contains(e.Campo))];

    private void Registrar(string campo, string code, string mensagem)
    {
        Erros.Add(new ErroDeCampo(campo, code, mensagem));
        CamposComErroDeFormato.Add(campo);
    }
}
