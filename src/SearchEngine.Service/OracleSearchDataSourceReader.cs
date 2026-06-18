using System.Data.Common;

using Oracle.ManagedDataAccess.Client;

namespace SearchEngine.Service;

/// <summary>
/// Читает документы из Oracle-источника данных.
/// </summary>
/// <remarks>
/// Создаёт reader Oracle-источника данных.
/// </remarks>
/// <param name="configuration">Конфигурация сервиса.</param>
public sealed class OracleSearchDataSourceReader(IConfiguration configuration)
    : SqlQuerySearchDataSourceReader(configuration, "Oracle")
{
  /// <summary>
  /// Имя provider-а Oracle.
  /// </summary>
  public const string ProviderName = "oracle";

  /// <inheritdoc />
  public override string Provider => ProviderName;

  /// <inheritdoc />
  protected override DbConnection CreateConnection(string connectionString)
  {
    return new OracleConnection(connectionString);
  }
}
