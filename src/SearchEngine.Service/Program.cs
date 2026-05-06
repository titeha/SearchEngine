using System.Text.Json.Serialization;

using SearchEngine;
using SearchEngine.Service;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<SearchIndexStore>();

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

app.MapGet("/v1/index", (SearchIndexStore store) => Results.Ok(store.GetStatus()));

app.MapPost("/v1/index", BuildIndexAsync);

app.MapPost("/v1/search", Search);

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

static async Task<IResult> BuildIndexAsync(IndexBuildRequest request, SearchIndexStore store)
{
  ApiError? error = await store.BuildAsync(request);

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