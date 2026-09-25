using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using PersonalTimeline.API.Services.ThirdParty;
using Xunit;

namespace PersonalTimeline.Tests
{
    public class GitHubServiceTests
    {
        [Fact]
        public async Task SyncUserDataAsync_ShouldParsePushEvent_AndSaveToDb()
        {
            // --- 1. ARRANGE (Setup Mocks) ---

            // Setup In-Memory Database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "GitHubTestDb")
                .Options;
            var dbContext = new ApplicationDbContext(options);

            // Mock Configuration
            var mockConfig = new Mock<IConfiguration>();

            // Mock HTTP Handler to intercept requests to GitHub
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            // Mock Response for "GET /user"
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains("user") && !r.RequestUri.ToString().Contains("events")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"login\": \"testuser\"}")
                });

            // Mock Response for "GET /users/testuser/events"
            // We create a fake PushEvent JSON
            var fakeEventsJson = @"[
                {
                    ""id"": ""12345"",
                    ""type"": ""PushEvent"",
                    ""created_at"": ""2025-01-01T12:00:00Z"",
                    ""repo"": { ""name"": ""jayesh/test-repo"" },
                    ""payload"": {
                        ""size"": 3,
                        ""commits"": [ { ""message"": ""Fixed login bug"" } ]
                    }
                }
            ]";

            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.ToString().Contains("/events")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeEventsJson)
                });

            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var service = new GitHubService(httpClient, mockConfig.Object);

            var connection = new ApiConnection { AccessToken = "fake-token", UserId = 1 };

            // --- 2. ACT (Run the function) ---
            var result = await service.SyncUserDataAsync(dbContext, connection, 1);

            // --- 3. ASSERT (Verify Results) ---
            Assert.Single(result); // Should find 1 entry
            var entry = result.First();

            Assert.Equal("Pushed 3 commit(s) to jayesh/test-repo", entry.Title);
            Assert.Equal("Fixed login bug", entry.Description);
            Assert.Equal("GitHub", entry.SourceApi);
            Assert.Equal("12345", entry.ExternalId);

            // Verify it was saved to the DB
            var dbEntry = await dbContext.TimelineEntries.FirstOrDefaultAsync(e => e.ExternalId == "12345");
            Assert.NotNull(dbEntry);
        }
    }
}
