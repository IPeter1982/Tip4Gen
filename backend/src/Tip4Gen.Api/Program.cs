using Serilog;
using Tip4Gen.Api.Auth;
using Tip4Gen.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<Tip4Gen.Api.Auth.CurrentUserService>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuth0(builder.Configuration);

const string DevCorsPolicy = "Dev";
builder.Services.AddCors(options =>
    options.AddPolicy(DevCorsPolicy, p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseCors(DevCorsPolicy);
}

app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
