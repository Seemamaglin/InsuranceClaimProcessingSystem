using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using InsuranceClaimSystem.Infrastructure.Configuration;
using InsuranceClaimSystem.API.Filters;
using InsuranceClaimSystem.API.Hubs;
using InsuranceClaimSystem.API.Middleware;
using InsuranceClaimSystem.API.Services;
using InsuranceClaimSystem.Application.Interfaces.External;
using InsuranceClaimSystem.Application.Interfaces.Repositories;
using InsuranceClaimSystem.Application.Interfaces.Services;
using InsuranceClaimSystem.Infrastructure.Data;
using InsuranceClaimSystem.Infrastructure.Repositories;
using InsuranceClaimSystem.Infrastructure.Services;
using InsuranceClaimSystem.Infrastructure.Services.Auth;                                        
using InsuranceClaimSystem.Infrastructure.Services.Email;                                       
using InsuranceClaimSystem.Infrastructure.Services.Encryption;                                  
                                
using InsuranceClaimSystem.Infrastructure.Services.Payment;                                     
using InsuranceClaimSystem.Infrastructure.Services.Storage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .WriteTo.Console();
});

// 2. Settings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));
builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));
builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorageSettings"));

// 3. Controllers + API Explorer
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiResponseFilterAttribute>();
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(e => e.Value != null && e.Value.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(e => InsuranceClaimSystem.Application.Common.Error.Validation(x.Key, e.ErrorMessage)))
            .ToList();

        var response = InsuranceClaimSystem.Application.Common.ApiResponse<object>.Fail("Validation Failed", errors);

        return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(response);
    };
});
builder.Services.AddEndpointsApiExplorer();

// 4. Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Insurance Claim System API",
        Version = "v1"
    });

    // Add JWT Bearer support
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// 5. DbContext
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")!));

// 6. Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IPolicyTypeRepository, PolicyTypeRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPolicyPaymentRepository, PolicyPaymentRepository>();
builder.Services.AddScoped<INomineeRepository, NomineeRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IClaimTypeRepository, ClaimTypeRepository>();
builder.Services.AddScoped<IClaimWorkflowHistoryRepository, ClaimWorkflowHistoryRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IEmailVerificationCodeRepository, EmailVerificationCodeRepository>();
builder.Services.AddScoped<IKYCDocumentRepository, KYCDocumentRepository>();

builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IClaimValidationService, ClaimValidationService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();
builder.Services.AddScoped<IPolicyService, PolicyService>();
builder.Services.AddScoped<INomineeService, NomineeService>();
builder.Services.AddScoped<IPremiumPaymentService, PremiumPaymentService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IStripeService, StripePaymentService>();
builder.Services.AddSingleton<IPiiEncryptionService, AesEncryptionService>();
builder.Services.AddSingleton<IAadhaarMaskingService, AadhaarMaskingService>();

// 7. AutoMapper
builder.Services.AddAutoMapper(
    typeof(Program).Assembly,
    typeof(InsuranceClaimSystem.Application.Common.Result).Assembly);

// 8. FluentValidation
builder.Services.AddValidatorsFromAssembly(
    typeof(InsuranceClaimSystem.Application.Common.Result).Assembly);
builder.Services.AddFluentValidationAutoValidation();

// 9. SignalR
builder.Services.AddSignalR();

// 10. Hangfire
builder.Services.AddHangfire(config =>
{
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UsePostgreSqlStorage(options =>
              options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")!));
    
    // Log job failures after retries are exhausted
    GlobalJobFilters.Filters.Add(new AutomaticRetryAttribute { LogEvents = true, OnAttemptsExceeded = AttemptsExceededAction.Fail });
});
builder.Services.AddHangfireServer();

// 11. CORS
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("StrictCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// 12. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
var key = Encoding.ASCII.GetBytes(jwtSettings?.Secret ?? "default-secret-key-must-be-32-chars!!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSettings?.Issuer ?? "InsuranceClaimSystem.API",
        ValidAudience = jwtSettings?.Audience ?? "InsuranceClaimSystem.Client",
        ClockSkew = TimeSpan.Zero
    };
    
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var result = System.Text.Json.JsonSerializer.Serialize(
                InsuranceClaimSystem.Application.Common.ApiResponse<object>.Fail(
                    "You are not authorized to access this resource.",
                    new System.Collections.Generic.List<InsuranceClaimSystem.Application.Common.Error> 
                    { 
                        InsuranceClaimSystem.Application.Common.Error.Unauthorized("Unauthorized", "Token is missing or invalid.") 
                    })
            );

            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var result = System.Text.Json.JsonSerializer.Serialize(
                InsuranceClaimSystem.Application.Common.ApiResponse<object>.Fail(
                    "You are forbidden from accessing this resource.",
                    new System.Collections.Generic.List<InsuranceClaimSystem.Application.Common.Error> 
                    { 
                        InsuranceClaimSystem.Application.Common.Error.Forbidden("Forbidden", "You do not have the required permissions.") 
                    })
            );

            return context.Response.WriteAsync(result);
        }
    };
});

// 13. Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("ClaimsManagerOnly", policy => policy.RequireRole("ClaimsManager"));
    options.AddPolicy("ClaimReviewerOnly", policy => policy.RequireRole("ClaimReviewer"));
    options.AddPolicy("FinanceOfficerOnly", policy => policy.RequireRole("FinanceOfficer"));
    options.AddPolicy("PolicyHolderOnly", policy => policy.RequireRole("PolicyHolder"));
    options.AddPolicy("ManagerOrAdmin", policy => policy.RequireRole("Admin", "ClaimsManager"));
    options.AddPolicy("ReviewerOrManager", policy => policy.RequireRole("ClaimReviewer", "ClaimsManager"));
    options.AddPolicy("StaffOnly", policy => policy.RequireRole("Admin", "ClaimsManager", "ClaimReviewer", "FinanceOfficer"));
});

// 14. Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("auth", opt =>
    {
        opt.PermitLimit = 50; // Increased from 5 to 50 for local frontend development
        opt.Window = TimeSpan.FromMinutes(15);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.AddFixedWindowLimiter("upload", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
    });
    options.AddFixedWindowLimiter("global", opt =>
    {
        opt.PermitLimit = 100;
        opt.Window = TimeSpan.FromMinutes(1);
    });
});

// 15. HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 16. Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseCors("StrictCors");
app.UseRateLimiter();

// Custom middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAdminAuthFilter() }
});

// Map controllers
app.MapControllers();

// SignalR Hubs
app.MapHub<NotificationHub>("/hubs/notifications");

// Health Checks
app.MapHealthChecks("/health");

// Apply pending migrations and seed data
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.MigrateAsync();
        await DbSeeder.SeedAsync(dbContext);
        Log.Information("Database migrated and seeded successfully.");
    }
}
catch (Exception ex)
{
    Log.Error(ex, "An error occurred during database migration or seeding.");
}

app.Run();