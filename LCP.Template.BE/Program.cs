using LCP.Template.BE.Extensions;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddApplicationServices();

// Entra ID auth is only enabled once an "AzureAd" section is configured.
if (builder.Configuration.GetSection("AzureAd").Exists())
{
    builder.Services.AddAuthentication("Bearer")
        .AddMicrosoftIdentityWebApi(builder.Configuration);
}
else
{
    builder.Services.AddAuthorization();
}

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
