using System.Data.Common;

using Microsoft.Data.Sqlite;

namespace SearchEngine.Service;

/// <summary>
/// Читает документы из SQLite-источника данных.
/// </summary>
/// <remarks>
/// Создаёт reader SQLite-источника данных.
/// </remarks>
/// <param name="configuration">Конфигурация сервиса.</param>
public sealed class SqliteSearchDataSourceReader(IConfiguration configuration)
    : SqlQuerySearchDataSourceReader(configuration, "SQLite")
{
  /// <summary>
  /// Имя provider-а SQLite.
  /// </summary>
  public const string ProviderName = "sqlite";

  /// <inheritdoc />
  public override string Provider => ProviderName;

  /// <inheritdoc />
  protected override DbConnection CreateConnection(string connectionString)
  {
    return new SqliteConnection(connectionString);
  }
}
