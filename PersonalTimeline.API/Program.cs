using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PersonalTimeline.API.BackgroundServices;
using PersonalTimeline.API.Services;
using PersonalTimeline.API.Services.ThirdParty;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
string GetFrontendUrl()
{
    var value = (configuration["Frontend:Url"] ?? "http://localhost:3000").TrimEnd('/');
    if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
        (uri.Scheme != "http" && uri.Scheme != "https") || uri.AbsolutePath != "/" ||
        !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
        throw new InvalidOperationException("Frontend:Url must be an absolute HTTP(S) origin.");
    return value;
}
string GetJwtIssuer() => configuration["Jwt:Issuer"] ?? "Dayweave";
string GetJwtKey()
{
    var value = configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) < 32)
        throw new InvalidOperationException("Set Jwt:Key to a random secret of at least 32 bytes using dotnet user-secrets or Jwt__Key.");
    return value;
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=dayweave.db"));
builder.Services.AddHttpClient<GitHubService>();
builder.Services.AddHttpClient<SpotifyService>();
builder.Services.AddHttpClient<YouTubeService>();
builder.Services.AddTransient<IThirdPartyApiService>(sp => sp.GetRequiredService<GitHubService>());
builder.Services.AddTransient<IThirdPartyApiService>(sp => sp.GetRequiredService<SpotifyService>());
builder.Services.AddTransient<IThirdPartyApiService>(sp => sp.GetRequiredService<YouTubeService>());
builder.Services.AddSingleton<OAuthStateStore>();
builder.Services.AddSingleton<SyncCoordinator>();
builder.Services.AddHostedService<SyncWorker>();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(GetFrontendUrl()).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
    ValidIssuer = GetJwtIssuer(), ValidAudience = GetJwtIssuer(), ClockSkew = TimeSpan.FromSeconds(30),
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetJwtKey()))
})
.AddCookie(options =>
{
    options.Cookie.Name = "dayweave.oauth";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
})
.AddGitHub("GitHub", options =>
{
    options.ClientId = configuration["Authentication:GitHub:ClientId"] ?? "not-configured";
    options.ClientSecret = configuration["Authentication:GitHub:ClientSecret"] ?? "not-configured";
    options.Scope.Add("user:email");
    options.CallbackPath = "/signin-github";
    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.SaveTokens = true;
    options.Events.OnRemoteFailure = context =>
    {
        context.HandleResponse();
        context.Response.Redirect($"{GetFrontendUrl()}/login?error=authentication");
        return Task.CompletedTask;
    };
});

var app = builder.Build();
// Validate the final host configuration, including integration-test overrides.
var frontendUrl = GetFrontendUrl();
var jwtIssuer = GetJwtIssuer();
var jwtKey = GetJwtKey();
var githubConfigured = !string.IsNullOrWhiteSpace(configuration["Authentication:GitHub:ClientId"]) &&
    !string.IsNullOrWhiteSpace(configuration["Authentication:GitHub:ClientSecret"]);
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Cache-Control"] = "no-store";
    await next();
});

// Apply the committed migrations, never create a second InitialCreate migration.
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

int UserId(ClaimsPrincipal user) => int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
IThirdPartyApiService? FindService(string provider, IEnumerable<IThirdPartyApiService> services) =>
    services.SingleOrDefault(s => s.ProviderName.Equals(provider, StringComparison.OrdinalIgnoreCase));
string CreateToken(User user)
{
    var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.DisplayName) };
    var token = new JwtSecurityToken(jwtIssuer, jwtIssuer, claims, expires: DateTime.UtcNow.AddHours(8),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), SecurityAlgorithms.HmacSha256));
    return new JwtSecurityTokenHandler().WriteToken(token);
}

app.MapGet("/health", () => Results.Ok(new { status = "healthy", application = "Dayweave" }));
app.MapGet("/login/github", () => githubConfigured
    ? Results.Challenge(new AuthenticationProperties { RedirectUri = "/signin-github-callback" }, new[] { "GitHub" })
    : Results.Problem("Configure GitHub OAuth credentials on the API before signing in.", statusCode: 503));
