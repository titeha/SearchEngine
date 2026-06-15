using System.Data.Common;

using Npgsql;

namespace SearchEngine.Service;

/// <summary>
/// Читает документы из PostgreSQL-источника данных.
/// </summary>
/// <remarks>
/// Создаёт reader PostgreSQL-источника данных.
/// </remarks>
/// <param name="configuration">Конфигурация сервиса.</param>
public sealed class PostgresSearchDataSourceReader(IConfiguration configuration)
    : SqlQuerySearchDataSourceReader(configuration, "PostgreSQL")
{
  /// <summary>
  /// Имя provider-а PostgreSQL.
  /// </summary>
  public const string ProviderName = "postgres";

  /// <inheritdoc />
  public override string Provider => ProviderName;

  /// <inheritdoc />
  protected override DbConnection CreateConnection(string connectionString)
  {
    return new NpgsqlConnection(connectionString);
  }
}
