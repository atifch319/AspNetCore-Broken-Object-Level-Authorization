using BolaDemo.Api.Authentication;
using BolaDemo.Api.Authorization;
using BolaDemo.Api.Data;
using BolaDemo.Api.Hosting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

LocalEnvFile.LoadInto(builder);

builder.Services.AddDbContext<AppDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var environment = serviceProvider.GetRequiredService<IHostEnvironment>();
    options.UseSqlServer(ConnectionStringResolver.Resolve(configuration, environment));
});

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var demoAuthAllowed = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
var demoAuthEnabled = demoAuthAllowed && builder.Configuration.GetValue("DemoAuth:Enabled", false);

if (demoAuthEnabled)
{
    builder.Services.AddAuthentication(DemoAuthenticationDefaults.Scheme)
        .AddScheme<AuthenticationSchemeOptions, DemoAuthenticationHandler>(
            DemoAuthenticationDefaults.Scheme,
            _ => { });
}
else
{
    builder.Services.AddAuthentication(RejectAuthenticationDefaults.Scheme)
        .AddScheme<AuthenticationSchemeOptions, RejectAuthenticationHandler>(
            RejectAuthenticationDefaults.Scheme,
            _ => { });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PolicyNames.CanReadOrder, policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ReadOrderRequirement());
    });
});
builder.Services.AddSingleton<IAuthorizationHandler, ReadOrderAuthorizationHandler>();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BOLA Demo API",
        Version = "v1",
        Description =
            "Educational demonstration of broken object-level authorization (BOLA / IDOR). " +
            "X-Demo-User is a Development and Testing convenience for the three seeded demo emails. " +
            "It is not production authentication. GET /api/insecure/orders/{id} is intentionally vulnerable."
    });

    options.AddSecurityDefinition(DemoAuthenticationDefaults.Scheme, new OpenApiSecurityScheme
    {
        Description =
            "Development and Testing only. Send one seeded demo email: " +
            "user-a@example.com, user-b@example.com, or admin@example.com. " +
            "Arbitrary user ids are rejected. Do not use this header in production.",
        Name = DemoAuthenticationDefaults.HeaderName,
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(DemoAuthenticationDefaults.Scheme, document)] = []
    });

    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

await using var app = builder.Build();

if (demoAuthEnabled)
{
    app.Logger.LogWarning(
        "Demo authentication is enabled for {Environment}. Header {Header} accepts only the seeded demo emails. This is not production authentication.",
        app.Environment.EnvironmentName,
        DemoAuthenticationDefaults.HeaderName);
}
else
{
    app.Logger.LogWarning(
        "Demo authentication is disabled in {Environment}. Protected endpoints reject callers.",
        app.Environment.EnvironmentName);
}

if (args.Contains("--initialize", StringComparer.OrdinalIgnoreCase))
{
    if (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
    {
        app.Logger.LogError("Refusing to initialize the database outside Development or Testing.");
        Environment.ExitCode = 1;
        return;
    }

    try
    {
        await DatabaseInitializer.MigrateAndSeedAsync(app.Services, app.Logger);
    }
    catch (Exception exception)
    {
        app.Logger.LogError(exception, "Database initialization failed.");
        Environment.ExitCode = 1;
    }

    return;
}

if (app.Environment.IsDevelopment()
    && app.Configuration.GetValue("Database:InitializeOnStartup", false))
{
    app.Logger.LogInformation("Applying migrations and seed because Database:InitializeOnStartup is enabled for Development.");
    await DatabaseInitializer.MigrateAndSeedAsync(app.Services, app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "BOLA Demo API");
        options.DocumentTitle = "BOLA Demo API";
    });
}
else if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseExceptionHandler();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
await app.RunAsync();

public partial class Program;
