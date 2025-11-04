using Microsoft.EntityFrameworkCore;
using PaddleBook.Infrastructure.Persistence;
using PaddleBook.Domain.Entities;
using PaddleBook.Api.Contracts;
using FluentValidation;
using FluentValidation.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog: consola
builder.Host.UseSerilog((ctx, cfg) =>
{
    cfg.ReadFrom.Configuration(ctx.Configuration)  // si añades settings en appsettings
       .Enrich.FromLogContext()
       .WriteTo.Console();
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// EF Core
builder.Services.AddDbContext<PaddleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Middleware simple de errores (opcional pero útil)
app.Use(async (ctx, next) =>
{
    try { await next(); }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled error");
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await ctx.Response.WriteAsJsonAsync(new { error = "Unexpected error" });
    }
});

// ------------------ ENDPOINTS COURTS ------------------

// CREATE
app.MapPost("/courts", async (PaddleDbContext db, IValidator<CreateCourtDto> validator, CreateCourtDto dto) =>
{
    var result = await validator.ValidateAsync(dto);
    if (!result.IsValid)
        return Results.ValidationProblem(result.ToDictionary());

    var court = new Court(Guid.NewGuid(), dto.Name, dto.Surface);
    db.Courts.Add(court);
    await db.SaveChangesAsync();

    return Results.Created($"/courts/{court.Id}", new CourtResponse(court.Id, court.Name, court.Surface));
})
.Produces<CourtResponse>(StatusCodes.Status201Created)
.ProducesValidationProblem();

// LIST ALL
app.MapGet("/courts", async (PaddleDbContext db, [AsParameters] CourtQueryParams query) =>
{
    var courts = db.Courts.AsNoTracking().AsQueryable();

    // filtro por superficie (si se indica)
    if (!string.IsNullOrWhiteSpace(query.Surface))
        courts = courts.Where(c => c.Surface.ToLower() == query.Surface.ToLower());

    // búsqueda parcial por nombre
    if (!string.IsNullOrWhiteSpace(query.Search))
        courts = courts.Where(c => c.Name.ToLower().Contains(query.Search.ToLower()));

    // total antes de paginar
    var total = await courts.CountAsync();

    // paginación
    var items = await courts
        .OrderBy(c => c.Name)
        .Skip((query.Page - 1) * query.PageSize)
        .Take(query.PageSize)
        .Select(c => new CourtResponse(c.Id, c.Name, c.Surface))
        .ToListAsync();

    return Results.Ok(new
    {
        total,
        query.Page,
        query.PageSize,
        items
    });
});


// GET BY ID
app.MapGet("/courts/{id:guid}", async (PaddleDbContext db, Guid id) =>
{
    var c = await db.Courts.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new CourtResponse(x.Id, x.Name, x.Surface))
        .FirstOrDefaultAsync();

    return c is null ? Results.NotFound() : Results.Ok(c);
})
.Produces<CourtResponse>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound);

// UPDATE
app.MapPut("/courts/{id:guid}", async (PaddleDbContext db, IValidator<UpdateCourtDto> validator, Guid id, UpdateCourtDto dto) =>
{
    var result = await validator.ValidateAsync(dto);
    if (!result.IsValid)
        return Results.ValidationProblem(result.ToDictionary());

    var entity = await db.Courts.FindAsync(id);
    if (entity is null) return Results.NotFound();

    // actualizar (como las props son private set, recreamos de forma simple)
    entity = new Court(id, dto.Name, dto.Surface);
    db.Entry(entity).State = EntityState.Modified; // o mapea campos a mano si prefieres
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.ProducesValidationProblem()
.Produces(StatusCodes.Status404NotFound);

// DELETE
app.MapDelete("/courts/{id:guid}", async (PaddleDbContext db, Guid id) =>
{
    var entity = await db.Courts.FindAsync(id);
    if (entity is null) return Results.NotFound();

    db.Courts.Remove(entity);
    await db.SaveChangesAsync();
    return Results.NoContent();
})
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// ------------------------------------------------------

app.Run();
