namespace testpool;
public record MyRecord
{
  public required int Id { get; set; } 
  public required string FirstName { get; set; }
  public required string LastName { get; set; }  
  public required string FullName { get; set; }  
  public required string Language { get; set; }
  public required string Gender { get; set; }
}