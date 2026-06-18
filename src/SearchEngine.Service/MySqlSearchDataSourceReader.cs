using System.Data.Common;

using MySqlConnector;

namespace SearchEngine.Service;

/// <summary>
/// Читает документы из MySQL- или MariaDB-источника данных.
/// </summary>
/// <remarks>
/// Создаёт reader MySQL/MariaDB-источника данных.
/// </remarks>
/// <param name="configuration">Конфигурация сервиса.</param>
public sealed class MySqlSearchDataSourceReader(IConfiguration configuration)
    : SqlQuerySearchDataSourceReader(configuration, "MySQL/MariaDB")
{
  /// <summary>
  /// Имя provider-а MySQL/MariaDB.
  /// </summary>
  public const string ProviderName = "mysql";

  /// <inheritdoc />
  public override string Provider => ProviderName;

  /// <inheritdoc />
  protected override DbConnection CreateConnection(string connectionString)
  {
    return new MySqlConnection(connectionString);
  }
}
