using System.Threading.RateLimiting;
using Commerce.Api.Data;
using Commerce.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddScoped<CommerceRepository>();
builder.Services.AddSingleton<CsvImportService>();
builder.Services.AddSingleton<TokenService>();
var signingKey = TokenService.GetKey(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(signingKey);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Authentication:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Authentication:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});
builder.Services.AddAuthorization();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions
        { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    options.OnRejected = async (context, ct) => await context.HttpContext.Response.WriteAsJsonAsync(
        new ProblemDetails { Status = 429, Title = "Demasiados intentos", Detail = "Espera un minuto antes de volver a iniciar sesión." }, ct);
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "iRoute Commerce API",
        Version = "v1",
        Description = "Importación CSV, procesamiento por fecha y cuarentena. Inicia sesión y utiliza el token en Authorize."
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pega solo el token devuelto por POST /api/auth/login."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();
if (args.Contains("--initialize-db"))
{
    using var scope = app.Services.CreateScope();
    await DatabaseInitializer.InitializeAsync(scope.ServiceProvider.GetRequiredService<CommerceRepository>(), app.Configuration, app.Environment);
    return;
}
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    var databaseError = exception is SqlException;
    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("UnhandledException");
    logger.LogError(exception, "Error en {Path}. TraceId: {TraceId}", context.Request.Path, context.TraceIdentifier);
    context.Response.StatusCode = databaseError ? 503 : 500;
    await Results.Problem(statusCode: context.Response.StatusCode,
        title: databaseError ? "Base de datos no disponible" : "Ocurrió un error inesperado",
        detail: databaseError ? "Verifica la conexión a SQL Server y ejecuta la inicialización de la base de datos." : "Intenta nuevamente.",
        extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
}));
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
else app.UseHttpsRedirection();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/api/health", async (CommerceRepository repository, CancellationToken ct) =>
{
    await using var connection = await repository.OpenAsync(ct);
    await using var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID('dbo.commerce','U') IS NOT NULL AND OBJECT_ID('dbo.sp_process_commerce','P') IS NOT NULL THEN 1 ELSE 0 END", connection);
    var initialized = (int)(await command.ExecuteScalarAsync(ct) ?? 0) == 1;
    return initialized ? Results.Ok(new { status = "healthy", database = "SQL Server" }) : Results.Problem(statusCode: 503, title: "Base sin inicializar");
}).AllowAnonymous();
app.Run();

public partial class Program;
