using System.Reflection;
using GestaoPedidos.Api.Errors;
using GestaoPedidos.Application.Abstractions;
using GestaoPedidos.Domain.Pedidos;
using NetArchTest.Rules;
using Shouldly;

namespace GestaoPedidos.ArchitectureTests;

public sealed class DependencyRulesTests
{
    private const string Domain = "GestaoPedidos.Domain";
    private const string Application = "GestaoPedidos.Application";
    private const string Infrastructure = "GestaoPedidos.Infrastructure";
    private const string Api = "GestaoPedidos.Api";
    private const string Composition = "GestaoPedidos.Api.Composition";
    private const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    private const string Dapper = "Dapper";
    private const string AspNetCore = "Microsoft.AspNetCore";
    private const string SqlClient = "Microsoft.Data.SqlClient";
    private const string SqlClientLegado = "System.Data.SqlClient";

    private static readonly Assembly DomainAssembly = typeof(Pedido).Assembly;
    private static readonly Assembly ApplicationAssembly = typeof(IClock).Assembly;
    private static readonly Assembly InfrastructureAssembly = Assembly.Load(new AssemblyName(Infrastructure));
    private static readonly Assembly ApiAssembly = typeof(ErrorCatalog).Assembly;

    private static void DeveSerCumprida(TestResult resultado)
    {
        var violadores = resultado.FailingTypeNames ?? [];
        resultado.IsSuccessful.ShouldBeTrue("Tipos que violam a regra: " + string.Join(", ", violadores));
    }

    [Fact]
    public void Domain_does_not_depend_on_other_layers_or_frameworks()
    {
        var resultado = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Api, EntityFrameworkCore, Dapper, AspNetCore, SqlClient, SqlClientLegado)
            .GetResult();

        DeveSerCumprida(resultado);
    }

    [Fact]
    public void Domain_depends_only_on_the_base_library()
    {
        var resultado = Types.InAssembly(DomainAssembly)
            .Should()
            .OnlyHaveDependenciesOn(Domain, "System", "Microsoft.CodeAnalysis")
            .GetResult();

        DeveSerCumprida(resultado);
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_api_or_data_access()
    {
        var resultado = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api, EntityFrameworkCore, Dapper, SqlClient, SqlClientLegado, AspNetCore)
            .GetResult();

        DeveSerCumprida(resultado);
    }

    [Fact]
    public void Application_depends_only_on_domain_validation_and_dependency_injection_abstractions()
    {
        var resultado = Types.InAssembly(ApplicationAssembly)
            .Should()
            .OnlyHaveDependenciesOn(
                Application,
                Domain,
                "System",
                "Microsoft.CodeAnalysis",
                "FluentValidation",
                "Microsoft.Extensions.DependencyInjection")
            .GetResult();

        DeveSerCumprida(resultado);
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_api()
    {
        var resultado = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(Api)
            .GetResult();

        DeveSerCumprida(resultado);
    }

    [Fact]
    public void Api_uses_entity_framework_and_dapper_only_in_the_composition_root()
    {
        var resultado = Types.InAssembly(ApiAssembly)
            .That()
            .DoNotResideInNamespace(Composition)
            .ShouldNot()
            .HaveDependencyOnAny(EntityFrameworkCore, Dapper)
            .GetResult();

        DeveSerCumprida(resultado);
    }
}
