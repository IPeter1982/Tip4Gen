using Tip4Gen.Infrastructure;
using Tip4Gen.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

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

var host = builder.Build();
host.Run();
