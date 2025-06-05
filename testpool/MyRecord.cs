using Microsoft.Identity.Client.Advanced;

namespace testpool;

public class MyRecord
{
  public int Id { get; set; }
  public string? FirstName { get; set; }
  public string? LastName { get; set; }
  public string? FullName { get; set; }
  public string? Language { get; set; }
  public string? Gender { get; set; }
  public string? Metadata { get; set; }

  public override string ToString()
  {
    return $"{Id}, {FirstName}, {LastName}, {FullName}, {Language}, {Metadata}";
  }
}