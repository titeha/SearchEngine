using System.Text;

using Microsoft.Data.Sqlite;

namespace SearchEngine.Service.SqliteDemoSeed;

/// <summary>
/// Создаёт локальную SQLite-БД для demo-сценария SearchEngine.Service.
/// </summary>
internal static class Program
{
  private const string _defaultDatabaseRelativePath =
      "src/SearchEngine.Service/data/sqlite-demo/search-demo.db";

  /// <summary>
  /// Точка входа seed-инструмента.
  /// </summary>
  /// <param name="args">Аргументы командной строки.</param>
  /// <returns>Код выхода процесса.</returns>
  public static async Task<int> Main(string[] args)
  {
    Console.OutputEncoding = Encoding.UTF8;

    string databasePath = GetArgument(
        args,
        "--database",
        Path.Combine(Environment.CurrentDirectory, _defaultDatabaseRelativePath));

    databasePath = Path.GetFullPath(databasePath);

    await SeedAsync(databasePath);

    Console.WriteLine("SQLite demo database created.");
    Console.WriteLine($"Database: {databasePath}");

    return 0;
  }

  /// <summary>
  /// Создаёт SQLite-БД с demo-документами.
  /// </summary>
  /// <param name="databasePath">Путь к SQLite-файлу.</param>
  private static async Task SeedAsync(string databasePath)
  {
    string? directoryPath = Path.GetDirectoryName(databasePath);

    if (!string.IsNullOrWhiteSpace(directoryPath))
      Directory.CreateDirectory(directoryPath);

    DeleteExistingDatabaseFiles(databasePath);

    await using SqliteConnection connection = new(CreateConnectionString(databasePath));

    await connection.OpenAsync();

    await using SqliteCommand createCommand = connection.CreateCommand();

    createCommand.CommandText = """
            create table search_documents
            (
                id integer not null primary key,
                text text not null
            );
            """;

    await createCommand.ExecuteNonQueryAsync();

    await using SqliteCommand insertCommand = connection.CreateCommand();

    insertCommand.CommandText = """
            insert into search_documents (id, text)
            values
                (1, 'Иванов Сергей Петрович'),
                (2, 'Папандопуло Александр'),
                (3, 'Красный велосипед');
            """;

    await insertCommand.ExecuteNonQueryAsync();
  }

  /// <summary>
  /// Создаёт строку подключения к SQLite-БД.
  /// </summary>
  /// <param name="databasePath">Путь к SQLite-файлу.</param>
  /// <returns>Строка подключения.</returns>
  private static string CreateConnectionString(string databasePath)
  {
    SqliteConnectionStringBuilder builder = new()
    {
      DataSource = databasePath,
      Pooling = false
    };

    return builder.ToString();
  }

  /// <summary>
  /// Удаляет старые файлы SQLite-БД.
  /// </summary>
  /// <param name="databasePath">Путь к SQLite-файлу.</param>
  private static void DeleteExistingDatabaseFiles(string databasePath)
  {
    DeleteFileIfExists(databasePath);
    DeleteFileIfExists($"{databasePath}-wal");
    DeleteFileIfExists($"{databasePath}-shm");
  }

  /// <summary>
  /// Удаляет файл, если он существует.
  /// </summary>
  /// <param name="filePath">Путь к файлу.</param>
  private static void DeleteFileIfExists(string filePath)
  {
    if (File.Exists(filePath))
      File.Delete(filePath);
  }

  /// <summary>
  /// Возвращает значение аргумента командной строки.
  /// </summary>
  /// <param name="args">Аргументы командной строки.</param>
  /// <param name="name">Имя аргумента.</param>
  /// <param name="defaultValue">Значение по умолчанию.</param>
  /// <returns>Значение аргумента.</returns>
  private static string GetArgument(
      string[] args,
      string name,
      string defaultValue)
  {
    for (int i = 0; i < args.Length - 1; i++)
    {
      if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        return args[i + 1];
    }

    return defaultValue;
  }
}