namespace SearchEngine;

public interface ISourceData<T> where T : struct
{
  public T Id { get; }

  public string Text { get; }
}