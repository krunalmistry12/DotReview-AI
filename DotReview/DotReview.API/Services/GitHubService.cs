using System.Net.Http.Headers;

namespace DotReview.API.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetPullRequestDiffAsync(
        string owner,
        string repo,
        int pullRequestNumber)
    {
        var url =
            $"https://api.github.com/repos/{owner}/{repo}/pulls/{pullRequestNumber}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        request.Headers.UserAgent.Add(
            new ProductInfoHeaderValue("DotReview-AI", "1.0"));

        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github.diff"));

        var response = await _httpClient.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync();
    }
}