app.MapGet("/signin-github-callback", async (HttpContext context, ApplicationDbContext db) =>
{
    var auth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    if (!auth.Succeeded || auth.Principal is null) return Results.Unauthorized();
    var oauthId = auth.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
    var accessToken = auth.Properties?.GetTokenValue("access_token");
    if (string.IsNullOrWhiteSpace(oauthId) || string.IsNullOrWhiteSpace(accessToken)) return Results.Unauthorized();
    var name = auth.Principal.FindFirstValue(ClaimTypes.Name) ?? "GitHub user";
    var user = await db.Users.SingleOrDefaultAsync(u => u.OAuthProvider == "GitHub" && u.OAuthId == oauthId);
    if (user is null)
    {
        user = new User { OAuthProvider = "GitHub", OAuthId = oauthId };
        db.Users.Add(user);
    }
    user.DisplayName = name;
    user.Email = auth.Principal.FindFirstValue(ClaimTypes.Email) ?? "";
    user.ProfileImageUrl = auth.Principal.FindFirstValue("urn:github:avatar");
    user.LastLoginAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    var connection = await db.ApiConnections.SingleOrDefaultAsync(c => c.UserId == user.Id && c.ApiProvider == "GitHub");
    if (connection is null)
    {
        connection = new ApiConnection { UserId = user.Id, ApiProvider = "GitHub", LastSyncAt = DateTime.UtcNow.AddDays(-30) };
        db.ApiConnections.Add(connection);
    }
    connection.AccessToken = accessToken; connection.IsActive = true;
    await db.SaveChangesAsync();
    var token = CreateToken(user);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect($"{frontendUrl}/oauth-callback#token={token}");
});

