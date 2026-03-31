using FinanceFlow.Workers;
using FinanceFlow.Workers.Jobs;
using FinanceFlow.Workers.Services;
using Quartz;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// Serilog
builder.Services.AddSerilog(config =>
    config.ReadFrom.Configuration(builder.Configuration));

// HttpClient para a API
builder.Services.AddHttpClient("FinanceFlowApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Serviços do Worker
builder.Services.AddSingleton<ApiAuthService>();
builder.Services.AddSingleton<BudgetAlertService>();
builder.Services.AddSingleton<ReportGeneratorService>();

// Quartz
builder.Services.AddQuartz(q =>
{
    var jobKey = new JobKey("BudgetAlertConsumerJob");

    q.AddJob<BudgetAlertConsumerJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("BudgetAlertConsumerJob-trigger")
        .WithSimpleSchedule(s => s
            .WithIntervalInSeconds(30)
            .RepeatForever()));


    var reportJobKey = new JobKey("ReportConsumerJob");

    q.AddJob<ReportConsumerJob>(opts => opts.WithIdentity(reportJobKey));

    q.AddTrigger(opts => opts
        .ForJob(reportJobKey)
        .WithIdentity("ReportConsumerJob-trigger")
        .WithSimpleSchedule(s => s
            .WithIntervalInSeconds(30)
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
