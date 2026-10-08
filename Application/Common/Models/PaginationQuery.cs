using System.ComponentModel;

namespace ScentMatch.Application.Common.Models;

public class PaginationQuery
{
    [Description("Page number (1-based)")]
    public int Page { get; set; } = 1;

    [Description("Page size (1..100)")]
    public int PageSize { get; set; } = 20;
}
