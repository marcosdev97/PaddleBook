using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PaddleBook.Api.Contracts;
using PaddleBook.Api.Messaging;
using PaddleBook.Api.Messaging.Events;
using PaddleBook.Domain.Entities;
using PaddleBook.Infrastructure.Identity;
using PaddleBook.Infrastructure.Persistence;
using Serilog;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
var isTesting = builder.Environment.EnvironmentName == "Testing";
var connStr = builder.Configuration.GetConnectionString("Default");

if (isTesting)
{
    // Usar InMemory SOLO en tests
    var dbName = builder.Configuration["TestDbName"] ?? "PaddleBook_TestDB";
    builder.Services.AddDbContext<PaddleDbContext>(opt =>
        opt.UseInMemoryDatabase(dbName));
}
else
{
    // Producción/Desarrollo normal: PostgreSQL
    builder.Services.AddDbContext<PaddleDbContext>(opt =>
        opt.UseNpgsql(connStr));
}

// FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Identity Core (sin UI, para API)
builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<PaddleDbContext>()
    .AddSignInManager(); // para comprobar contraseñas en login

// JWT 
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSection["Key"]!;
var jwtIssuer = jwtSection["Issuer"];
var jwtAudience = jwtSection["Audience"];

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            RoleClaimType = ClaimTypes.Role,  // 👈 importantísimo
            NameClaimType = ClaimTypes.NameIdentifier
        };
    });



builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var jwtScheme = new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduce: Bearer {tu_token_jwt}"
    };

    c.AddSecurityDefinition("Bearer", jwtScheme);

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

/////////////////////////////RabbitMQ Publisher/////////////////////////////
builder.Services.Configure<RabbitOptions>(builder.Configuration.GetSection("Rabbit"));
builder.Services.AddSingleton<IEventPublisher, RabbitMqPublisher>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

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
.RequireAuthorization("AdminOnly")
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
.RequireAuthorization("AdminOnly")
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
.RequireAuthorization("AdminOnly")
.Produces(StatusCodes.Status204NoContent)
.Produces(StatusCodes.Status404NotFound);

// ------------------ BOOKINGS ------------------

// CREATE
app.MapPost("/bookings", async (PaddleDbContext db, IValidator<CreateBookingDto> validator, CreateBookingDto dto, IEventPublisher publisher) =>
{
    var validation = await validator.ValidateAsync(dto);
    if (!validation.IsValid)
        return Results.ValidationProblem(validation.ToDictionary());

    // Comprobamos si la pista existe
    var courtExists = await db.Courts.AnyAsync(c => c.Id == dto.CourtId);
    if (!courtExists)
        return Results.BadRequest(new { error = "Court not found" });

    var booking = new Booking(Guid.NewGuid(), dto.CourtId, dto.StartTime, dto.EndTime, dto.CustomerName);
    db.Bookings.Add(booking);
    await db.SaveChangesAsync();

    publisher.Publish("booking.created", new
    {
        booking.Id,
        booking.CourtId,
        booking.StartTime,
        booking.EndTime,
        booking.CustomerName
    });

    return Results.Created($"/bookings/{booking.Id}",
        new BookingResponse(booking.Id, booking.CourtId, booking.StartTime, booking.EndTime, booking.CustomerName));
})
  .RequireAuthorization();

// GET ALL
app.MapGet("/bookings", async (PaddleDbContext db) =>
{
    var list = await db.Bookings.AsNoTracking()
        .Select(b => new BookingResponse(b.Id, b.CourtId, b.StartTime, b.EndTime, b.CustomerName))
        .ToListAsync();

    return Results.Ok(list);
});

// GET BY ID
app.MapGet("/bookings/{id:guid}", async (PaddleDbContext db, Guid id) =>
{
    var b = await db.Bookings.AsNoTracking()
        .Where(x => x.Id == id)
        .Select(x => new BookingResponse(x.Id, x.CourtId, x.StartTime, x.EndTime, x.CustomerName))
        .FirstOrDefaultAsync();

    return b is null ? Results.NotFound() : Results.Ok(b);
});

// DELETE
app.MapDelete("/bookings/{id:guid}", async (PaddleDbContext db, Guid id) =>
{
    var entity = await db.Bookings.FindAsync(id);
    if (entity is null) return Results.NotFound();

    db.Bookings.Remove(entity);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
  .RequireAuthorization();

// POST /auth/register
app.MapPost("/auth/register", async (UserManager<AppUser> users, RegisterDto dto) =>
{
    var user = new AppUser { UserName = dto.Email, Email = dto.Email };
    var result = await users.CreateAsync(user, dto.Password);
    return result.Succeeded
        ? Results.Ok(new AuthResult("User created"))
        : Results.BadRequest(result.Errors.Select(e => e.Description));
});

// POST /auth/login
app.MapPost("/auth/login", async (
    SignInManager<AppUser> signIn,
    UserManager<AppUser> users,
    IConfiguration config,
    LoginDto dto) =>
{
    var jwtSection = config.GetSection("Jwt");
    var jwtKey = jwtSection["Key"]!;
    var jwtIssuer = jwtSection["Issuer"];
    var jwtAudience = jwtSection["Audience"];

    var user = await users.FindByEmailAsync(dto.Email);
    if (user is null) return Results.BadRequest("Invalid credentials");

    var check = await signIn.CheckPasswordSignInAsync(user, dto.Password, false);
    if (!check.Succeeded) return Results.BadRequest("Invalid credentials");
    
    var roles = await users.GetRolesAsync(user);

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
        new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
        new(ClaimTypes.NameIdentifier, user.Id.ToString())
    };

    foreach (var role in roles)
        claims.Add(new Claim(ClaimTypes.Role, role));

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var expires = DateTime.UtcNow.AddMinutes(int.Parse(jwtSection["ExpiresMinutes"] ?? "60"));

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: expires,
        signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);

    return Results.Ok(new { accessToken = jwt, expiresAt = expires });
});

app.MapPost("/dev/publish-booking", (IEventPublisher publisher) =>
{
    var evt = new BookingCreatedEvent()
    {
        BookingId = Guid.NewGuid(),
        CourtId = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        StartUtc = DateTime.UtcNow.AddHours(24),
        EndUtc = DateTime.UtcNow.AddHours(25),
        Price = 15.0m
    };

    publisher.Publish("booking.created", evt);
    return Results.Ok(evt);
});



if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Testing")
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;

    var userManager = sp.GetRequiredService<UserManager<AppUser>>();
    var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

    await DataSeeder.SeedAsync(userManager, roleManager);
}


app.Run();
public partial class Program { }