using CanvasFlow.Api.Services.CmsApi.Models;
using System.Net.Http.Json;

namespace CanvasFlow.Api.Services.CmsApi;

/// <summary>
/// C# client for the Content CMS API (http://192.168.88.68:8085/swagger/v1/swagger.json)
/// Covers all content-related endpoints across the CMS API with Bearer token authorization.
/// </summary>
public class CmsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private string? _token;

    public CmsApiClient(HttpClient httpClient, string baseUrl)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    #region Authorization

    /// <summary>
    /// Authenticate with the CMS API and store the Bearer token.
    /// POST /api/Auth/login (body: LoginRequestDto)
    /// </summary>
    public async Task<CmsLoginResponseDto> LoginAsync(string username, string password)
    {
        var loginRequest = new LoginRequestDto { Username = username, Password = password };
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/Auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CmsLoginResponseDto>() 
            ?? throw new InvalidOperationException("Failed to deserialize login response.");
        
        // Store the token for subsequent requests
        _token = result.Token;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(_token);
        
        return result;
    }

    /// <summary>
    /// Logout from the CMS API and clear the stored token.
    /// POST /api/Auth/logout
    /// </summary>
    public async Task LogoutAsync()
    {
        if (_httpClient.DefaultRequestHeaders.Contains("Authorization"))
        {
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }
        _token = null;
        
        await _httpClient.PostAsync($"{_baseUrl}/api/Auth/logout", null);
    }

    /// <summary>
    /// Set the Bearer token manually (useful if token is obtained externally).
    /// </summary>
    public void SetToken(string token)
    {
        _token = token;
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    #endregion

    #region Content Endpoints

    /// <summary>
    /// Get paginated list of all content objects.
    /// GET /api/Content?page=1&pageSize=20
    /// Requires admin role.
    /// </summary>
    public async Task<ContentObjectDtoPagedResult> GetContentsAsync(int page = 1, int pageSize = 20)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/Content?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContentObjectDtoPagedResult>() 
            ?? throw new InvalidOperationException("Failed to deserialize content list.");
    }

    /// <summary>
    /// Get a single content object by ID.
    /// GET /api/Content/{id}
    /// </summary>
    public async Task<ContentModel> GetContentByContentIdAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/Content/{id}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContentModel>() 
            ?? throw new InvalidOperationException("Failed to deserialize content.");
    }

    /// <summary>
    /// Create a new content object with file upload.
    /// POST /api/Content (multipart/form-data)
    /// </summary>
    public async Task<ContentModel> CreateContentAsync(
     StreamContent fileContent, // Змінено назву для точності
     string fileName,
     int ownerId,
     bool enabled = true,
     string? description = null,
     bool isPublic = false,
     bool isDeleted = false)
    {
        using var content = new MultipartFormDataContent();

        // Встановлюємо ContentType, якщо він ще не був заданий при створенні fileContent
        if (fileContent.Headers.ContentType == null)
        {
            fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/octet-stream");
        }

        // Додаємо готовий StreamContent до форми
        content.Add(fileContent, "File", fileName);

        void AddFormField(string name, string value)
        {
            var stringContent = new StringContent(value);
            stringContent.Headers.ContentType = null;
            content.Add(stringContent, name);
        }

        AddFormField("OwnerId", ownerId.ToString());
        AddFormField("Enabled", enabled.ToString().ToLowerInvariant());
        AddFormField("IsPublic", isPublic.ToString().ToLowerInvariant());
        AddFormField("IsDeleted", isDeleted.ToString().ToLowerInvariant());

        if (description != null)
        {
            AddFormField("Description", description);
        }

        var response = await _httpClient.PostAsync($"{_baseUrl}/api/Content", content);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<ContentModel>()
            ?? throw new InvalidOperationException("Failed to deserialize created content.");
    }

    /// <summary>
    /// Update an existing content object with file upload.
    /// PUT /api/Content/{id}?contentId={id} (multipart/form-data)
    /// </summary>
    public async Task UpdateContentAsync(
        int id,
        Stream fileStream,
        string fileName,
        int ownerId,
        bool enabled = true,
        string? description = null,
        bool isPublic = false,
        bool isDeleted = false)
    {
        var content = new MultipartFormDataContent();

        // Add file
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "File", fileName);

        // Add other fields
        content.Add(new StringContent(ownerId.ToString()), "OwnerId");
        content.Add(new StringContent(enabled.ToString().ToLowerInvariant()), "Enabled");
        content.Add(new StringContent(description ?? string.Empty), "Description");
        content.Add(new StringContent(isPublic.ToString().ToLowerInvariant()), "IsPublic");
        content.Add(new StringContent(isDeleted.ToString().ToLowerInvariant()), "IsDeleted");

        var response = await _httpClient.PutAsync($"{_baseUrl}/api/Content/{id}?contentId={id}", content);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Delete a content object by ID.
    /// DELETE /api/Content/{id}
    /// </summary>
    public async Task DeleteContentAsync(int id)
    {
        var response = await _httpClient.DeleteAsync($"{_baseUrl}/api/Content/{id}");
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Get content objects by user ID (string).
    /// GET /api/Content/user/{userId}?page=1&pageSize=10
    /// </summary>
    public async Task<ContentObjectDtoPagedResult> GetContentsByUserIdAsync(int userId, int page = 1, int pageSize = 10)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/Content/user/{userId}?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContentObjectDtoPagedResult>() 
            ?? new ContentObjectDtoPagedResult();
    }

    /// <summary>
    /// Get user content by user ID (int) with pagination.
    /// GET /api/Content/userContent/{userId}?page=1&pageSize=10
    /// </summary>
    public async Task<ContentObjectDtoPagedResult> GetUserContentAsync(int userId, int page = 1, int pageSize = 10)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/Content/userContent/{userId}?page={page}&pageSize={pageSize}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ContentObjectDtoPagedResult>() 
            ?? new ContentObjectDtoPagedResult();
    }

    /// <summary>
    /// Assign content to a user.
    /// PUT /api/Content/{id}/assign (body: userId as int)
    /// </summary>
    public async Task AssignContentAsync(int id, int userId)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/Content/{id}/assign", userId);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Update content status (enabled/disabled).
    /// PUT /api/Content/{id}/status (body: enabled as bool)
    /// </summary>
    public async Task UpdateContentStatusAsync(int id, bool enabled)
    {
        var response = await _httpClient.PutAsJsonAsync($"{_baseUrl}/api/Content/{id}/status", enabled);
        response.EnsureSuccessStatusCode();
    }

    #endregion
}
