using System.Collections.Generic;
using System.Threading.Tasks;

public interface IThirdPartyApiService
{
    string ProviderName { get; }

    // Updated to accept a state parameter
    string GetAuthorizationUrl(string? state = null);

    Task<ApiConnection> HandleCallbackAndSaveConnectionAsync(
        ApplicationDbContext db,
        int userId,
        string code,
        string redirectUri);

    Task<IEnumerable<TimelineEntry>> SyncUserDataAsync(
        ApplicationDbContext db,
        ApiConnection connection,
        int userId);

    Task<bool> DisconnectApiAsync(ApiConnection connection);
}
