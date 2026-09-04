using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DotReview.API.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;
    private readonly string _githubToken;

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;

        _githubToken =
            Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? throw new InvalidOperationException(
                "GITHUB_TOKEN is not configured.");
    }

    public async Task<string> GetPullRequestDiffAsync(
        string owner,
        string repo,
        int pullRequestNumber)
    {
        var url =
            $"https://api.github.com/repos/{owner}/{repo}/pulls/{pullRequestNumber}";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Get,
                url);

        request.Headers.UserAgent.Add(
            new ProductInfoHeaderValue(
                "DotReview-AI",
                "1.0"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _githubToken);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github.diff"));

        var response =
            await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }

    public async Task CreatePullRequestCommentAsync(
        string owner,
        string repo,
        int pullRequestNumber,
        string comment)
    {
        var url =
            $"https://api.github.com/repos/{owner}/{repo}/issues/{pullRequestNumber}/comments";

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                url);

        request.Headers.UserAgent.Add(
            new ProductInfoHeaderValue(
                "DotReview-AI",
                "1.0"));

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _githubToken);

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue(
                "application/vnd.github+json"));

        var payload = new
        {
            body = comment
        };

        var json =
            JsonSerializer.Serialize(payload);

        request.Content =
            new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

        var response =
            await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();
    }
}