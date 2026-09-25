using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace PersonalTimeline.Tests;

public class ApiIntegrationTests : IClassFixture<DayweaveFactory>
{
    private readonly DayweaveFactory _factory;
    public ApiIntegrationTests(DayweaveFactory factory) => _factory = factory;
    [Fact]
    public async Task AnonymousRequestsCannotReadWriteOrDeleteActivity()
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/timeline")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/timeline/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/timeline", Moment())).StatusCode);
    }
    [Fact]
    public async Task ManualCrudValidatesAndIsolatesAccounts()
    {
        using var owner = _factory.ForUser(1); using var other = _factory.ForUser(2);
        var response = await owner.PostAsJsonAsync("/api/timeline", Moment());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<TimelineEntryDto>())!;
        Assert.Equal("Manual", saved.SourceApi); Assert.True(saved.Id > 0);
        Assert.Contains("Z", await response.Content.ReadAsStringAsync());
        Assert.DoesNotContain((await other.GetFromJsonAsync<TimelineEntryDto[]>("/api/timeline"))!, e => e.Id == saved.Id);
        Assert.Equal(HttpStatusCode.NotFound, (await other.PutAsJsonAsync($"/api/timeline/{saved.Id}", Moment())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await other.DeleteAsync($"/api/timeline/{saved.Id}")).StatusCode);
        var blank = Moment(); blank.Title = " ";
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PutAsJsonAsync($"/api/timeline/{saved.Id}", blank)).StatusCode);
        var updated = Moment(); updated.Title = "Updated moment";
        Assert.Equal(HttpStatusCode.NoContent, (await owner.PutAsJsonAsync($"/api/timeline/{saved.Id}", updated)).StatusCode);
        Assert.Contains((await owner.GetFromJsonAsync<TimelineEntryDto[]>("/api/timeline"))!, e => e.Id == saved.Id && e.Title == "Updated moment");
        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/timeline/{saved.Id}")).StatusCode);
        Assert.DoesNotContain((await owner.GetFromJsonAsync<TimelineEntryDto[]>("/api/timeline"))!, e => e.Id == saved.Id);
    }
    [Fact]
    public async Task ForgedOauthStateDoesNotCreateAConnection()
    {
        using var client = _factory.CreateClient(new() { AllowAutoRedirect = false });
        var result = await client.GetAsync("/api/connect/Spotify/callback?state=1&code=fake");
        Assert.Equal(HttpStatusCode.Redirect, result.StatusCode);
        Assert.Contains("sync=error", result.Headers.Location!.ToString());
    }
    [Fact]
    public async Task SameExternalEventCanBelongToTwoUsersButCannotDuplicateForOne()
    {
        using var first = _factory.ForUser(1); using var second = _factory.ForUser(2);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var externalId = Guid.NewGuid().ToString();
        TimelineEntry Item(int user) => new() { UserId = user, Title = "Shared event", SourceApi = "GitHub", ExternalId = externalId, EntryType = "Activity", EventDate = DateTime.UtcNow };
        db.TimelineEntries.AddRange(Item(1), Item(2)); await db.SaveChangesAsync();
        db.TimelineEntries.Add(Item(1));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }
    private static TimelineEntryDto Moment() => new() { Title = "A test milestone", Description = "Integration test", EventDate = DateTime.UtcNow, EntryType = "Memory", Category = "Personal", SourceApi = "GitHub" };
}

public sealed class DayweaveFactory : WebApplicationFactory<Program>
{
    // Deliberately public test-only signing material; never used outside this fixture.
    private const string TestKey = "dayweave-integration-test-only-signing-key-0123456789";
    private readonly string _database = Path.Combine(Path.GetTempPath(), $"dayweave-tests-{Guid.NewGuid()}.db");
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = TestKey, ["Jwt:Issuer"] = "DayweaveTests", ["AllowedHosts"] = "*", ["Sync:Enabled"] = "false",
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={_database}"
        }));
    }
    public HttpClient ForUser(int id)
    {
        var client = CreateClient();
        using (var scope = Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!db.Users.Any(u => u.Id == id)) { db.Users.Add(new User { Id = id, OAuthProvider = "Test", OAuthId = id.ToString(), DisplayName = $"User {id}", Email = "test@example.invalid" }); db.SaveChanges(); }
        }
        var token = new JwtSecurityToken("DayweaveTests", "DayweaveTests", new[] { new Claim(ClaimTypes.NameIdentifier, id.ToString()) },
            expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        return client;
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_database)) File.Delete(_database);
    }
}
