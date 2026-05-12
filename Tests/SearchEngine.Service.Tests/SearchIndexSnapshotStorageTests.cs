using Microsoft.Extensions.Options;

using SearchEngine.Service;

namespace SearchEngineService.Tests;

/// <summary>
/// Тесты файлового хранилища snapshot поискового индекса.
/// </summary>
public sealed class SearchIndexSnapshotStorageTests
{
  /// <summary>
  /// Проверяет, что при выключенном snapshot файл не создаётся.
  /// </summary>
  [Fact]
  public async Task SaveAsync_ПриВыключенномSnapshot_НеСоздаетФайл()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      SearchIndexSnapshotStorage sut = CreateStorage(
          isEnabled: false,
          filePath: filePath);

      SearchIndexSnapshotFile snapshot = CreateSnapshot();

      // Act
      await sut.SaveAsync(snapshot);

      // Assert
      Assert.False(File.Exists(filePath));
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Проверяет, что при включённом snapshot файл создаётся.
  /// </summary>
  [Fact]
  public async Task SaveAsync_ПриВключенномSnapshot_СоздаетФайл()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      SearchIndexSnapshotStorage sut = CreateStorage(
          isEnabled: true,
          filePath: filePath);

      SearchIndexSnapshotFile snapshot = CreateSnapshot();

      // Act
      await sut.SaveAsync(snapshot);

      // Assert
      Assert.True(File.Exists(filePath));
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Проверяет, что сохранённый snapshot можно загрузить обратно.
  /// </summary>
  [Fact]
  public async Task LoadAsync_ПриСуществующемSnapshot_ЗагружаетФайл()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      SearchIndexSnapshotStorage sut = CreateStorage(
          isEnabled: true,
          filePath: filePath);

      SearchIndexSnapshotFile snapshot = CreateSnapshot();

      await sut.SaveAsync(snapshot);

      // Act
      SearchIndexSnapshotFile? result = await sut.LoadAsync();

      // Assert
      Assert.NotNull(result);
      Assert.Equal(1, result.Version);
      Assert.True(result.IsPhoneticSearch);
      Assert.Equal(snapshot.CreatedAtUtc, result.CreatedAtUtc);
      Assert.Equal(2, result.Documents.Count);

      Assert.Equal(1, result.Documents[0].Id);
      Assert.Equal("Иванов Сергей Петрович", result.Documents[0].Text);

      Assert.Equal(2, result.Documents[1].Id);
      Assert.Equal("Папандопуло Александр", result.Documents[1].Text);
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Проверяет, что при отсутствии файла загрузка возвращает пустой результат.
  /// </summary>
  [Fact]
  public async Task LoadAsync_ПриОтсутствующемSnapshot_ВозвращаетNull()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      SearchIndexSnapshotStorage sut = CreateStorage(
          isEnabled: true,
          filePath: filePath);

      // Act
      SearchIndexSnapshotFile? result = await sut.LoadAsync();

      // Assert
      Assert.Null(result);
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Проверяет, что snapshot-файл сохраняет кириллицу в читаемом виде.
  /// </summary>
  [Fact]
  public async Task SaveAsync_ПриВключенномSnapshot_СохраняетКириллицуБезUnicodeEscaping()
  {
    // Arrange
    string filePath = CreateTempSnapshotPath();

    try
    {
      SearchIndexSnapshotStorage sut = CreateStorage(
          isEnabled: true,
          filePath: filePath);

      SearchIndexSnapshotFile snapshot = CreateSnapshot();

      // Act
      await sut.SaveAsync(snapshot);

      string json = await File.ReadAllTextAsync(filePath);

      // Assert
      Assert.Contains("Иванов Сергей Петрович", json);
      Assert.Contains("Папандопуло Александр", json);
      Assert.DoesNotContain("\\u0418", json);
    }
    finally
    {
      DeleteTempSnapshotDirectory(filePath);
    }
  }

  /// <summary>
  /// Создаёт тестовое хранилище snapshot.
  /// </summary>
  /// <param name="isEnabled">Признак включения snapshot.</param>
  /// <param name="filePath">Путь к snapshot-файлу.</param>
  /// <returns>Хранилище snapshot.</returns>
  private static SearchIndexSnapshotStorage CreateStorage(
      bool isEnabled,
      string filePath)
  {
    return new SearchIndexSnapshotStorage(
        Options.Create(
            new SearchEngineServiceOptions
            {
              Snapshot = new SearchIndexSnapshotOptions
              {
                IsEnabled = isEnabled,
                FilePath = filePath
              }
            }));
  }

  /// <summary>
  /// Создаёт тестовый snapshot.
  /// </summary>
  /// <returns>Тестовый snapshot.</returns>
  private static SearchIndexSnapshotFile CreateSnapshot()
  {
    return new SearchIndexSnapshotFile
    {
      Version = 1,
      IsPhoneticSearch = true,
      CreatedAtUtc = new DateTimeOffset(2026, 5, 12, 10, 0, 0, TimeSpan.Zero),
      Documents =
        [
            new()
                {
                    Id = 1,
                    Text = "Иванов Сергей Петрович"
                },
                new()
                {
                    Id = 2,
                    Text = "Папандопуло Александр"
                }
        ]
    };
  }

  /// <summary>
  /// Создаёт временный путь к snapshot-файлу.
  /// </summary>
  /// <returns>Временный путь к snapshot-файлу.</returns>
  private static string CreateTempSnapshotPath()
  {
    return Path.Combine(
        Path.GetTempPath(),
        "SearchEngine.Service.Tests",
        Guid.NewGuid().ToString("N"),
        "search-index-snapshot.json");
  }

  /// <summary>
  /// Удаляет временную папку snapshot-файла.
  /// </summary>
  /// <param name="filePath">Путь к snapshot-файлу.</param>
  private static void DeleteTempSnapshotDirectory(string filePath)
  {
    string? directoryPath = Path.GetDirectoryName(filePath);

    if (string.IsNullOrWhiteSpace(directoryPath))
      return;

    if (Directory.Exists(directoryPath))
      Directory.Delete(directoryPath, recursive: true);
  }
}