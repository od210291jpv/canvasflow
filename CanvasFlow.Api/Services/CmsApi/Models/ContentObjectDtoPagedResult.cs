namespace CanvasFlow.Api.Services.CmsApi.Models;

/// <summary>
/// Paged result for ContentObjectDto items.
/// </summary>
public class ContentObjectDtoPagedResult
{
    public List<ContentObjectDto>? Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
