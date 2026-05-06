using SearchEngine;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
  Status = "ok"
}));

app.MapGet("/v1/info", () => Results.Ok(new
{
  Service = "TiSoft.SearchEngine.Service",
  Status = "ok",
  SearchEngineVersion = typeof(Search<int>).Assembly.GetName().Version?.ToString()
}));

app.Run();
