using System.Data.Common;

using Microsoft.Data.SqlClient;

namespace SearchEngine.Service;

/// <summary>
/// Читает документы из Microsoft SQL Server-источника данных.
/// </summary>
/// <remarks>
/// Создаёт reader SQL Server-источника данных.
/// </remarks>
/// <param name="configuration">Конфигурация сервиса.</param>
public sealed class SqlServerSearchDataSourceReader(IConfiguration configuration)
    : SqlQuerySearchDataSourceReader(configuration, "SQL Server")
{
  /// <summary>
  /// Имя provider-а SQL Server.
  /// </summary>
  public const string ProviderName = "sqlserver";

  /// <inheritdoc />
  public override string Provider => ProviderName;

  /// <inheritdoc />
  protected override DbConnection CreateConnection(string connectionString)
  {
    return new SqlConnection(connectionString);
  }
}
