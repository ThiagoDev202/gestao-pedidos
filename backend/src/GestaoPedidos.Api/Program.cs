using GestaoPedidos.Api.Composition;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGestaoPedidos(builder.Configuration);

var app = builder.Build();

app.UseGestaoPedidos();

app.Run();

public partial class Program;
