using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Linq;

namespace PersonalTimeline.API.Services.ThirdParty
{
    public class SpotifyService : IThirdPartyApiService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        private readonly ILogger<SpotifyService> _logger;

        // --- FIX 1: USE OFFICIAL SPOTIFY URLS ---
        private const string BaseUrl = "https://api.spotify.com/v1/";
        private const string AccountsUrl = "https://accounts.spotify.com/api/token";
        private const string AuthUrl = "https://accounts.spotify.com/authorize";

        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;

        public string ProviderName => "Spotify";

        public SpotifyService(IConfiguration config, HttpClient httpClient, ILogger<SpotifyService> logger)
        {
            _config = config;
            _httpClient = httpClient;
            _logger = logger;
            _clientId = config["Authentication:Spotify:ClientId"] ?? "";
            _clientSecret = config["Authentication:Spotify:ClientSecret"] ?? "";
            _redirectUri = config["Authentication:Spotify:RedirectUri"] ?? "";
        }

        public string GetAuthorizationUrl(string? state = null)
        {
            var scope = "user-read-recently-played user-top-read";
            var stateForUrl = state ?? Guid.NewGuid().ToString();

            // --- FIX 1B: Use Official Auth URL ---
            var builder = new UriBuilder(AuthUrl);
            var query = HttpUtility.ParseQueryString(string.Empty);
            query["client_id"] = _clientId;
            query["response_type"] = "code";
            query["redirect_uri"] = _redirectUri;
            query["scope"] = scope;
            query["state"] = stateForUrl;
            // distinct allows you to get a refresh token every time you log in (good for testing)
            query["show_dialog"] = "true";
            builder.Query = query.ToString();

            return builder.ToString();
        }

