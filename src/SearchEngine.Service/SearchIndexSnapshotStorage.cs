using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

using Microsoft.Extensions.Options;

namespace SearchEngine.Service;

/// <summary>
/// Выполняет чтение и запись snapshot-файла поискового индекса.
/// </summary>
public sealed class SearchIndexSnapshotStorage
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
  {
    WriteIndented = true,
    Encoder = JavaScriptEncoder.Create(
        [UnicodeRanges.BasicLatin,
        UnicodeRanges.Cyrillic])
  };

  private readonly SearchEngineServiceOptions _options;

  /// <summary>
  /// Создаёт хранилище snapshot-файла поискового индекса.
  /// </summary>
  /// <param name="options">Настройки поискового сервиса.</param>
  public SearchIndexSnapshotStorage(IOptions<SearchEngineServiceOptions> options) => _options = options.Value;

  /// <summary>
  /// Сохраняет snapshot-файл поискового индекса.
  /// </summary>
  /// <param name="snapshot">Снимок данных поискового индекса.</param>
  /// <param name="indexName">Имя индекса. Если не задано, используется индекс по умолчанию.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Задача сохранения snapshot-файла.</returns>
  public async Task SaveAsync(
      SearchIndexSnapshotFile snapshot,
      string indexName = SearchIndexStore.DefaultIndexName,
      CancellationToken cancellationToken = default)
  {
    if (!_options.Snapshot.IsEnabled)
      return;

    string filePath = ResolveFilePath(indexName);

    string? directoryPath = Path.GetDirectoryName(filePath);

    if (!string.IsNullOrWhiteSpace(directoryPath))
      Directory.CreateDirectory(directoryPath);

    await using FileStream stream = File.Create(filePath);

    await JsonSerializer
        .SerializeAsync(stream, snapshot, _jsonOptions, cancellationToken)
        .ConfigureAwait(false);
  }

  /// <summary>
  /// Загружает snapshot-файл поискового индекса.
  /// </summary>
  /// <param name="indexName">Имя индекса. Если не задано, используется индекс по умолчанию.</param>
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Снимок данных поискового индекса или <see langword="null"/>, если файл отсутствует или snapshot отключён.</returns>
  public async Task<SearchIndexSnapshotFile?> LoadAsync(
      string indexName = SearchIndexStore.DefaultIndexName,
      CancellationToken cancellationToken = default)
  {
    if (!_options.Snapshot.IsEnabled)
      return null;

    string filePath = ResolveFilePath(indexName);

    if (!File.Exists(filePath))
      return null;

    await using FileStream stream = File.OpenRead(filePath);

    return await JsonSerializer
        .DeserializeAsync<SearchIndexSnapshotFile>(stream, _jsonOptions, cancellationToken)
        .ConfigureAwait(false);
  }

  /// <summary>
  /// Возвращает путь к snapshot-файлу для указанного индекса.
  /// </summary>
  /// <remarks>
  /// Индекс по умолчанию хранится в базовом файле из настроек (ради совместимости со старыми
  /// развёртываниями), а именованные индексы — в файлах с именем индекса перед расширением,
  /// например <c>search-index-snapshot.products.json</c>.
  /// </remarks>
  /// <param name="indexName">Имя индекса.</param>
  /// <returns>Путь к snapshot-файлу индекса.</returns>
  private string ResolveFilePath(string indexName)
  {
    string basePath = _options.Snapshot.FilePath;

    if (string.IsNullOrWhiteSpace(indexName)
        || string.Equals(indexName, SearchIndexStore.DefaultIndexName, StringComparison.OrdinalIgnoreCase))
      return basePath;

    string? directoryPath = Path.GetDirectoryName(basePath);
    string fileName = Path.GetFileNameWithoutExtension(basePath);
    string extension = Path.GetExtension(basePath);

    string indexFileName = $"{fileName}.{indexName}{extension}";

    return string.IsNullOrEmpty(directoryPath)
        ? indexFileName
        : Path.Combine(directoryPath, indexFileName);
  }
}