using System.Net;
using System.Text;
using GestaoPedidos.IntegrationTests.Support;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Errors;

public sealed class CorpoInvalidoTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    [Fact]
    public async Task Rejects_null_item_with_400_and_invalid_body_code()
    {
        var conteudo = new StringContent("""{"clienteId":1,"itens":[null]}""", Encoding.UTF8, "application/json");

        var resposta = await CriarCliente().PostAsync("/pedidos", conteudo);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        var erros = problema.ErrosDeCampo();
        erros.Count.ShouldBe(1);
        erros[0].Code.ShouldBe("corpo_invalido");
        erros[0].Campo.ShouldBe("$");
        erros[0].Mensagem.ShouldNotBeNullOrWhiteSpace();
        (await Banco.ContarPedidosAsync()).ShouldBe(0);
    }
}
