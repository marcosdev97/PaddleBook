using Microsoft.EntityFrameworkCore;
using PaddleBook.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core + Npgsql apuntando a la connection string "Default"
builder.Services.AddDbContext<PaddleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/ping", () => Results.Ok(new { message = "pong" }));

app.Run();
