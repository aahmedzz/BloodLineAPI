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

app.UseBloodLineRecurringJobs();

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
app.UseDefaultFiles();
app.UseStaticFiles();

// Serve .well-known folder for Android App Links (assetlinks.json)
// and iOS Universal Links (apple-app-site-association) deep link verification
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
        Path.Combine(app.Environment.WebRootPath, ".well-known")),
    RequestPath = "/.well-known",
    ServeUnknownFileTypes = true, // needed for apple-app-site-association (no extension)
    DefaultContentType = "application/json"
});
app.UseCors("WebDashboard");

app.UseAuthentication();
app.UseAuthorization();

if (app.Services.GetService<IRecurringJobManager>() is not null)
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
    });
}

app.MapControllers();
app.MapHub<BloodLineAPI.Hubs.AppointmentsHub>("/hubs/appointments");

app.Run();
