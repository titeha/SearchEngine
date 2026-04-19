using BenchmarkDotNet.Running;

namespace SearchEngine.Benchmarks;

internal static class Program
{
  private const string RuntimeEnvironmentVariable = "SEARCHENGINE_BENCHMARK_RUNTIMES";
  private const string JobEnvironmentVariable = "SEARCHENGINE_BENCHMARK_JOB";

  private static void Main(string[] args)
  {
    if (args.Length == 0)
      ConfigureInteractiveRun();

    BenchmarkSwitcher
      .FromAssembly(typeof(Program).Assembly)
      .Run(args);
  }

  private static void ConfigureInteractiveRun()
  {
    Console.WriteLine("Профиль запуска бенчмарков:");
    Console.WriteLine("1 - Быстрый: net10 + ShortRun");
    Console.WriteLine("2 - Обычный: net6, net8, net10");
    Console.WriteLine("3 - Полный: net6, net7, net8, net9, net10");
    Console.WriteLine("4 - Свой профиль");
    Console.WriteLine();
    Console.Write("Выбор [1]: ");

    string? choice = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(choice))
      choice = "1";

    switch (choice.Trim())
    {
      case "1":
        SetProfile("net10", "short");
        break;

      case "2":
        SetProfile("net6,net8,net10", "default");
        break;

      case "3":
        SetProfile("all", "default");
        break;

      case "4":
        ConfigureCustomProfile();
        break;

      default:
        Console.WriteLine("Неизвестный выбор. Используется быстрый профиль.");
        SetProfile("net10", "short");
        break;
    }

    Console.WriteLine();
    Console.WriteLine($"Runtime'ы: {Environment.GetEnvironmentVariable(RuntimeEnvironmentVariable)}");
    Console.WriteLine($"Режим job: {Environment.GetEnvironmentVariable(JobEnvironmentVariable)}");
    Console.WriteLine();
    Console.WriteLine("Дальше BenchmarkDotNet предложит выбрать benchmark-класс.");
    Console.WriteLine();
  }

  private static void ConfigureCustomProfile()
  {
    Console.WriteLine();
    Console.WriteLine("Введите runtime'ы через запятую.");
    Console.WriteLine("Допустимые значения: net6, net7, net8, net9, net10, all");
    Console.Write("Runtime'ы [net10]: ");

    string? runtimes = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(runtimes))
      runtimes = "net10";

    Console.WriteLine();
    Console.WriteLine("Введите режим job.");
    Console.WriteLine("Допустимые значения: default, short");
    Console.Write("Job [short]: ");

    string? job = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(job))
      job = "short";

    SetProfile(runtimes, job);
  }

  private static void SetProfile(string runtimes, string job)
  {
    Environment.SetEnvironmentVariable(RuntimeEnvironmentVariable, runtimes);
    Environment.SetEnvironmentVariable(JobEnvironmentVariable, job);
  }
}