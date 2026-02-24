using System.Data;
using System.Security.Claims;
using System.Text;
using Dapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using UserManagement.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AzureAdOptions>(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddSingleton<PasswordHasher>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<UserRepository>();

builder.Services.AddScoped<IDbConnection>(_ =>
    new SqlConnection(builder.Configuration.GetConnectionString("Default")
                      ?? throw new InvalidOperationException("Missing SQL Server connection string.")));

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = key
        };
    })
    .AddOpenIdConnect("AzureAd", options =>
    {
        var aad = builder.Configuration.GetSection("AzureAd").Get<AzureAdOptions>() ?? new AzureAdOptions();
        options.Authority = $"https://login.microsoftonline.com/{aad.TenantId}/v2.0";
        options.ClientId = aad.ClientId;
        options.ClientSecret = aad.ClientSecret;
        options.CallbackPath = "/auth/sso/callback";
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("ng", policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

var app = builder.Build();
app.UseCors("ng");
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/api/auth/login", async (LoginRequest request, UserRepository repo, PasswordHasher hasher, TokenService tokenService) =>
{
    var user = await repo.GetByUserNameAsync(request.UserName);
    if (user is null || !hasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
    {
        return Results.Unauthorized();
    }

    var token = tokenService.CreateToken(user);
    return Results.Ok(new AuthResponse(token, user.UserName, user.Role));
});

app.MapGet("/api/auth/sso", (HttpContext context) =>
{
    var properties = new Microsoft.AspNetCore.Authentication.AuthenticationProperties { RedirectUri = "/api/auth/sso/callback" };
    return Results.Challenge(properties, new[] { "AzureAd" });
});

app.MapGet("/api/auth/sso/callback", async (HttpContext context, UserRepository repo, TokenService tokenService) =>
{
    var principal = context.User;
    var email = principal.FindFirstValue(ClaimTypes.Email) ?? principal.FindFirstValue("preferred_username");
    if (string.IsNullOrWhiteSpace(email))
    {
        return Results.BadRequest("Azure AD did not return an email claim.");
    }

    var user = await repo.GetByUserNameAsync(email);
    if (user is null)
    {
        user = await repo.CreateSsoUserAsync(email);
    }

    var jwt = tokenService.CreateToken(user);
    return Results.Redirect($"http://localhost:4200/sso-callback?token={Uri.EscapeDataString(jwt)}");
});

app.MapPost("/api/users/change-password", async (ChangePasswordRequest request, ClaimsPrincipal principal, UserRepository repo, PasswordHasher hasher) =>
{
    var userName = principal.Identity?.Name;
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.Unauthorized();
    }

    var user = await repo.GetByUserNameAsync(userName);
    if (user is null || !hasher.Verify(request.CurrentPassword, user.PasswordHash, user.PasswordSalt))
    {
        return Results.BadRequest("Current password is invalid.");
    }

    var hash = hasher.Hash(request.NewPassword);
    await repo.ChangePasswordAsync(user.Id, hash.Hash, hash.Salt);
    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/users/me", async (ClaimsPrincipal principal, UserRepository repo) =>
{
    var userName = principal.Identity?.Name;
    if (string.IsNullOrWhiteSpace(userName))
    {
        return Results.Unauthorized();
    }

    var user = await repo.GetByUserNameAsync(userName);
    return user is null ? Results.NotFound() : Results.Ok(new UserProfile(user.UserName, user.Role, user.CreatedAt));
}).RequireAuthorization();

app.MapGet("/api/admin/users", async (UserRepository repo) => Results.Ok(await repo.GetAllAsync()))
   .RequireAuthorization(policy => policy.RequireRole("Admin"));

app.Run();
