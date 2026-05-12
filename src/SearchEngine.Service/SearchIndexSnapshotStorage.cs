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
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Задача сохранения snapshot-файла.</returns>
  public async Task SaveAsync(
      SearchIndexSnapshotFile snapshot,
      CancellationToken cancellationToken = default)
  {
    if (!_options.Snapshot.IsEnabled)
      return;

    string filePath = _options.Snapshot.FilePath;

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
  /// <param name="cancellationToken">Токен отмены операции.</param>
  /// <returns>Снимок данных поискового индекса или <see langword="null"/>, если файл отсутствует или snapshot отключён.</returns>
  public async Task<SearchIndexSnapshotFile?> LoadAsync(
      CancellationToken cancellationToken = default)
  {
    if (!_options.Snapshot.IsEnabled)
      return null;

    string filePath = _options.Snapshot.FilePath;

    if (!File.Exists(filePath))
      return null;

    await using FileStream stream = File.OpenRead(filePath);

    return await JsonSerializer
        .DeserializeAsync<SearchIndexSnapshotFile>(stream, _jsonOptions, cancellationToken)
        .ConfigureAwait(false);
  }
}