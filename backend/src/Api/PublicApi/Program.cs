using Carter;
using PublicApi.Infrastructure;
using DotNetEnv;
using Scalar.AspNetCore;
using PublicApi.Common.Extensions;
using PublicApi.Infrastructure.Notifications;
using Modules.Shared;
using Modules.Shared.Infrastructure;
using Modules.Identity;
using Modules.Attendence;
using Modules.Employees;
using PublicApi.Infrastructure.Payments;


Env.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSharedModule(
    typeof(Program).Assembly,
    typeof(Modules.Identity.Application.Users.Register).Assembly);

builder.Services.AddProblemDetails();

var connectionString = Environment.GetEnvironmentVariable("DefaultConnectionLocal");

builder.Services.AddSharedInfrastructure(connectionString!);

builder.Services.AddPayments();


builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddIdentityModule();

builder.Services.AddEmployeeModule();

builder.Services.AddAttendenceModule(connectionString!);

builder.Services.AddCarter();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
              "https://veterinary-app-nu.vercel.app",
              "http://localhost",
              "http://localhost:5173",
              "http://localhost:5174",
              "https://www.aviavet.online",
              "http://localhost:3000",
              "http://localhost:3001"
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalHost", policy =>
        policy
            .WithOrigins("https://localhost")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});


//builder.WebHost.ConfigureKestrel(options =>
//{
//    options.AllowSynchronousIO = true;
//});

builder.Services.AddAuthorization();



var app = builder.Build();
AppSettings.appMode = AppMode.Production;

// Global API prefix

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    AppSettings.appMode = AppMode.Dev;
}
app.ApplyMigrations();
app.UseHttpsRedirection();


app.UseCors("AllowFrontend");

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.MapHub<NotificationHub>("/hubs/notification");

app.UsePayments();

var api = app.MapGroup("/api/v1");
api.MapCarter();



app.Run();