        public async Task<ApiConnection> HandleCallbackAndSaveConnectionAsync(
            ApplicationDbContext db,
            int userId,
            string code,
            string redirectUri)
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", redirectUri)
            });

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, AccountsUrl);
            request.Content = requestContent;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("Spotify token exchange failed.");
                throw new HttpRequestException($"Spotify token exchange failed. Status: {response.StatusCode}, Body: {errorBody}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

            var accessToken = tokenResponse.GetProperty("access_token").GetString();

            // Handle optional refresh token (Spotify doesn't always send it on every login unless 'show_dialog=true')
            string? refreshToken = null;
            if (tokenResponse.TryGetProperty("refresh_token", out var rtElement))
            {
                refreshToken = rtElement.GetString();
            }

            var expiresIn = tokenResponse.GetProperty("expires_in").GetInt32();

            var connection = await db.ApiConnections
                .SingleOrDefaultAsync(c => c.UserId == userId && c.ApiProvider == ProviderName);

            if (connection == null)
            {
                connection = new ApiConnection
                {
                    UserId = userId,
                    ApiProvider = ProviderName,
                    IsActive = true
                };
                db.ApiConnections.Add(connection);
            }

            connection.AccessToken = accessToken;
            // Only update refresh token if we got a new one, otherwise keep the old one
            if (!string.IsNullOrEmpty(refreshToken))
            {
                connection.RefreshToken = refreshToken;
            }

            connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            connection.LastSyncAt = DateTime.UtcNow;

            await db.SaveChangesAsync();
            return connection;
        }

        public async Task<IEnumerable<TimelineEntry>> SyncUserDataAsync(
    ApplicationDbContext db,
    ApiConnection connection,
    int userId)
{
    await RefreshTokenIfExpiredAsync(connection);

    var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}me/player/recently-played?limit=50");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);

    var response = await _httpClient.SendAsync(request);

    if (!response.IsSuccessStatusCode)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
                connection.IsActive = false;
                await db.SaveChangesAsync();
                throw new Exception("Spotify session expired. Please reconnect in Settings.");
        }
        throw new HttpRequestException($"Spotify API failed: {response.ReasonPhrase}");
    }

    var tracksResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
    var newEntries = new List<TimelineEntry>();

    if (tracksResponse.TryGetProperty("items", out var items))
    {
        var apiItems = items.EnumerateArray().ToList();

        // --- FIX START: Generate Unique IDs first ---
        // We create a dictionary of items to their NEW unique ID
        var itemWithUniqueIds = new List<(JsonElement Item, string UniqueId)>();

        foreach(var item in apiItems)
        {
            var track = item.GetProperty("track");
            var trackId = track.GetProperty("id").GetString();
            var playedAtString = item.GetProperty("played_at").GetString();

            // Create a Unique ID: "UserId_TrackId_PlayedTime"
            // This allows multiple users to listen to the same song,
            // and the same user to listen to it multiple times at different times.
            var uniqueId = $"{userId}{trackId}{playedAtString}";

            itemWithUniqueIds.Add((item, uniqueId));
        }

        // Get list of IDs we just generated
        var generatedIds = itemWithUniqueIds.Select(x => x.UniqueId).ToList();

        // Check DB for these specific Unique IDs
        var existingIds = await db.TimelineEntries
            .Where(e => e.UserId == userId && e.SourceApi == ProviderName && e.ExternalId != null && generatedIds.Contains(e.ExternalId))
            .Select(e => e.ExternalId)
            .ToListAsync();

        foreach (var (item, uniqueId) in itemWithUniqueIds)
        {
            // Skip if exists
            if (existingIds.Contains(uniqueId) || newEntries.Any(e => e.ExternalId == uniqueId))
            {
                continue;
            }

            var track = item.GetProperty("track");
            var artists = string.Join(", ", track.GetProperty("artists").EnumerateArray().Select(a => a.GetProperty("name").GetString()));

            string? albumImage = null;
            if (track.TryGetProperty("album", out var album) &&
                album.TryGetProperty("images", out var images) &&
                images.GetArrayLength() > 0)
            {
                albumImage = images.EnumerateArray().FirstOrDefault().GetProperty("url").GetString();
            }

            var listenedAt = item.GetProperty("played_at").GetDateTimeOffset().UtcDateTime;

            newEntries.Add(new TimelineEntry
            {
                UserId = userId,
                Title = $"Listened to '{track.GetProperty("name").GetString()}'",
                Description = $"Artist: {artists}",
                EventDate = listenedAt,
                EntryType = "Activity",
                Category = "Music",
                ImageUrl = albumImage,
                ExternalUrl = track.GetProperty("external_urls").GetProperty("spotify").GetString(),
                SourceApi = ProviderName,
                // USE THE NEW UNIQUE ID
                ExternalId = uniqueId,
                Metadata = JsonSerializer.Serialize(new { Artists = artists, Album = track.GetProperty("album").GetProperty("name").GetString() })
            });
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
            connection.IsActive = false;
            return Task.FromResult(true);
        }

        // --- FIX 2: ROBUST REFRESH TOKEN LOGIC ---
        private async Task RefreshTokenIfExpiredAsync(ApiConnection connection)
        {
            // 1. Safety Check: If we don't have a refresh token, we cannot refresh.
            if (string.IsNullOrEmpty(connection.RefreshToken))
            {
                _logger.LogWarning("Cannot refresh Spotify token: RefreshToken is missing.");
                return; // Or throw exception if you want to force disconnect
            }

            // 2. Check Expiry (5 minute buffer)
            if (connection.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                return;
            }

            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", connection.RefreshToken)
            });

            var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Post, AccountsUrl);
            request.Content = requestContent;
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authString);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Spotify token refresh failed.");
                // If refresh fails (e.g. user revoked access), stop trying
                if(response.StatusCode == System.Net.HttpStatusCode.BadRequest) {
                     throw new Exception("Spotify Refresh Token Invalid.");
                }
                return;
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (tokenResponse.TryGetProperty("access_token", out var token))
            {
                connection.AccessToken = token.GetString();
            }

            if (tokenResponse.TryGetProperty("refresh_token", out var newRefreshToken))
            {
                connection.RefreshToken = newRefreshToken.GetString();
            }

            if (tokenResponse.TryGetProperty("expires_in", out var expiresIn))
            {
                connection.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn.GetInt32());
            }
        }
    }
}
