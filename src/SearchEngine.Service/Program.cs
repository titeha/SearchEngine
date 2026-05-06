namespace SearchEngine.Service;

public class Program
{
  public static void Main(string[] args)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    WebApplication app = builder.Build();

    app.MapGet("/health", () => Results.Ok(new
    {
      Status = "ok"
    }));

    app.Run();
  }
}
