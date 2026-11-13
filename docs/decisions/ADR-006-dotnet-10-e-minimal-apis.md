# ADR-006 — .NET 10 LTS e Minimal APIs

**Status:** Aceito · **Data:** 2026-11-13

## Contexto
O enunciado pede ".NET 8 ou superior". O suporte do .NET 8 termina em novembro de 2026.

## Decisão
.NET 10 (LTS) com **Minimal APIs**: um `MapGroup` por recurso em `Api/Endpoints/<Recurso>Endpoints.cs`, `TypedResults`, OpenAPI nativo e Scalar. Endpoints finos que delegam a handlers.

## Alternativas
- .NET 8: suporte acabando.
- Controllers MVC: mais cerimônia para endpoints de um caso de uso cada.

## Consequências
Exige SDK 10 local e imagens `mcr.microsoft.com/dotnet/*:10.0`.
