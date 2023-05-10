using CTWebApi.Models;
using CTWebApi.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CTWebApiTests
{
    public class GitHubUserServiceTests
    {
        public readonly GitHubUserService _gitHubUserService;
        public GitHubUserServiceTests()
        {
            _gitHubUserService = new GitHubUserService(new NullLogger<GitHubUserService>(), null);
        }

        private static GitHubUserService PrepareGitHubUserService(List<GitHubUser> users)
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

            var service = new GitHubUserService(new NullLogger<GitHubUserService>(), httpClientFactoryMock.Object);
            return service;
        }

        [Fact]
        public void GetValidUserNames_ShouldReturnSort()
        {
            var input = new List<string> { "b", "c", "a" };
            var expectedOutput = new List<string> { "a", "b", "c" };
            var output = _gitHubUserService.GetValidUserNames(input);
            Assert.Equal(expectedOutput, output);
        }


        [Fact]
        public void GetValidUserNames_ShouldHandleInvalidElements()
        {
            var input = new List<string> { "", " ", "-", "&" };
            var output = _gitHubUserService.GetValidUserNames(input);
            Assert.Empty(output);
        }

        
        [Fact]
        public void GetValidUserNames_ShouldReturnDistinct()
        {
            var input = new List<string> { "b", "b", "c" };
            var expectedOutput = new List<string> { "b", "c" };
            var output = _gitHubUserService.GetValidUserNames(input);
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
            var output = _gitHubUserService.GetValidUserNames(input);
            Assert.Equal(3, output.Count);
        }


        [Fact]
        public async Task GetGitHubUsers_ShouldHandleInvalidList()
        {
            var expectedOutput = new List<GitHubUser>() { };
            var service = PrepareGitHubUserService(new List<GitHubUser>());

            var usernames = new List<string> { "-doe", "Joj_Ki", "" };

            // Act
            var output = await service.GetGitHubUsers(usernames, 1, 10);

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output));
        }

        [Fact]
        public async Task GetGitHubUsers_ShouldHandleDuplicate()
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
            var service = PrepareGitHubUserService(expectedOutput);

            var usernames = new List<string> { "Jane", "Jane" };

            // Act
            var output = await service.GetGitHubUsers(usernames, 1, 10);

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output));
        }

        [Fact]
        public async Task GetGitHubUsers_ShouldHandleNotFound()
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

            var service = PrepareGitHubUserService(expectedOutput);

            var usernames = new List<string> { "Jane" };

            // Act
            var output = await service.GetGitHubUsers(usernames, 1, 10);

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output));
        }

        [Fact]
        public async Task GetGitHubUsers_ShouldReturnSort()
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

            var service = PrepareGitHubUserService(expectedOutput);

            var usernames = new List<string> { "johndoe", "zeta", "JaneJo" };

            // Act
            var output = await service.GetGitHubUsers(usernames, 1, 10);

            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(output));
        }

        [Fact]
        public async Task GetGitHubUsers_HandlePagingForInvalidPage()
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

            var service = PrepareGitHubUserService(expectedOutput);

            var usernames = new List<string> { "johndoe", "JaneJo", "zeta" };

            // Act
            var outputforZero = await service.GetGitHubUsers(usernames, 0, 10);
            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(outputforZero));
        }

        [Fact]
        public async Task GetGitHubUsers_HandlePagingForInvalidPageSize()
        {
            // Arrange
            var expectedOutput = new List<GitHubUser>()
            {

            };

            var service = PrepareGitHubUserService(expectedOutput);

            var usernames = new List<string> { "johndoe", "JaneJo", "zeta" };

            // Act
            var outputforNegative = await service.GetGitHubUsers(usernames, 1, -1);
            var outputforZero = await service.GetGitHubUsers(usernames, 1, 0);
            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(outputforNegative));
            Assert.Equal(JsonSerializer.Serialize(expectedOutput), JsonSerializer.Serialize(outputforZero));
        }

        [Fact]
        public async Task GetGitHubUsers_HandlePaging()
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

            var service = PrepareGitHubUserService(expectedOutput);

            var usernames = new List<string> { "johndoe", "JaneJo", "zeta" };

            // Act
            var firstPageOutput = await service.GetGitHubUsers(usernames, 1, 2);
            var secPageOutput = await service.GetGitHubUsers(usernames, 2, 2);
            // Assert
            Assert.Equal(JsonSerializer.Serialize(expectedOutput.Take(2)), JsonSerializer.Serialize(firstPageOutput));
            Assert.Equal(JsonSerializer.Serialize(expectedOutput.Skip(2)), JsonSerializer.Serialize(secPageOutput));
        }


        // Add more test cases for error handling, exceptions, etc.
    }
}
