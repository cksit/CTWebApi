using CTWebApi.Controllers;
using CTWebApi.Models;
using CTWebApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace CTWebApiTests
{
    public class GitHubUserControllerTests
    {
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
        public async Task PostAsync_ValidInput_ReturnsOkResultWithUsers()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<GitHubUserController>>();
            var gitHubUserServiceMock = new Mock<IGitHubUserService>();
            var controller = new GitHubUserController(loggerMock.Object, gitHubUserServiceMock.Object);

            var usernames = new List<string> { "JaneJo", "zeta", "johndoe" };
            var expectedUsers = new List<GitHubUser> {
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
            gitHubUserServiceMock.Setup(service => service.GetGitHubUsers(usernames, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(expectedUsers);

            // Act
            var result = await controller.PostAsync(usernames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualUsers = Assert.IsAssignableFrom<List<GitHubUser>>(okResult.Value);
            Assert.Equal(expectedUsers, actualUsers);
        }

        
        [Fact]
        public async Task PostAsync_InvalidUsernameFormat_ReturnsEmptyList()
        {
            // Arrange
            var loggerMock = new Mock<ILogger<GitHubUserController>>();
            var gitHubUserServiceMock = new Mock<IGitHubUserService>();
            var controller = new GitHubUserController(loggerMock.Object, gitHubUserServiceMock.Object);

            var invalidUsernames = new List<string> { "*invalid-user1", "#invalid-user2" };

            gitHubUserServiceMock.Setup(service => service.GetGitHubUsers(invalidUsernames, It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new List<GitHubUser>());

            // Act
            var result = await controller.PostAsync(invalidUsernames);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var actualUsers = Assert.IsAssignableFrom<List<GitHubUser>>(okResult.Value);
            Assert.Empty(actualUsers);
        }

    }
}
