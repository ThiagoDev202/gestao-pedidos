FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props GestaoPedidos.slnx ./
COPY src/GestaoPedidos.Domain/*.csproj src/GestaoPedidos.Domain/
COPY src/GestaoPedidos.Application/*.csproj src/GestaoPedidos.Application/
COPY src/GestaoPedidos.Infrastructure/*.csproj src/GestaoPedidos.Infrastructure/
COPY src/GestaoPedidos.Api/*.csproj src/GestaoPedidos.Api/
RUN dotnet restore src/GestaoPedidos.Api/GestaoPedidos.Api.csproj
COPY src/ src/
RUN dotnet publish src/GestaoPedidos.Api/GestaoPedidos.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "GestaoPedidos.Api.dll"]
