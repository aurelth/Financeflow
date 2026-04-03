using FinanceFlow.Workers;
using FinanceFlow.Workers.Jobs;
using FinanceFlow.Workers.Services;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
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
builder.Services.AddSingleton<NotificationDispatchService>();

// Quartz
builder.Services.AddQuartz(q =>
{
    // BudgetAlertConsumerJob
    var jobKey = new JobKey("BudgetAlertConsumerJob");
    q.AddJob<BudgetAlertConsumerJob>(opts => opts.WithIdentity(jobKey));
    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("BudgetAlertConsumerJob-trigger")
        .WithSimpleSchedule(s => s
            .WithIntervalInSeconds(30)
            .RepeatForever()));

    // ReportConsumerJob
    var reportJobKey = new JobKey("ReportConsumerJob");
    q.AddJob<ReportConsumerJob>(opts => opts.WithIdentity(reportJobKey));
    q.AddTrigger(opts => opts
        .ForJob(reportJobKey)
        .WithIdentity("ReportConsumerJob-trigger")
        .WithSimpleSchedule(s => s
            .WithIntervalInSeconds(5)
            .RepeatForever()));

    // MonthlyReportJob
    var monthlyReportJobKey = new JobKey("MonthlyReportJob");
    q.AddJob<MonthlyReportJob>(opts => opts.WithIdentity(monthlyReportJobKey));
    q.AddTrigger(opts => opts
        .ForJob(monthlyReportJobKey)
        .WithIdentity("MonthlyReportJob-trigger")
        .WithCronSchedule("0 0 1 * * ?",
            x => x.InTimeZone(TimeZoneInfo.Utc)));

    // NotificationConsumerJob
    var notificationJobKey = new JobKey("NotificationConsumerJob");
    q.AddJob<NotificationConsumerJob>(opts => opts.WithIdentity(notificationJobKey));
    q.AddTrigger(opts => opts
        .ForJob(notificationJobKey)
        .WithIdentity("NotificationConsumerJob-trigger")
        .WithSimpleSchedule(s => s
            .WithIntervalInSeconds(10)
            .RepeatForever()));

    // TransactionDueAlertJob — roda a cada hora
    var dueAlertJobKey = new JobKey("TransactionDueAlertJob");
    q.AddJob<TransactionDueAlertJob>(opts => opts.WithIdentity(dueAlertJobKey));
    q.AddTrigger(opts => opts
        .ForJob(dueAlertJobKey)
        .WithIdentity("TransactionDueAlertJob-trigger")
        .WithSimpleSchedule(s => s
            .WithIntervalInHours(1)
            .RepeatForever()));
});

builder.Services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// Garante que os tópicos Kafka existem antes de iniciar
var bootstrapServers = builder.Configuration["Kafka:BootstrapServers"]!;
var topics = new[]
{
    builder.Configuration["Kafka:Topics:TransactionCreated"]  ?? "finance.transactions.created",
    builder.Configuration["Kafka:Topics:BudgetAlerts"]        ?? "finance.budget.alerts",
    builder.Configuration["Kafka:Topics:NotificationsCreated"] ?? "finance.notifications.created",
    builder.Configuration["Kafka:Topics:ReportsRequested"]    ?? "finance.reports.requested",
};

using var adminClient = new AdminClientBuilder(
    new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

foreach (var topic in topics)
{
    try
    {
        await adminClient.CreateTopicsAsync(new[]
        {
            new TopicSpecification
            {
                Name              = topic,
                NumPartitions     = 1,
                ReplicationFactor = 1,
            }
        });
    }
    catch (CreateTopicsException ex)
        when (ex.Results[0].Error.Code == ErrorCode.TopicAlreadyExists)
    {
        // Tópico já existe — ignorar 
    } 
}

host.Run();
