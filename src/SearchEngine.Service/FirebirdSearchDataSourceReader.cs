using System.Data.Common;

using FirebirdSql.Data.FirebirdClient;

namespace SearchEngine.Service;

/// <summary>
/// Читает документы из Firebird-источника данных.
/// </summary>
/// <remarks>
/// Создаёт reader Firebird-источника данных.
/// </remarks>
/// <param name="configuration">Конфигурация сервиса.</param>
public sealed class FirebirdSearchDataSourceReader(IConfiguration configuration)
    : SqlQuerySearchDataSourceReader(configuration, "Firebird")
{
  /// <summary>
  /// Имя provider-а Firebird.
  /// </summary>
  public const string ProviderName = "firebird";

  /// <inheritdoc />
  public override string Provider => ProviderName;

  /// <inheritdoc />
  protected override DbConnection CreateConnection(string connectionString)
  {
    return new FbConnection(connectionString);
  }
}
