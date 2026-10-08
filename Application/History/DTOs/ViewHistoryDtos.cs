using ScentMatch.Application.Catalog.DTOs;

namespace ScentMatch.Application.History.DTOs;

public class ViewHistoryDto
{
    public int Id { get; set; }
    public PerfumeListItemDto Perfume { get; set; } = null!;
    public DateTime? ViewedAt { get; set; }
}
