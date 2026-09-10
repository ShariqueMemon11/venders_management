using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Application.Common.Interfaces;
using Shared.Domain.Identity;
using Shared.Infrastructure.Encryption;
using Shared.Infrastructure.Identity;
using Shared.Infrastructure.Persistence;
using Vendors.Api.Configuration;
using Vendors.Api.Services;
using Vendors.Domain.Entities;
using Vendors.Tests.Infrastructure;

namespace Vendors.Tests.Integration;

/// <summary>
/// Deactivate/terminate must invalidate already-issued JWTs immediately — not only block a new login.
/// Uses the same OnTokenValidated handler Program.cs wires onto JWT Bearer.
/// </summary>
public class JwtSessionRevocationTests
{
    [Fact]
    public async Task Deactivate_RejectsExistingToken_With401()
    {
        await using var pipeline = await JwtAuthPipeline.StartAsync();

        var before = await pipeline.GetSecureAsync();
        before.StatusCode.Should().Be(HttpStatusCode.OK, "token must work while the user is still active");

        await using (var scope = pipeline.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
            var result = await users.SetActiveAsync(
                pipeline.TargetUserId,
                isActive: false,
                pipeline.TenantId,
                actorUserId: "admin@jwt-session.test");
            result.Success.Should().BeTrue();
        }

        var after = await pipeline.GetSecureAsync();
        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the same pre-issued token must fail immediately after deactivate");
    }

    [Fact]
    public async Task Terminate_RejectsExistingToken_With401()
    {
        await using var pipeline = await JwtAuthPipeline.StartAsync();

        var before = await pipeline.GetSecureAsync();
        before.StatusCode.Should().Be(HttpStatusCode.OK, "token must work while the user is still active");

        await using (var scope = pipeline.App.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<IUserManagementService>();
            var result = await users.TerminateAsync(
                pipeline.TargetUserId,
                pipeline.TenantId,
                actorUserId: "admin@jwt-session.test");
            result.Success.Should().BeTrue();
        }

        var after = await pipeline.GetSecureAsync();
        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "the same pre-issued token must fail immediately after terminate");
    }

    private sealed class JwtAuthPipeline : IAsyncDisposable
    {
        public required WebApplication App { get; init; }
        public required HttpClient Client { get; init; }
        public required Guid TargetUserId { get; init; }
        public required Guid TenantId { get; init; }

        public Task<HttpResponseMessage> GetSecureAsync() => Client.GetAsync("/secure");

        public static async Task<JwtAuthPipeline> StartAsync()
        {
            var tenantId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var databaseName = $"jwt-session-{Guid.NewGuid():N}";
            var jwtSettings = new JwtSettings
            {
                SecretKey = "local-dev-jwt-secret-at-least-32-characters-long",
                Issuer = "https://localhost:5182",
                Audience = "vendors-api"
            };

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Development
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");

            builder.Services.AddSingleton(jwtSettings);
            builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton(new FakeCurrentUserService
            {
                UserId = "admin@jwt-session.test",
                Role = "Admin",
                TenantId = tenantId
            });
            builder.Services.AddSingleton<ICurrentUserService>(sp =>
                sp.GetRequiredService<FakeCurrentUserService>());
            builder.Services.AddSingleton(new BankDataProtectionSettings
            {
                EncryptionKey = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA="
            });
            builder.Services.AddSingleton<IBankFieldEncryptor, AesGcmBankFieldEncryptor>();
            builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserManagementService, UserManagementService>();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = jwtSettings.CreateTokenValidationParameters();
                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = ActiveUserTokenValidator.OnTokenValidatedAsync
                    };
                });
            builder.Services.AddAuthorization();

            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapGet("/secure", () => Results.Ok(new { ok = true })).RequireAuthorization();

            Guid targetUserId;
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Tenants.Add(new Tenant
                {
                    Id = tenantId,
                    Name = "Acme",
                    Identifier = "acme",
                    IsActive = true,
                    CreatedBy = "test"
                });
                var target = new ApplicationUser
                {
                    Email = "viewer@jwt-session.test",
                    PasswordHash = "not-used",
                    DisplayName = "Session Viewer",
                    Role = "Viewer",
                    TenantId = tenantId,
                    IsActive = true,
                    CreatedBy = "test"
                };
                db.Users.Add(target);
                db.Users.Add(new ApplicationUser
                {
                    Email = "admin@jwt-session.test",
                    PasswordHash = "not-used",
                    DisplayName = "Session Admin",
                    Role = "Admin",
                    TenantId = tenantId,
                    IsActive = true,
                    CreatedBy = "test"
                });
                await db.SaveChangesAsync();
                targetUserId = target.Id;
            }

            var tokens = app.Services.GetRequiredService<IJwtTokenService>();
            var token = tokens.CreateToken(
                [
                    new Claim(ClaimTypes.NameIdentifier, "viewer@jwt-session.test"),
                    new Claim(ClaimTypes.Email, "viewer@jwt-session.test"),
                    new Claim(ClaimTypes.Name, "Session Viewer"),
                    new Claim(ClaimTypes.Role, "Viewer"),
                    new Claim(CurrentUserService.TenantIdClaimType, tenantId.ToString())
                ],
                DateTime.UtcNow.AddHours(24));

            await app.StartAsync();

            var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return new JwtAuthPipeline
            {
                App = app,
                Client = client,
                TargetUserId = targetUserId,
                TenantId = tenantId
            };
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.StopAsync();
            await App.DisposeAsync();
        }
    }
}
