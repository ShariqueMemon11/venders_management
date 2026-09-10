using System.Threading.RateLimiting;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using Vendors.Api.Configuration;
using Vendors.Api.Middleware;
using Vendors.Api.Services;
using Vendors.Application;
using Shared.Application.Common.Interfaces;
using Shared.Application.Authorization;
using Shared.Infrastructure;
using Platform.Workflow;
using Vendors.Infrastructure;
using Shared.Infrastructure.Persistence;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "Vendors.Api")
        .WriteTo.Console());

    builder.Services.AddControllers();
    builder.Services.AddFluentValidationAutoValidation();
    builder.Services.Configure<ApiBehaviorOptions>(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var problem = new ValidationProblemDetails(context.ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                Instance = context.HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
        {
            Title = "Vendors API",
            Version = "v1",
            Description = "Vendor platform API. Authorize with a JWT from POST /api/v1/Auth/login (Bearer token)."
        });

        options.AddSecurityDefinition("bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the JWT from /api/v1/Auth/login (without the 'Bearer ' prefix)."
        });
        options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("bearer", document)] = []
        });
    });

    var jwtSettings = builder.Configuration.GetRequiredJwtSettings();
    builder.Services.AddSingleton(jwtSettings);
    builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = jwtSettings.CreateTokenValidationParameters();
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = ActiveUserTokenValidator.OnTokenValidatedAsync
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        void AddPermissionPolicy(string name, string permission) =>
            options.AddPolicy(name, policy => policy.RequireClaim(AppPermissions.ClaimType, permission));

        AddPermissionPolicy("Perm.Vendor.Edit", AppPermissions.Vendor.Edit);
        AddPermissionPolicy("Perm.Vendor.Create", AppPermissions.Vendor.Create);
        AddPermissionPolicy("Perm.Vendor.Terminate", AppPermissions.Vendor.Terminate);
        AddPermissionPolicy("Perm.Vendor.Submit", AppPermissions.Vendor.Submit);
        AddPermissionPolicy("Perm.Vendor.ViewSensitive", AppPermissions.Vendor.ViewSensitive);
        AddPermissionPolicy("Perm.Workflow.View", AppPermissions.Workflow.View);
        AddPermissionPolicy("Perm.Workflow.Approve", AppPermissions.Workflow.Approve);
        AddPermissionPolicy("Perm.Compliance.Edit", AppPermissions.Compliance.Edit);
        AddPermissionPolicy("Perm.Risk.Edit", AppPermissions.Risk.Edit);
        AddPermissionPolicy("Perm.Performance.Edit", AppPermissions.Performance.Edit);
        AddPermissionPolicy("Perm.Document.Upload", AppPermissions.Document.Upload);
        AddPermissionPolicy("Perm.Document.Download", AppPermissions.Document.Download);
        AddPermissionPolicy("Perm.Document.Delete", AppPermissions.Document.Delete);
        AddPermissionPolicy("Perm.Document.Verify", AppPermissions.Document.Verify);
        AddPermissionPolicy("Perm.Contract.Edit", AppPermissions.Contract.Edit);
        AddPermissionPolicy("Perm.Report.VendorMaster", AppPermissions.Report.VendorMaster);
        AddPermissionPolicy("Perm.Report.Risk", AppPermissions.Report.Risk);
        AddPermissionPolicy("Perm.User.Manage", AppPermissions.User.Manage);
    });

    // Login brute-force protection: fixed window per client IP.
    var loginPermitLimit = builder.Configuration.GetValue("RateLimiting:Login:PermitLimit", 5);
    var loginWindowSeconds = builder.Configuration.GetValue("RateLimiting:Login:WindowSeconds", 60);
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "application/json";
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { message = "Too many login attempts. Try again later." },
                cancellationToken);
        };
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = loginPermitLimit,
                    Window = TimeSpan.FromSeconds(loginWindowSeconds),
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    });

    builder.Services.AddHttpContextAccessor();

    builder.Services.AddSharedInfrastructure(builder.Configuration);
    builder.Services.AddVendorsApplication();
    builder.Services.AddVendorsModule();
    builder.Services.AddWorkflowPlatform();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowReact", policy =>
        {
            policy.WithOrigins("http://localhost:5173", "http://localhost:5174", "http://localhost:5175")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = services.GetRequiredService<Shared.Infrastructure.Identity.IPasswordHasher>();
            var bankEncryptor = services.GetRequiredService<IBankFieldEncryptor>();
            await DbInitializer.SeedAsync(context, passwordHasher, bankEncryptor);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred while seeding the database.");
        }
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Vendors API v1");
        });
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();

    app.UseCors("AllowReact");

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.Use(async (context, next) =>
    {
        try
        {
            await next();
        }
        catch (UnauthorizedAccessException ex)
        {
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = ex.Message });
        }
    });

    app.MapControllers();
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Vendors.Api terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
