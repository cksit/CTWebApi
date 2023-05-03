using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTWebApi_Test
{
    public class Unittest
    {
        [Fact]
        public async Task HttpClient_Retry_Policy_Test()
        {
            // Arrange
            var expectedRetries = 3;
            var httpClient = new HttpClient(new RetryDelegatingHandler(expectedRetries))
            {
                BaseAddress = new Uri("https://example.com/")
            };

            // Act
            var stopwatch = Stopwatch.StartNew();
            var response = await httpClient.GetAsync("api/test");
            stopwatch.Stop();

            // Assert
            Assert.False(response.IsSuccessStatusCode);
            Assert.Equal(expectedRetries + 1, httpClient.DefaultRequestHeaders.GetValues("retry-count").Count());
            Assert.True(stopwatch.Elapsed >= TimeSpan.FromSeconds(expectedRetries * 2));
        }

    }

    public class RetryDelegatingHandler : DelegatingHandler
    {
        private readonly int maxRetries;
        private readonly TimeSpan delay = TimeSpan.FromSeconds(2);
        private int retries = 0;

        public RetryDelegatingHandler(int maxRetries)
        {
            this.maxRetries = maxRetries;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Contains("retry-count"))
            {
                request.Headers.GetValues("retry-count").First().TryParse(out retries);
            }

            HttpResponseMessage response = null;

            do
            {
                response = await base.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode && retries < maxRetries)
                {
                    retries++;
                    request.Headers.Add("retry-count", retries.ToString());
                    await Task.Delay(delay);
                }
                else
                {
                    break;
                }

            } while (true);

            return response;
        }
    }
}
