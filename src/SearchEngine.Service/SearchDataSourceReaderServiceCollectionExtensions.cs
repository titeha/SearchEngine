using System.Data.Common;

namespace SearchEngine.Service;

/// <summary>
/// Методы регистрации reader-ов источников данных в контейнере зависимостей.
/// </summary>
/// <remarks>
/// Эти методы — публичная точка расширения сервиса. Они позволяют подключить
/// собственный источник данных (другую СУБД, файл, внешний API), не изменяя код сервиса.
/// </remarks>
public static class SearchDataSourceReaderServiceCollectionExtensions
{
  /// <summary>
  /// Регистрирует пользовательский reader источника данных.
  /// </summary>
  /// <typeparam name="TReader">Тип reader-а источника данных.</typeparam>
  /// <param name="services">Коллекция сервисов.</param>
  /// <returns>Та же коллекция сервисов для построения цепочки вызовов.</returns>
  public static IServiceCollection AddSearchDataSourceReader<TReader>(this IServiceCollection services)
      where TReader : class, ISearchDataSourceReader
  {
    ArgumentNullException.ThrowIfNull(services);

    services.AddSingleton<ISearchDataSourceReader, TReader>();

    return services;
  }

  /// <summary>
  /// Регистрирует SQL-источник данных по имени provider-а и фабрике подключения.
  /// </summary>
  /// <remarks>
  /// Подходит для СУБД без встроенного reader-а (например IBM DB2): не требуется
  /// создавать класс-наследник, достаточно передать фабрику ADO.NET-подключения.
  /// </remarks>
  /// <param name="services">Коллекция сервисов.</param>
  /// <param name="provider">Имя provider-а, по которому источник выбирается в registry.</param>
  /// <param name="providerDisplayName">Отображаемое имя provider-а для сообщений об ошибках.</param>
  /// <param name="connectionFactory">Фабрика, создающая ADO.NET-подключение по строке подключения.</param>
  /// <returns>Та же коллекция сервисов для построения цепочки вызовов.</returns>
  public static IServiceCollection AddSqlSearchDataSource(
      this IServiceCollection services,
      string provider,
      string providerDisplayName,
      Func<string, DbConnection> connectionFactory)
  {
    ArgumentNullException.ThrowIfNull(services);

    if (string.IsNullOrWhiteSpace(provider))
      throw new ArgumentException("Имя provider-а не должно быть пустым.", nameof(provider));

    if (string.IsNullOrWhiteSpace(providerDisplayName))
      throw new ArgumentException("Отображаемое имя provider-а не должно быть пустым.", nameof(providerDisplayName));

    ArgumentNullException.ThrowIfNull(connectionFactory);

    services.AddSingleton<ISearchDataSourceReader>(serviceProvider =>
        new DelegateSqlSearchDataSourceReader(
            serviceProvider.GetRequiredService<IConfiguration>(),
            provider,
            providerDisplayName,
            connectionFactory));

    return services;
  }
}
