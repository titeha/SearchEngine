using System.Data.Common;

namespace SearchEngine.Service;

/// <summary>
/// SQL-reader источника данных, настраиваемый именем provider-а и фабрикой ADO.NET-подключения.
/// </summary>
/// <remarks>
/// Позволяет подключить СУБД, для которой нет встроенного reader-а (например IBM DB2),
/// без создания отдельного класса-наследника: достаточно передать имя provider-а
/// и фабрику подключения. Вся остальная механика SQL-чтения берётся из
/// <see cref="SqlQuerySearchDataSourceReader"/>.
/// </remarks>
public sealed class DelegateSqlSearchDataSourceReader : SqlQuerySearchDataSourceReader
{
  private readonly string _provider;
  private readonly Func<string, DbConnection> _connectionFactory;

  /// <summary>
  /// Создаёт SQL-reader источника данных на основе фабрики подключения.
  /// </summary>
  /// <param name="configuration">Конфигурация сервиса.</param>
  /// <param name="provider">Имя provider-а, по которому источник выбирается в registry.</param>
  /// <param name="providerDisplayName">Отображаемое имя provider-а для сообщений об ошибках.</param>
  /// <param name="connectionFactory">Фабрика, создающая ADO.NET-подключение по строке подключения.</param>
  public DelegateSqlSearchDataSourceReader(
      IConfiguration configuration,
      string provider,
      string providerDisplayName,
      Func<string, DbConnection> connectionFactory)
      : base(configuration, providerDisplayName)
  {
    if (string.IsNullOrWhiteSpace(provider))
      throw new ArgumentException("Имя provider-а не должно быть пустым.", nameof(provider));

    ArgumentNullException.ThrowIfNull(connectionFactory);

    _provider = provider.Trim();
    _connectionFactory = connectionFactory;
  }

  /// <inheritdoc />
  public override string Provider => _provider;

  /// <inheritdoc />
  protected override DbConnection CreateConnection(string connectionString) => _connectionFactory(connectionString);
}