var api = app.MapGroup("/api").RequireAuthorization();
api.MapGet("/profile", (ClaimsPrincipal user) => Results.Ok(new { id = UserId(user), displayName = user.Identity?.Name }));
api.MapGet("/timeline", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var id = UserId(user);
    var entries = await db.TimelineEntries.AsNoTracking().Where(e => e.UserId == id).OrderByDescending(e => e.EventDate).ToListAsync();
    return Results.Ok(entries.Select(TimelineMapping.ToDto));
});
api.MapPost("/timeline", async (TimelineEntryDto dto, ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var errors = EntryValidation.Validate(dto);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    var entry = new TimelineEntry { UserId = UserId(user), SourceApi = "Manual", ExternalId = Guid.NewGuid().ToString("N") };
    TimelineMapping.Apply(dto, entry);
    db.TimelineEntries.Add(entry); await db.SaveChangesAsync();
    return Results.Created($"/api/timeline/{entry.Id}", TimelineMapping.ToDto(entry));
});
api.MapPut("/timeline/{id:int}", async (int id, TimelineEntryDto dto, ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var userId = UserId(user);
    var entry = await db.TimelineEntries.SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);
    if (entry is null) return Results.NotFound();
    if (entry.SourceApi != "Manual") return Results.Forbid();
    var errors = EntryValidation.Validate(dto);
    if (errors.Count > 0) return Results.ValidationProblem(errors);
    TimelineMapping.Apply(dto, entry); await db.SaveChangesAsync(); return Results.NoContent();
});
api.MapDelete("/timeline/{id:int}", async (int id, ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var userId = UserId(user);
    var entry = await db.TimelineEntries.SingleOrDefaultAsync(e => e.Id == id && e.UserId == userId);
    if (entry is null) return Results.NotFound();
    db.TimelineEntries.Remove(entry); await db.SaveChangesAsync(); return Results.NoContent();
});
api.MapGet("/connections", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var id = UserId(user);
    return Results.Ok(await db.ApiConnections.AsNoTracking().Where(c => c.UserId == id && c.IsActive)
        .Select(c => new { c.Id, c.ApiProvider, c.LastSyncAt, c.IsActive }).ToListAsync());
});
api.MapGet("/connect/{provider}", (string provider, HttpContext context, IEnumerable<IThirdPartyApiService> services, OAuthStateStore states) =>
{
    var service = FindService(provider, services);
    if (service is null || service.ProviderName == "GitHub") return Results.BadRequest("Use GitHub sign-in, or choose Spotify or YouTube.");
    if (string.IsNullOrWhiteSpace(configuration[$"Authentication:{service.ProviderName}:ClientId"]) ||
        string.IsNullOrWhiteSpace(configuration[$"Authentication:{service.ProviderName}:ClientSecret"]))
        return Results.Problem("This provider has not been configured on the server.", statusCode: 503);
    var state = states.Create(UserId(context.User), service.ProviderName);
    context.Response.Cookies.Append($"dayweave.state.{service.ProviderName}", state, new CookieOptions
    {
        HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax,
        MaxAge = TimeSpan.FromMinutes(10), Path = "/api/connect"
    });
    return Results.Ok(new { redirectUrl = service.GetAuthorizationUrl(state) });
});
// Provider callbacks cannot use the application's bearer token. A short-lived, single-use
// nonce binds the callback to the account, provider, and initiating browser instead.
app.MapGet("/api/connect/{provider}/callback", async (string provider, string? code, string? state, string? error,
    HttpContext context, ApplicationDbContext db, IEnumerable<IThirdPartyApiService> services, OAuthStateStore states) =>
{
    var service = FindService(provider, services);
    if (service is null || service.ProviderName == "GitHub") return Results.BadRequest();
    var cookieName = $"dayweave.state.{service.ProviderName}";
    var cookie = context.Request.Cookies[cookieName];
    context.Response.Cookies.Delete(cookieName, new CookieOptions { Path = "/api/connect", Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax });
    if (string.IsNullOrEmpty(state) || string.IsNullOrEmpty(cookie) ||
        !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(state), Encoding.UTF8.GetBytes(cookie)) ||
        !states.TryConsume(state, service.ProviderName, out var userId))
        return Results.Redirect($"{frontendUrl}/connections?sync=error");
    if (!string.IsNullOrEmpty(error) || string.IsNullOrWhiteSpace(code)) return Results.Redirect($"{frontendUrl}/connections?sync=error");
    var redirectUri = configuration[$"Authentication:{service.ProviderName}:RedirectUri"];
    if (string.IsNullOrWhiteSpace(redirectUri)) return Results.Redirect($"{frontendUrl}/connections?sync=error");
    try
    {
        await service.HandleCallbackAndSaveConnectionAsync(db, userId, code, redirectUri);
        return Results.Redirect($"{frontendUrl}/connections?sync=success");
    }
    catch { return Results.Redirect($"{frontendUrl}/connections?sync=error"); }
});
api.MapPost("/sync/{provider}", async (string provider, ClaimsPrincipal user, ApplicationDbContext db,
    IEnumerable<IThirdPartyApiService> services, SyncCoordinator coordinator, CancellationToken cancellationToken) =>
{
    var service = FindService(provider, services);
    if (service is null) return Results.BadRequest("Unsupported provider.");
    using var lease = await coordinator.EnterAsync(cancellationToken);
    var id = UserId(user);
    var connection = await db.ApiConnections.SingleOrDefaultAsync(c => c.UserId == id && c.ApiProvider == service.ProviderName && c.IsActive, cancellationToken);
    if (connection is null) return Results.BadRequest("Connect this provider first.");
    try
    {
        var entries = await service.SyncUserDataAsync(db, connection, id);
        connection.LastSyncAt = DateTime.UtcNow; await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { count = entries.Count() });
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
    catch { return Results.Problem("The provider could not be synced. Check its connection and try again.", statusCode: 502); }
});
api.MapDelete("/connections/{provider}", async (string provider, ClaimsPrincipal user, ApplicationDbContext db, SyncCoordinator coordinator, CancellationToken cancellationToken) =>
{
    var canonical = new[] { "GitHub", "Spotify", "YouTube" }.SingleOrDefault(p => p.Equals(provider, StringComparison.OrdinalIgnoreCase));
    if (canonical is null) return Results.BadRequest("Unsupported provider.");
    using var lease = await coordinator.EnterAsync(cancellationToken);
    var id = UserId(user);
    var connection = await db.ApiConnections.SingleOrDefaultAsync(c => c.UserId == id && c.ApiProvider == canonical, cancellationToken);
    if (connection is not null) db.ApiConnections.Remove(connection);
    var entries = await db.TimelineEntries.Where(e => e.UserId == id && e.SourceApi == canonical).ToListAsync(cancellationToken);
    db.TimelineEntries.RemoveRange(entries); await db.SaveChangesAsync(cancellationToken);
    return Results.NoContent();
});
app.Run();
public partial class Program { }
