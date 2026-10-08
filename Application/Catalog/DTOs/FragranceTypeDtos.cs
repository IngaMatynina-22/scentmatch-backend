namespace ScentMatch.Application.Catalog.DTOs;

public class FragranceTypeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CreateFragranceTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateFragranceTypeRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
