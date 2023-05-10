using CTWebApi.Models;

namespace CTWebApi.Services
{
    public interface IGitHubUserService
    {
        Task<List<GitHubUser>> GetGitHubUsers(List<string> usernames, int page, int pageSize);
    }
}
