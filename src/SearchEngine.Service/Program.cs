using System.Text.Json.Serialization;

using Microsoft.Extensions.Options;

using SearchEngine;
using SearchEngine.Service;

if (HealthCheckCommand.IsRequested(args))
{
  Environment.ExitCode = await HealthCheckCommand.RunAsync().ConfigureAwait(false);
  return;
}

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.Configure<SearchEngineServiceOptions>(builder.Configuration.GetSection("SearchEngineService"));

builder.Services.AddSingleton<SearchIndexSnapshotStorage>();
builder.Services.AddSingleton<SearchIndexStore>();

WebApplication app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
  Status = "ok"
}));

app.MapGet("/ready", GetReadiness);

app.MapGet("/v1/info", () => Results.Ok(new
{
  Service = "TiSoft.SearchEngine.Service",
  ServiceVersion = typeof(SearchIndexStore).Assembly.GetName().Version?.ToString(),
  Status = "ok",
  SearchEngineVersion = typeof(Search<int>).Assembly.GetName().Version?.ToString()
}));

app.MapGet("/v1/config", GetConfig);

app.MapGet("/v1/index", (SearchIndexStore store) => Results.Ok(store.GetStatus()));

app.MapPost("/v1/index", BuildIndexAsync);

app.MapPost("/v1/index/restore", RestoreIndexAsync);

app.MapPost("/v1/search", Search);

app.MapGet("/v1/search/options", GetSearchOptions);

app.MapPost("/v1/index/validate", ValidateIndexRequest);

app.Run();

static IResult ValidateIndexRequest(IndexBuildRequest request)
{
  if (request.Documents is null || request.Documents.Count == 0)
  {
    return Results.BadRequest(new ApiError
    {
      Code = "EmptyDocuments",
      Message = "Не переданы документы для индексации."
    });
  }

  int searchableDocumentCount = request.Documents.Count(
      document => !string.IsNullOrWhiteSpace(document?.Text));

  if (searchableDocumentCount == 0)
  {
    return Results.BadRequest(new ApiError
    {
      Code = "EmptySearchableDocuments",
      Message = "Документы не содержат пригодного для индексации текста."
    });
  }

  return Results.Ok(new IndexValidateResponse
  {
    DocumentCount = request.Documents.Count,
    SearchableDocumentCount = searchableDocumentCount,
    IsPhoneticSearch = request.IsPhoneticSearch
  });
}

static IResult GetReadiness(SearchIndexStore store)
{
  IndexStatusResponse status = store.GetStatus();

  ReadinessResponse response = new()
  {
    Status = status.IsReady ? "ready" : "not_ready",
    IsReady = status.IsReady,
    DocumentCount = status.DocumentCount,
    SearchableDocumentCount = status.SearchableDocumentCount,
    IsPhoneticSearch = status.IsPhoneticSearch,
    CreatedAtUtc = status.CreatedAtUtc
  };

  return status.IsReady
      ? Results.Ok(response)
      : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
}

static IResult GetConfig(IOptions<SearchEngineServiceOptions> options)
{
  SearchEngineServiceOptions value = options.Value;

  return Results.Ok(new SearchEngineServiceConfigResponse
  {
    MaxDocumentCount = value.MaxDocumentCount,
    MaxDocumentTextLength = value.MaxDocumentTextLength,
    Snapshot = new SearchIndexSnapshotConfigResponse
    {
      IsEnabled = value.Snapshot.IsEnabled,
      FilePath = value.Snapshot.FilePath
    }
  });
}

static IResult GetSearchOptions()
{
  return Results.Ok(new SearchOptionsResponse
  {
    MatchModes = Enum.GetNames<QueryMatchMode>(),
    SearchTypes = Enum.GetNames<SearchType>(),
    SearchLocations = Enum.GetNames<SearchLocation>(),
    DefaultMatchMode = nameof(QueryMatchMode.AllTerms),
    DefaultSearchType = nameof(SearchType.ExactSearch),
    DefaultSearchLocation = nameof(SearchLocation.BeginWord)
  });
}

static async Task<IResult> BuildIndexAsync(IndexBuildRequest request, SearchIndexStore store)
{
  ApiError? error = await store.BuildAsync(request);

  if (error is not null)
    return Results.BadRequest(error);

  return Results.Ok(store.GetStatus());
}

static async Task<IResult> RestoreIndexAsync(
    SearchIndexStore store,
    CancellationToken cancellationToken)
{
  ApiError? error = await store.RestoreAsync(cancellationToken);

  if (error is not null)
    return Results.BadRequest(error);

  return Results.Ok(store.GetStatus());
}

static IResult Search(SearchQueryRequest request, SearchIndexStore store)
{
  SearchQueryResponse? response = store.Search(request, out ApiError? error);

  if (error is not null)
    return Results.BadRequest(error);

  return Results.Ok(response);
}

/// <summary>
/// Точка входа сервиса, открытая для интеграционных тестов.
/// </summary>
public partial class Program
{
}