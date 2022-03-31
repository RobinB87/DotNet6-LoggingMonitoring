using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Exceptions;
using System.IdentityModel.Tokens.Jwt;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig.WriteTo.Console()
    .Enrich.WithExceptionDetails()
    .WriteTo.Seq("http://localhost:5341");
});

builder.Services.AddOpenTelemetryTracing(b =>
{
    b.SetResourceBuilder(
        ResourceBuilder.CreateDefault().AddService(builder.Environment.ApplicationName))
    .AddAspNetCoreInstrumentation()
    .AddHttpClientInstrumentation()
    .AddOtlpExporter(o => { o.Endpoint = new Uri("http://localhost:4317"); });
});

//// Configure http logging
//builder.Services.AddHttpLogging(logging =>
//{
//    // https://bit.ly/aspnetcore6-httplogging
//    logging.LoggingFields = HttpLoggingFields.All;
//    logging.MediaTypeOptions.AddText("application/javascript");
//    logging.RequestBodyLogLimit = 4096;
//    logging.ResponseBodyLogLimit = 4096;
//});

//// Configure W3C logging
//var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//builder.Services.AddW3CLogging(o =>
//{
//    // https://bit.ly/aspnetcore6-w3clogger
//    o.LoggingFields = W3CLoggingFields.All;
//    o.FileSizeLimit = 5 * 1024 * 1024;
//    o.RetainedFileCountLimit = 2;
//    o.FileName = "CarvedRock-W3C-UI";
//    o.LogDirectory = Path.Combine(path, "logs");
//    o.FlushInterval = TimeSpan.FromSeconds(2);
//});

JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "oidc";
})
.AddCookie("Cookies")
.AddOpenIdConnect("oidc", options =>
{
    options.Authority = "https://demo.duendesoftware.com";
    options.ClientId = "interactive.confidential";
    options.ClientSecret = "secret";
    options.ResponseType = "code";
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("api");
    options.Scope.Add("offline_access");
    options.GetClaimsFromUserInfoEndpoint = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        NameClaimType = "email"
    };
    options.SaveTokens = true;
});
builder.Services.AddHttpContextAccessor();

// Health checks
builder.Services.AddHealthChecks()
    // if there is a problem with the duendeservice, site will be degraded (site is functional, but users not able to login)
    .AddIdentityServer(new Uri("https://demo.duendesoftware.com"), failureStatus: HealthStatus.Degraded);

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler("/Error");
// The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
app.UseHsts();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();

// Map health checks, order is important, give a name / endpoint you want
// => https://localhost:7224/health
app.MapHealthChecks("health").AllowAnonymous();

app.Run();
