namespace ScentMatch.Application.Admin.DTOs;

public class AdminDashboardDto
{
    public int PerfumeCount { get; set; }
    public int BrandCount { get; set; }
    public int NoteCount { get; set; }
    public int SeasonCount { get; set; }
    public int FragranceTypeCount { get; set; }
    public ReviewsByStatusDto ReviewsByStatus { get; set; } = new();
    public CatalogGapsDto CatalogGaps { get; set; } = new();
    public int RecentJobErrorCount { get; set; }
}

public class ReviewsByStatusDto
{
    public int Pending { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
}

public class CatalogGapsDto
{
    public int MissingNotes { get; set; }
    public int MissingSeasons { get; set; }
    public int MissingImage { get; set; }
    public int MissingPrice { get; set; }
}
