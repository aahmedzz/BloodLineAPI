using BloodLineAPI;
using BloodLineAPI.Application;
using BloodLineAPI.Filters;
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
            "http://localhost:5173",
            "http://localhost:5174",
            "https://blood-bank-system-6eaj.vercel.app"
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials(); // REQUIRED for HttpOnly cookies
    });
});

//Add the dependency injection for each layer of the application
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
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

    recurringJobManager.AddOrUpdate<AppointmentNoShowJob>(
        "appointment-no-shows",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 * * * *");

    recurringJobManager.AddOrUpdate<ChatHistoryCleanupJob>(
        "chat-history-cleanup",
        job => job.ExecuteAsync(CancellationToken.None),
        "0 3 * * *"); // Daily at 3:00 AM UTC

    recurringJobManager.AddOrUpdate<BloodBagExpiryJob>(
        "blood-bag-expiry",
        job => job.ExecuteAsync(),
        "0 0 * * *"); // Daily at midnight UTC
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
app.UseCors("WebDashboard");

app.UseAuthentication();
app.UseAuthorization();

if (recurringJobManager is not null)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
    });
}

app.MapControllers();
app.MapHub<BloodLineAPI.Hubs.AppointmentsHub>("/hubs/appointments");

app.Run();
