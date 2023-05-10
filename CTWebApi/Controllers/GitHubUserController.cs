using CTWebApi.Models;
using CTWebApi.Services;
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
        private readonly IGitHubUserService _gitHubUserService;

        public GitHubUserController(ILogger<GitHubUserController> logger, 
                                    IGitHubUserService gitHubUserService)
        {
            _logger = logger;
            _gitHubUserService = gitHubUserService;
        }
        /*
        [HttpGet("Throw")]
        public IActionResult Throw() =>
            throw new Exception("Sample exception.");
        */
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] List<string> usernames,
                                                        [FromQuery] int page = 1,
                                                        [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Start processing PostAsync action");

            List<GitHubUser> users = await _gitHubUserService.GetGitHubUsers(usernames, page, pageSize);

            return Ok(users);
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
