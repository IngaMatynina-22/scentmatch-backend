namespace ScentMatch.Application.Catalog.DTOs;

public class SeasonDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateSeasonRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateSeasonRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
