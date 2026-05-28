using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Tip4Gen.Api.Auth;
using Tip4Gen.Api.Workers;
using Tip4Gen.Infrastructure;
using Tip4Gen.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://*:{port}");

builder.Host.UseSerilog((context, _, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Tip4Gen.Api.Auth.CurrentUserService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuth0(builder.Configuration);

builder.Services.AddOptions<FixturePollerOptions>()
    .Bind(builder.Configuration.GetSection(FixturePollerOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<TeamLockJobOptions>()
    .Bind(builder.Configuration.GetSection(TeamLockJobOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddOptions<AiTippingJobOptions>()
    .Bind(builder.Configuration.GetSection(AiTippingJobOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<FixturePoller>();
builder.Services.AddHostedService<TeamLockJob>();
builder.Services.AddHostedService<AiTippingJob>();

const string DevCorsPolicy = "Dev";
const string ProdCorsPolicy = "Prod";
builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod());

    var allowedOrigin = builder.Configuration["Cors:AllowedOrigin"];
    if (!string.IsNullOrWhiteSpace(allowedOrigin))
    {
        options.AddPolicy(ProdCorsPolicy, p => p
            .WithOrigins(allowedOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod());
    }
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevCorsPolicy);
}
else if (!string.IsNullOrWhiteSpace(builder.Configuration["Cors:AllowedOrigin"]))
{
    app.UseCors(ProdCorsPolicy);
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
