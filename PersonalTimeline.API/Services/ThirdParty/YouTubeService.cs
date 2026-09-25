using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace PersonalTimeline.API.Services.ThirdParty
{
    public class YouTubeService : IThirdPartyApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<YouTubeService> _logger;

        private const string AuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenUrl = "https://oauth2.googleapis.com/token";
        private const string BaseUrl = "https://www.googleapis.com/youtube/v3/";

        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;

        public string ProviderName => "YouTube";

        public YouTubeService(IConfiguration config, HttpClient httpClient, ILogger<YouTubeService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
            _clientId = config["Authentication:YouTube:ClientId"] ?? "";
            _clientSecret = config["Authentication:YouTube:ClientSecret"] ?? "";
            _redirectUri = config["Authentication:YouTube:RedirectUri"] ?? "";
        }

        public string GetAuthorizationUrl(string? state = null)
        {
            // Scope for reading liked videos
            var scope = "https://www.googleapis.com/auth/youtube.readonly";
            var stateForUrl = state ?? Guid.NewGuid().ToString();

            return $"{AuthUrl}?client_id={_clientId}&redirect_uri={_redirectUri}&response_type=code&scope={scope}&access_type=offline&prompt=consent&state={stateForUrl}";
        }

        public async Task<ApiConnection> HandleCallbackAndSaveConnectionAsync(ApplicationDbContext db, int userId, string code, string redirectUri)
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            var response = await _httpClient.PostAsync(TokenUrl, requestContent);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"YouTube Auth Failed: {response.StatusCode} - {error}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var accessToken = json.GetProperty("access_token").GetString();
            var refreshToken = json.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
            var expiresIn = json.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;

            var connection = await db.ApiConnections.SingleOrDefaultAsync(c => c.UserId == userId && c.ApiProvider == ProviderName);
            if (connection == null)
            {
                connection = new ApiConnection { UserId = userId, ApiProvider = ProviderName, IsActive = true };
                db.ApiConnections.Add(connection);
            }
            connection.AccessToken = accessToken;
            if (!string.IsNullOrEmpty(refreshToken)) connection.RefreshToken = refreshToken;
            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            connection.LastSyncAt = DateTime.UtcNow.AddDays(-30); // Sync last 30 days

            await db.SaveChangesAsync();
            return connection;
        }

        public async Task<IEnumerable<TimelineEntry>> SyncUserDataAsync(ApplicationDbContext db, ApiConnection connection, int userId)
        {
            await RefreshTokenIfExpiredAsync(connection);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);

            // Fetch "Liked" videos playlist (ID 'LL' stands for Liked List)
            var response = await _httpClient.GetAsync($"{BaseUrl}playlistItems?part=snippet&playlistId=LL&maxResults=10");

            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            var entries = new List<TimelineEntry>();

            if (json.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    try
                    {
                        var snippet = item.GetProperty("snippet");
                        var resourceId = snippet.GetProperty("resourceId");
                        var videoId = resourceId.GetProperty("videoId").GetString();

                        if (!await db.TimelineEntries.AnyAsync(e => e.UserId == userId && e.ExternalId == videoId && e.SourceApi == ProviderName))
                        {
                            var title = snippet.GetProperty("title").GetString();
                            // Skip deleted videos
                            if (title == "Deleted video" || title == "Private video") continue;

                            var channel = snippet.GetProperty("videoOwnerChannelTitle").GetString();
                            var date = snippet.GetProperty("publishedAt").GetDateTimeOffset().UtcDateTime;

                            string? thumb = null;
                            if (snippet.TryGetProperty("thumbnails", out var thumbs))
                            {
                                if (thumbs.TryGetProperty("medium", out var m)) thumb = m.GetProperty("url").GetString();
                                else if (thumbs.TryGetProperty("default", out var d)) thumb = d.GetProperty("url").GetString();
                            }

                            entries.Add(new TimelineEntry
                            {
                                UserId = userId,
                                Title = $"Liked Video: {title}",
                                Description = $"Channel: {channel}",
                                EventDate = date,
                                EntryType = "Activity",
                                Category = "Entertainment",
                                SourceApi = ProviderName,
                                ExternalId = videoId,
                                ExternalUrl = $"https://www.youtube.com/watch?v={videoId}",
                                ImageUrl = thumb
                            });
                        }
                    }
                    catch { /* Skip bad items */ }
                }
                db.TimelineEntries.AddRange(entries);
                await db.SaveChangesAsync();
            }
            return entries;
        }

        public Task<bool> DisconnectApiAsync(ApiConnection connection) { connection.IsActive = false; return Task.FromResult(true); }

        private async Task RefreshTokenIfExpiredAsync(ApiConnection connection)
        {
            if (connection.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5)) return;
            if (string.IsNullOrEmpty(connection.RefreshToken)) return;

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("client_id", _clientId),
                new KeyValuePair<string, string>("client_secret", _clientSecret),
                new KeyValuePair<string, string>("refresh_token", connection.RefreshToken),
                new KeyValuePair<string, string>("grant_type", "refresh_token")
            });

            var response = await _httpClient.PostAsync(TokenUrl, requestContent);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadFromJsonAsync<JsonElement>();
                connection.AccessToken = json.GetProperty("access_token").GetString();
                var expiresIn = json.GetProperty("expires_in").GetInt32();
                connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            }
        }
    }
}
