using System.Diagnostics;
using System.Net.Http.Json;

string baseUrl = GetArgument(args, "--url", "http://localhost:5037");
int documentCount = GetIntArgument(args, "--documents", 10_000);
int parallelism = GetIntArgument(args, "--parallel", 16);
int durationSeconds = GetIntArgument(args, "--seconds", 15);

Console.WriteLine("SearchEngine.Service load smoke");
Console.WriteLine($"URL:        {baseUrl}");
Console.WriteLine($"Documents:  {documentCount}");
Console.WriteLine($"Parallel:   {parallelism}");
Console.WriteLine($"Duration:   {durationSeconds}s");
Console.WriteLine();

using HttpClient client = new()
{
  BaseAddress = new Uri(baseUrl),
  Timeout = TimeSpan.FromSeconds(10)
};

await EnsureServiceIsAvailableAsync(client);
await BuildIndexAsync(client, documentCount);
await RunLoadAsync(client, parallelism, durationSeconds);

static async Task EnsureServiceIsAvailableAsync(HttpClient client)
{
  using HttpResponseMessage response = await client.GetAsync("/health");

  response.EnsureSuccessStatusCode();
}

static async Task BuildIndexAsync(HttpClient client, int documentCount)
{
  Console.WriteLine("Building test index...");

  IndexDocumentRequest[] documents = [.. Enumerable
      .Range(1, documentCount)
      .Select(id => new IndexDocumentRequest(
          id,
          $"Иванов Сергей {id} товар велосипед красный артикул ART-{id:D6}"))];

  IndexBuildRequest request = new(
      IsPhoneticSearch: true,
      Documents: documents);

  Stopwatch stopwatch = Stopwatch.StartNew();

  using HttpResponseMessage response = await client.PostAsJsonAsync("/v1/index", request);

  stopwatch.Stop();

  if (!response.IsSuccessStatusCode)
  {
    string body = await response.Content.ReadAsStringAsync();

    Console.WriteLine(body);

    response.EnsureSuccessStatusCode();
  }

  Console.WriteLine($"Index built in {stopwatch.ElapsedMilliseconds} ms");
  Console.WriteLine();
}

static async Task RunLoadAsync(
    HttpClient client,
    int parallelism,
    int durationSeconds)
{
  Console.WriteLine("Running search load...");

  using CancellationTokenSource cts = new(TimeSpan.FromSeconds(durationSeconds));

  List<long> latencies = [];
  int successCount = 0;
  int errorCount = 0;

  object sync = new();

  Task[] workers = [.. Enumerable
      .Range(0, parallelism)
      .Select(workerId => Task.Run(async () =>
      {
        for (int requestNumber = 0; !cts.IsCancellationRequested; requestNumber++)
        {
          SearchRequest request = CreateSearchRequest(workerId, requestNumber);

          Stopwatch stopwatch = Stopwatch.StartNew();

          try
          {
            using HttpResponseMessage response = await client.PostAsJsonAsync(
                  "/v1/search",
                  request,
                  cts.Token);

            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
              lock (sync)
              {
                successCount++;
                latencies.Add(stopwatch.ElapsedMilliseconds);
              }
            else
              lock (sync)
              {
                errorCount++;
              }
          }
          catch when (cts.IsCancellationRequested)
          {
            return;
          }
          catch
          {
            stopwatch.Stop();

            lock (sync)
            {
              errorCount++;
            }
          }
        }
      }))];

  await Task.WhenAll(workers);

  long[] sortedLatencies;

  lock (sync)
  {
    sortedLatencies = [.. latencies.Order()];
  }

  double totalSeconds = durationSeconds;
  double rps = successCount / totalSeconds;

  Console.WriteLine();
  Console.WriteLine("Result:");
  Console.WriteLine($"Success: {successCount}");
  Console.WriteLine($"Errors:  {errorCount}");
  Console.WriteLine($"RPS:     {rps:F2}");

  if (sortedLatencies.Length == 0)
    return;

  Console.WriteLine($"p50:     {Percentile(sortedLatencies, 50)} ms");
  Console.WriteLine($"p95:     {Percentile(sortedLatencies, 95)} ms");
  Console.WriteLine($"p99:     {Percentile(sortedLatencies, 99)} ms");
}

static SearchRequest CreateSearchRequest(int workerId, int requestNumber)
{
  int variant = (workerId + requestNumber) % 4;

  return variant switch
  {
    0 => new SearchRequest(
        Query: "Иванов",
        MatchMode: "AllTerms",
        SearchType: "ExactSearch",
        SearchLocation: "BeginWord"),

    1 => new SearchRequest(
        Query: "велосипед",
        MatchMode: "AllTerms",
        SearchType: "ExactSearch",
        SearchLocation: "BeginWord"),

    2 => new SearchRequest(
        Query: "Ivanov",
        MatchMode: "AllTerms",
        SearchType: "ExactSearch",
        SearchLocation: "BeginWord"),

    _ => new SearchRequest(
        Query: "веласипед",
        MatchMode: "AllTerms",
        SearchType: "NearSearch",
        SearchLocation: "BeginWord",
        PrecisionSearch: 70)
  };
}

static long Percentile(long[] sortedValues, int percentile)
{
  if (sortedValues.Length == 0)
    return 0;

  int index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Length) - 1;

  index = Math.Clamp(index, 0, sortedValues.Length - 1);

  return sortedValues[index];
}

static string GetArgument(string[] args, string name, string defaultValue)
{
  for (int i = 0; i < args.Length - 1; i++)
    if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
      return args[i + 1];

  return defaultValue;
}

static int GetIntArgument(string[] args, string name, int defaultValue)
{
  string value = GetArgument(args, name, defaultValue.ToString());

  return int.TryParse(value, out int result)
      ? result
      : defaultValue;
}

internal sealed record IndexBuildRequest(
    bool IsPhoneticSearch,
    IReadOnlyList<IndexDocumentRequest> Documents);

internal sealed record IndexDocumentRequest(
    int Id,
    string Text);

internal sealed record SearchRequest(
    string Query,
    string MatchMode,
    string SearchType,
    string SearchLocation,
    int? PrecisionSearch = null,
    int? AcceptableCountMisprint = null);