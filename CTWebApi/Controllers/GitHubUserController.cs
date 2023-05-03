using CTWebApi.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CTWebApi.Controllers
{
    [Route("retrieveUsers")]
    [ApiController]
    public class GitHubUserController : ControllerBase
    {
        private readonly ILogger<GitHubUserController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public GitHubUserController(ILogger<GitHubUserController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        /*
        [HttpGet("Throw")]
        public IActionResult Throw() =>
            throw new Exception("Sample exception.");
        */
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] List<string> username,
                                                        [FromQuery] int page = 1,
                                                        [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Start processing PostAsync action");
            List<GitHubUser> users = new();

            var list = GetValidUserNames(username);
            /*
            if (list.Count == 0)
            {
                return BadRequest("Invalid username format");
            }
            */
            _logger.LogInformation("Validated users count: " + list.Count);

            //Note that we no longer need to dispose the HttpClient instance
            //because the HttpClientFactory takes care of managing the lifetime of the HttpClient instances
            var httpClient = _httpClientFactory.CreateClient("default"); 
            httpClient.DefaultRequestHeaders.Add("User-Agent", "coreAPI");

            var paginatedUsers = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            _logger.LogInformation("paginatedUsers: " + paginatedUsers.Count);

            foreach (var user in paginatedUsers)
            {
                try
                {
                    _logger.LogInformation("Handling request for user: " + user);
                    using var response = await httpClient.GetAsync("https://api.github.com/users/" + user);
                    string apiResponse = await response.Content.ReadAsStringAsync();
                    var gitHubUser = JsonSerializer.Deserialize<GitHubUser>(apiResponse, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    if (gitHubUser != null)
                    {
                        if (string.IsNullOrEmpty(gitHubUser.login))
                        {
                            _logger.LogWarning($"GitHub API response for user '{user}' did not contain valid user information");
                            gitHubUser.login = user;
                        }
                        else
                        {
                            _logger.LogInformation("Found User " + user + " from GitHub API");
                        }
                        users.Add(gitHubUser);
                    }
                    else
                    {
                        _logger.LogWarning($"Response Deserization for user '{user}' failed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred while retrieving user information for user '{user}'" + ex.Message);
                }

            }
            return Ok(users);
        }

        [NonAction]
        public List<string> GetValidUserNames(List<string> users)
        {
            _logger.LogInformation("Validating input user lists");
            Regex regex = new Regex(@"^[a-z\d](?:[a-z\d]|-(?=[a-z\d])){0,38}$", RegexOptions.IgnoreCase);
            List<string> finalizedUses = new List<string>();
            foreach (var item in users.Distinct())
            {
                if (!string.IsNullOrWhiteSpace(item))
                {
                    if (regex.IsMatch(item))
                    {
                        finalizedUses.Add(item);
                    }
                }
            }
            finalizedUses.Sort();
            return finalizedUses;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/error-development")]
        public IActionResult HandleErrorDevelopment(
            [FromServices] IHostEnvironment hostEnvironment)
        {
            if (!hostEnvironment.IsDevelopment())
            {
                return NotFound();
            }

            var exceptionHandlerFeature =
                HttpContext.Features.Get<IExceptionHandlerFeature>()!;

            return Problem(
                detail: exceptionHandlerFeature.Error.StackTrace,
                title: exceptionHandlerFeature.Error.Message);
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [Route("/error")]
        public IActionResult HandleError() =>
            Problem();

    }
}
