using BloodLineAPI;
using BloodLineAPI.Application;
using BloodLineAPI.Infrastructure;
using BloodLineAPI.Middleware;

var builder = WebApplication.CreateBuilder(args);

//Add the dependency injection for each layer of the application
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
