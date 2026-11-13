using System.Net;
using System.Text;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shouldly;

namespace GestaoPedidos.IntegrationTests.Errors;

public sealed class ErroPadraoTests(SqlServerFixture sql) : TesteDeIntegracao(sql)
{
    private const string MensagemInterna = "Conexao 10.9.8.7 falhou ao gravar em dbo.Pedidos";

    [Fact]
    public async Task Rejects_malformed_json_with_400_and_invalid_body_code()
    {
        var conteudo = new StringContent("""{"clienteId": 1, "itens": [""", Encoding.UTF8, "application/json");

        var resposta = await CriarCliente().PostAsync("/pedidos", conteudo);

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.BadRequest, "validacao_falhou");
        var erros = problema.ErrosDeCampo();
        erros.Count.ShouldBe(1);
        erros[0].Code.ShouldBe("corpo_invalido");
        erros[0].Campo.ShouldBe("$");
        erros[0].Mensagem.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Rejects_non_json_content_type_with_415()
    {
        var conteudo = new StringContent("clienteId=1", Encoding.UTF8, "text/plain");

        var resposta = await CriarCliente().PostAsync("/pedidos", conteudo);

        await resposta.DeveSerProblemaAsync(HttpStatusCode.UnsupportedMediaType, "formato_nao_suportado");
    }

    [Fact]
    public async Task Returns_404_problem_for_an_unknown_route()
    {
        var resposta = await CriarCliente().GetAsync("/rota-que-nao-existe");

        await resposta.DeveSerProblemaAsync(HttpStatusCode.NotFound, "recurso_nao_encontrado");
    }

    [Fact]
    public async Task Returns_500_without_leaking_the_internal_message()
    {
        var cliente = CriarCliente(servicos: s =>
        {
            s.RemoveAll<IUnitOfWork>();
            s.AddScoped<IUnitOfWork, UnitOfWorkQueFalha>();
        });

        var resposta = await cliente.CriarPedidoAsync(Respostas.Pedido(1, (1, 1)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.InternalServerError, "erro_interno");
        var corpo = problema.GetRawText();
        corpo.ShouldNotContain(MensagemInterna);
        corpo.ShouldNotContain("dbo.Pedidos");
        corpo.ShouldNotContain(nameof(InvalidOperationException));
        corpo.ShouldNotContain(nameof(UnitOfWorkQueFalha));
    }

    [Fact]
    public async Task Returns_503_when_the_database_is_unavailable()
    {
        var api = CriarApi(connectionString: Sql.ConnectionStringInacessivel());
        var cliente = api.CreateClient();
        cliente.Timeout = TimeSpan.FromMinutes(3);

        var resposta = await cliente.CriarPedidoAsync(Respostas.Pedido(1, (1, 1)));

        var problema = await resposta.DeveSerProblemaAsync(HttpStatusCode.ServiceUnavailable, "banco_indisponivel");
        problema.GetRawText().ShouldNotContain("127.0.0.1");
    }

    private sealed class UnitOfWorkQueFalha : IUnitOfWork
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> work, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException(MensagemInterna);
        }
    }
}
