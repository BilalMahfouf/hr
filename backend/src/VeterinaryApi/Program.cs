using Carter;
using Microsoft.JSInterop.Infrastructure;
using Shared.CQRS;
using VeterinaryApi.Infrastructure;
using DotNetEnv;
using Scalar.AspNetCore;
using VeterinaryApi.Common.Exceptions;
using FluentValidation;
using VeterinaryApi.Common.Extensions;
using VeterinaryApi.Infrastructure.Notifications;
using Shared;
using VeterinaryApi.Infrastructure.Payments;


Env.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddValidatorsFromAssemblyContaining
    < Identity.Application.Users.ChangeEmail.Validator>(ServiceLifetime.Singleton);

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();



// Scan VeterinaryApi assembly for CQRS handlers
builder.Services.Scan(scan => scan.FromAssembliesOf(typeof(Program))
    .AddClasses(classes => classes
        .AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
    .AsImplementedInterfaces()
        .WithScopedLifetime()

    .AddClasses(classes => classes.
        AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
    .AsImplementedInterfaces()
        .WithScopedLifetime()

    .AddClasses(classes => classes
        .AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
    .AsImplementedInterfaces()
        .WithScopedLifetime());

// Scan Identity assembly for CQRS handlers
var identityAssembly = typeof(Identity.Application.Users.Register).Assembly;
builder.Services.Scan(scan => scan.FromAssemblies(identityAssembly)
    .AddClasses(classes => classes
        .AssignableTo(typeof(IQueryHandler<,>)), publicOnly: false)
    .AsImplementedInterfaces()
        .WithScopedLifetime()

    .AddClasses(classes => classes.
        AssignableTo(typeof(ICommandHandler<>)), publicOnly: false)
    .AsImplementedInterfaces()
        .WithScopedLifetime()

    .AddClasses(classes => classes
        .AssignableTo(typeof(ICommandHandler<,>)), publicOnly: false)
    .AsImplementedInterfaces()
        .WithScopedLifetime());

builder.Services.AddPayments();


builder.Services.AddInfrastructure(builder.Configuration);

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
