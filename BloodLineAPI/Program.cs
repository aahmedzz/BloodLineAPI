using BloodLineAPI;
using BloodLineAPI.Application;
using BloodLineAPI.Infrastructure;
using BloodLineAPI.Infrastructure.BackgroundJobs;
using BloodLineAPI.Middleware;
using Hangfire;

var builder = WebApplication.CreateBuilder(args);

//Add the dependency injection for each layer of the application
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

var recurringJobManager = app.Services.GetService<IRecurringJobManager>();
if (recurringJobManager is not null)
{
    recurringJobManager.AddOrUpdate<AppointmentReminderJob>(
        "appointment-reminders",
        job => job.ExecuteAsync(CancellationToken.None),
        "*/15 * * * *");
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
