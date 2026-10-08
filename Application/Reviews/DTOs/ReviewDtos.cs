using System.ComponentModel;
using ScentMatch.Application.Common.Models;

namespace ScentMatch.Application.Reviews.DTOs;

public class ReviewUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class ReviewDto
{
    public int Id { get; set; }
    public ReviewUserDto User { get; set; } = null!;
    public int Rating { get; set; }
    public string? Text { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class OwnReviewDto
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public ReviewUserDto User { get; set; } = null!;
    public int Rating { get; set; }
    public string? Text { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class AdminReviewUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class AdminReviewPerfumeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AdminReviewDto
{
    public int Id { get; set; }
    public AdminReviewUserDto User { get; set; } = null!;
    public AdminReviewPerfumeDto Perfume { get; set; } = null!;
    public int Rating { get; set; }
    public string? Text { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateReviewRequest
{
    public int Rating { get; set; }
    public string? Text { get; set; }
}

public class UpdateReviewRequest
{
    public int Rating { get; set; }
    public string? Text { get; set; }
}

public class AdminReviewQueryParameters : PaginationQuery
{
    [Description("Filter by status: Pending | Approved | Rejected. Defaults to Pending when omitted.")]
    public string? Status { get; set; }
}
