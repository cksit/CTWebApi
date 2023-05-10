using CTWebApi.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CTWebApi.Services
{
    public class GitHubUserService : IGitHubUserService
    {
        private readonly ILogger<GitHubUserService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubUserService(ILogger<GitHubUserService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<List<GitHubUser>> GetGitHubUsers(List<string> usernames, int page, int pageSize)
        {
            _logger.LogInformation("Start processing GetGitHubUsers");

            List<string> validUsernames = GetValidUserNames(usernames);
            _logger.LogInformation("Validated users count: " + validUsernames.Count);

            var httpClient = _httpClientFactory.CreateClient("default");
            httpClient.DefaultRequestHeaders.Add("User-Agent", "coreAPI");

            var paginatedUsernames = validUsernames.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            List<GitHubUser> users = new List<GitHubUser>();
            foreach (var username in paginatedUsernames)
            {
                try
                {
                    _logger.LogInformation("Handling request for user: " + username);
                    using var response = await httpClient.GetAsync("https://api.github.com/users/" + username);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var gitHubUser = JsonSerializer.Deserialize<GitHubUser>(apiResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (gitHubUser != null)
                    {
                        if (string.IsNullOrEmpty(gitHubUser.login))
                        {
                            _logger.LogWarning($"GitHub API response for user '{username}' did not contain valid user information");
                            gitHubUser.login = username;
                        }
                        else
                        {
                            _logger.LogInformation("Found User " + username + " from GitHub API");
                        }
                        users.Add(gitHubUser);
                    }
                    else
                    {
                        _logger.LogWarning($"Response Deserialization for user '{username}' failed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred while retrieving user information for user '{username}'" + ex.Message);
                }
            }

            return users;
        }

        public List<string> GetValidUserNames(List<string> usernames)
        {
            _logger.LogInformation("Validating input user lists");
            Regex regex = new Regex(@"^[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38}$", RegexOptions.IgnoreCase);
            List<string> finalizedUsernames = new List<string>();
            foreach (var item in usernames.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    if (regex.IsMatch(item))
                    {
                        finalizedUsernames.Add(item);
                    }
                }
            }
            finalizedUsernames.Sort();
            return finalizedUsernames;
        }
    }
}
