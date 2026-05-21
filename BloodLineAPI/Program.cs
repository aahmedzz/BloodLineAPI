using BloodLineAPI;
using BloodLineAPI.Application;
using BloodLineAPI.Infrastructure;
using BloodLineAPI.Infrastructure.BackgroundJobs;
using BloodLineAPI.Infrastructure.Seeding;
using BloodLineAPI.Middleware;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebDashboard", policy =>
    {
        policy.WithOrigins(
            "http://localhost:3000",
            "http://localhost:5173"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // REQUIRED for HttpOnly cookies
    });
});

//Add the dependency injection for each layer of the application
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

// Seed default admin account for testing
await AdminAccountSeeder.SeedAdminAccountAsync(app.Services);

var recurringJobManager = app.Services.GetService<IRecurringJobManager>();
if (recurringJobManager is not null)
{
    recurringJobManager.AddOrUpdate<AppointmentReminderJob>(
        "appointment-reminders",
        job => job.ExecuteAsync(CancellationToken.None),
        "*/15 * * * *");

    recurringJobManager.AddOrUpdate<ChatHistoryCleanupJob>(
        "chat-history-cleanup",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 3 * * *"); // Daily at 3:00 AM UTC
}

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

//if (app.Environment.IsDevelopment())
//{
app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "BloodLine API v1");
});
//}

app.UseHttpsRedirection();
if (recurringJobManager is not null)
{
    app.UseHangfireDashboard("/hangfire");
}

app.UseCors("WebDashboard");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
