using CTWebApi.Controllers;
using CTWebApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CTWebApiTests
{
    public class GitHubUserControllerTests
    {
        public readonly GitHubUserController _gitHubController;
        public GitHubUserControllerTests()
        {
            _gitHubController = new GitHubUserController(new NullLogger<GitHubUserController>(), null);
        }

        private static GitHubUserController PrepareControllerforPostAsync(List<GitHubUser> users)
        {
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var sequence = httpMessageHandlerMock.Protected()
                .SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());

            foreach (var user in users)
            {
                sequence = sequence.ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(JsonSerializer.Serialize(user), Encoding.UTF8, "application/json")
                });
            }

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);

            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => httpClient);

            var controller = new GitHubUserController(new NullLogger<GitHubUserController>(), httpClientFactoryMock.Object);
            return controller;
        }

        [Fact]
        public void GetValidUserNames_ShouldReturnSort()
        {
            var input = new List<string> { "b", "c", "a" };
            var expectedOutput = new List<string> { "a", "b", "c" };
            var output = _gitHubController.GetValidUserNames(input);
            Assert.Equal(expectedOutput, output);
        }


        [Fact]
        public void GetValidUserNames_ShouldHandleInvalidElements()
        {
            var input = new List<string> { "", " ", "-", "&" };
            var output = _gitHubController.GetValidUserNames(input);
            Assert.Empty(output);
        }

        [Fact]
        public void GetValidUserNames_ShouldReturnDistinct()
        {
            var input = new List<string> { "b", "b", "c" };
            var expectedOutput = new List<string> { "b", "c" };
            var output = _gitHubController.GetValidUserNames(input);
            Assert.Equal(expectedOutput, output);
        }

        [Fact]
        public void GetValidUserNames_ShouldHandleNameWith39chars()
        {
            var input = new List<string> {
                "abcdefghijklmnopqrstuvwxyz1234567890123",
                "b",
                "c"
            };
            var output = _gitHubController.GetValidUserNames(input);
            Assert.Equal(3, output.Count);
        }


        [Fact]
        public void GitHubUser_ForFlowersPerRepos()
        {
            // Arrange
            var input = new List<GitHubUser>
            {
                new GitHubUser
                {
                    login = "jane-doe",
                    name = "Jane Doe",
                    public_repos = 10,
                    followers = 100,
                    company = "abc"
                },
                new GitHubUser
                {
                    login = "johndoe",
                    name = "John Doe",
                    public_repos = 21,
                    followers = 200,
                    company = "bcd"
                },
                new GitHubUser
                {
                    login = "johndoe",
                    name = "John Doe",
                    public_repos = 0,
                    followers = 200,
                    company = "bcd"
                }
            };
            Assert.Equal(10, input[0].flowers_per_p_repos);
            Assert.Equal(9, input[1].flowers_per_p_repos);
            Assert.Equal(0, input[2].flowers_per_p_repos);
        }


        [Fact]
        public async Task PostAsync_ShouldHandleInvalidList()
        {
            var expectedOutput = new List<GitHubUser>() { };
            var controller = PrepareControllerforPostAsync(new List<GitHubUser>());

            var usernames = new List<string> { "-doe", "Joj_Ki", "" };

            // Act
            var output = await controller.PostAsync(usernames, 1, 10) as OkObjectResult;

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output?.Value));
        }


        [Fact]
        public async Task PostAsync_ShouldHandleDuplicate()
        {
            // Arrange
            var expectedOutput = new List<GitHubUser>
            {
                new GitHubUser
                {
                    login = "Jane",
                    name = "Jane Doe",
                    public_repos = 10,
                    followers = 100,
                    company = "bcd"
                }
            };
            var controller = PrepareControllerforPostAsync(expectedOutput);

            var usernames = new List<string> { "Jane", "Jane" };

            // Act
            var output = await controller.PostAsync(usernames, 1, 10) as OkObjectResult;

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output?.Value));
        }

        [Fact]
        public async Task PostAsync_ShouldHandleNotFound()
        {
            // Arrange
            var expectedOutput = new List<GitHubUser>
            {
                new GitHubUser
                {
                    login = "Jane",
                    name = null,
                    public_repos = 0,
                    followers = 0,
                    company = ""
                }
            };

            var controller = PrepareControllerforPostAsync(expectedOutput);

            var usernames = new List<string> { "Jane" };

            // Act
            var output = await controller.PostAsync(usernames, 1, 10) as OkObjectResult;

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output?.Value));
        }

        [Fact]
        public async Task PostAsync_ShouldReturnSort()
        {
            // Arrange
            var expectedOutput = new List<GitHubUser>
            {
                new GitHubUser
                {
                    login = "JaneJo",
                    name = "Jane Jo",
                    public_repos = 10,
                    followers = 100,
                    company = "bcd"
                },
                new GitHubUser
                {
                    login = "johndoe",
                    name = "John Doe",
                    public_repos = 20,
                    followers = 200,
                    company = "bcd"
                },
                new GitHubUser
                {
                    login = "zeta",
                    name = "zeta",
                    public_repos = 20,
                    followers = 200,
                    company = "bcd"
                }
            };

            var controller = PrepareControllerforPostAsync(expectedOutput);

            var usernames = new List<string> { "johndoe", "zeta", "JaneJo" };

            // Act
            var output = await controller.PostAsync(usernames, 1, 10) as OkObjectResult;

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output?.Value));
        }

        [Fact]
        public async Task PostAsync_HandlePagingForInvalidPage()
        {
            // Arrange
            var expectedOutput = new List<GitHubUser>
            {
                new GitHubUser
                {
                    login = "JaneJo",
                    name = "Jane Jo",
                    public_repos = 10,
                    followers = 100,
                    company = "bcd"
                },
                new GitHubUser
                {
                    login = "johndoe",
                    name = "John Doe",
                    public_repos = 20,
                    followers = 200,
                    company = "bcd"
                },
                new GitHubUser
                {
                    login = "zeta",
                    name = "zeta",
                    public_repos = 20,
                    followers = 200,
                    company = "bcd"
                }
            };

            var controller = PrepareControllerforPostAsync(expectedOutput);

            var usernames = new List<string> { "johndoe", "JaneJo", "zeta" };

            // Act
            var outputforZero = await controller.PostAsync(usernames, 0, 10) as OkObjectResult;
            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(outputforZero?.Value));
        }

        [Fact]
        public async Task PostAsync_HandlePagingForInvalidPageSize()
        {
            // Arrange
            var expectedOutput = new List<GitHubUser>()
            {

            };

            var controller = PrepareControllerforPostAsync(expectedOutput);

            var usernames = new List<string> { "johndoe", "JaneJo", "zeta" };

            // Act
            var outputforNegative = await controller.PostAsync(usernames, 1, -1) as OkObjectResult;
            var outputforZero = await controller.PostAsync(usernames, 1, 0) as OkObjectResult;
            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(outputforNegative?.Value));
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(outputforZero?.Value));
        }

        [Fact]
        public async Task PostAsync_HandlePaging()
        {
            // Arrange
            var expectedOutput = new List<GitHubUser>
            {
                new GitHubUser
                {
                    login = "JaneJo",
                    name = "Jane Jo",
                    public_repos = 10,
                    followers = 100,
                    company = "bcd"
                },
                new GitHubUser
                {
                    login = "johndoe",
                    name = "John Doe",
                    public_repos = 20,
                    followers = 200,
                    company = "bcd"
                },
                new GitHubUser
                {
                    login = "zeta",
                    name = "zeta",
                    public_repos = 20,
                    followers = 200,
                    company = "bcd"
                }
            };

            var controller = PrepareControllerforPostAsync(expectedOutput);

            var usernames = new List<string> { "johndoe", "JaneJo", "zeta" };

            // Act
            var firstPageOutput = await controller.PostAsync(usernames, 1, 2) as OkObjectResult;
            var secPageOutput = await controller.PostAsync(usernames, 2, 2) as OkObjectResult;
            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput.Take(2)), JsonSerializer.Serialize(firstPageOutput?.Value));
            Assert.Equal(JsonSerializer.Serialize(expectedOutput.Skip(2)), JsonSerializer.Serialize(secPageOutput?.Value));
        }
    }
}
