using CarvedRock.Api;
using CarvedRock.Data;
using CarvedRock.Domain;
using Hellang.Middleware.ProblemDetails;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);

// remove all providers
builder.Logging.ClearProviders();

// now manually add providers
//builder.Logging.AddJsonConsole();
// builder.Logging.AddDebug(); // this shows the debug entries again

// run seq by:
// docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:80 datalust/seq:latest
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console()
    .Enrich.WithExceptionDetails()
    .Enrich.FromLogContext()
    .Enrich.With<ActivityEnricher>()
    .WriteTo.Seq("http://localhost:5341");
});

builder.Services.AddProblemDetails(opts =>
{
    opts.IncludeExceptionDetails = (ctx, ex) => false;

    opts.OnBeforeWriteDetails = (ctx, dtls) =>
    {
        if (dtls.Status == 500)
        {
            dtls.Detail = "An error occurred in our API. Use the trace id when contacting us.";
        }
    };
    opts.Rethrow<SqliteException>();
    opts.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});

// Example of adding log filter by code
//builder.Logging.AddFilter("CarvedRock", LogLevel.Debug);

// Create log file:
//var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//var tracePath = Path.Join(path, $"Log_CarvedRock_{DateTime.Now.ToString("yyyyMMdd-HHmm")}.txt");

//Trace.Listeners.Add(new TextWriterTraceListener(System.IO.File.CreateText(tracePath)));
//Trace.AutoFlush = true;

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://demo.duendesoftware.com";
        options.Audience = "api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            NameClaimType = "email"
        };
    });

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, SwaggerOptions>();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductLogic, ProductLogic>();
builder.Services.AddDbContext<LocalContext>();
builder.Services.AddScoped<ICarvedRockRepository, CarvedRockRepository>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<LocalContext>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LocalContext>();
    context.MigrateAndCreateData();
}

app.UseMiddleware<CriticalExceptionMiddleware>();
app.UseProblemDetails();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("interactive.public.short");
        options.OAuthAppName("CarvedRock API");
        options.OAuthUsePkce();
    });
}
app.MapFallback(() => Results.Redirect("/swagger"));
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseMiddleware<UserScopeMiddleware>();
app.UseAuthorization();
app.MapControllers().RequireAuthorization();

app.MapHealthChecks("health").AllowAnonymous();

app.Run();
