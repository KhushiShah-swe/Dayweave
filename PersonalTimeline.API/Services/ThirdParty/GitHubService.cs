using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace PersonalTimeline.API.Services.ThirdParty
{
    public class GitHubService : IThirdPartyApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private const string BaseUrl = "https://api.github.com/";

        public string ProviderName => "GitHub";

        public GitHubService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
            // GitHub requires a User-Agent header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PersonalTimeline-App");
        }

        // --- FIX: Added the 'state' parameter to match the interface ---
        public string GetAuthorizationUrl(string? state = null)
        {
            // Since GitHub is used for primary login, this endpoint is largely unnecessary.
            // We return a message instructing the user to connect via the main login if they haven't.
            throw new NotSupportedException("GitHub connection is handled via primary application login.");
        }

        public Task<ApiConnection> HandleCallbackAndSaveConnectionAsync(
            ApplicationDbContext db,
            int userId,
            string code,
            string redirectUri)
        {
            // The initial GitHub OAuth flow handles the user and token.
            throw new NotImplementedException("GitHub token management is handled by ASP.NET Core middleware.");
        }

        public async Task<IEnumerable<TimelineEntry>> SyncUserDataAsync(
            ApplicationDbContext db,
            ApiConnection connection,
            int userId)
        {
            if (string.IsNullOrEmpty(connection.AccessToken))
            {
                throw new InvalidOperationException("GitHub token not found or invalid.");
            }

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);

            // 1. Get the Username first
            var userReq = await _httpClient.GetAsync($"{BaseUrl}user");
            if (!userReq.IsSuccessStatusCode)
            {
                 var error = await userReq.Content.ReadAsStringAsync();
                 throw new Exception($"GitHub User Fetch Failed: {userReq.StatusCode} - {error}");
            }

            var userJson = await userReq.Content.ReadFromJsonAsync<JsonElement>();
            var username = userJson.GetProperty("login").GetString();

            // 2. Fetch events for that specific user
            var response = await _httpClient.GetAsync($"{BaseUrl}users/{username}/events?per_page=100");

            if (!response.IsSuccessStatusCode)
            {
                 var error = await response.Content.ReadAsStringAsync();
                 throw new Exception($"GitHub Events Failed: {response.StatusCode} - {error}");
            }

            var events = await response.Content.ReadFromJsonAsync<JsonElement>();
            var newEntries = new List<TimelineEntry>();

            if (events.ValueKind == JsonValueKind.Array)
            {
                var apiEvents = events.EnumerateArray().ToList();
                var externalIds = apiEvents.Select(e => e.GetProperty("id").GetString()).ToList();

                // Check for duplicates in one batch
                var existingIds = await db.TimelineEntries
                    .Where(e => e.UserId == userId && e.SourceApi == ProviderName && externalIds.Contains(e.ExternalId))
                    .Select(e => e.ExternalId)
                    .ToListAsync();

                foreach (var githubEvent in apiEvents)
                {
                    var id = githubEvent.GetProperty("id").GetString();

                    if (existingIds.Contains(id)) continue;

                    var type = githubEvent.GetProperty("type").GetString();
                    TimelineEntry? entry = null;

                    if (type == "PushEvent")
                    {
                        var repoName = githubEvent.GetProperty("repo").GetProperty("name").GetString();
                        var payload = githubEvent.GetProperty("payload");
                        var size = payload.TryGetProperty("size", out var s) ? s.GetInt32() : 1;
                        var message = "Pushed code";

                        if (payload.TryGetProperty("commits", out var commits) && commits.ValueKind == JsonValueKind.Array && commits.GetArrayLength() > 0)
                        {
                            message = commits[0].GetProperty("message").GetString();
                        }

                        entry = new TimelineEntry
                        {
                            Title = $"Pushed {size} commit(s) to {repoName}",
                            Description = message,
                            ExternalUrl = $"https://github.com/{repoName}",
                            EntryType = size > 5 ? "Achievement" : "Activity",
                            Category = "Development",
                            ImageUrl = "https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png"
                        };
                    }
                    else if (type == "CreateEvent")
                    {
                        var repoName = githubEvent.GetProperty("repo").GetProperty("name").GetString();
                        entry = new TimelineEntry
                        {
                            Title = $"Created Repository: {repoName}",
                            Description = "Started a new project.",
                            ExternalUrl = $"https://github.com/{repoName}",
                            EntryType = "Milestone",
                            Category = "Development",
                            ImageUrl = "https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png"
                        };
                    }
                    else if (type == "WatchEvent")
                    {
                         var repoName = githubEvent.GetProperty("repo").GetProperty("name").GetString();
                         entry = new TimelineEntry
                        {
                            Title = $"Starred {repoName}",
                            Description = "Starred a repository.",
                            ExternalUrl = $"https://github.com/{repoName}",
                            EntryType = "Activity",
                            Category = "Development",
                            ImageUrl = "https://github.githubassets.com/images/modules/logos_page/GitHub-Mark.png"
                        };
                    }

                    if (entry != null)
                    {
                        entry.UserId = userId;
                        entry.SourceApi = ProviderName;
                        entry.ExternalId = id;
                        entry.EventDate = githubEvent.GetProperty("created_at").GetDateTimeOffset().UtcDateTime;
                        newEntries.Add(entry);
                    }
                }
            }

            if (newEntries.Any())
            {
                db.TimelineEntries.AddRange(newEntries);
                await db.SaveChangesAsync();
            }

            return newEntries;
        }

        public Task<bool> DisconnectApiAsync(ApiConnection connection)
        {
            return Task.FromResult(true);
        }
    }
}
