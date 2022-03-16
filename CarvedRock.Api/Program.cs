using CarvedRock.Api;
using CarvedRock.Data;
using CarvedRock.Domain;
using Hellang.Middleware.ProblemDetails;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails(o =>
{
    o.IncludeExceptionDetails = (ctx, ex) => false;
    o.OnBeforeWriteDetails = (ctx, dtls) =>
    {
        if (dtls.Status == 500)
        {
            dtls.Detail = "An error occurred in our API. Use the trace id when contacting us.";
        }
    };
    o.Rethrow<SqliteException>();
    o.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});

// Example of adding log filter by code
builder.Logging.AddFilter("CarvedRock", LogLevel.Debug);

// Create log file:
//var path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//var tracePath = Path.Join(path, $"Log_CarvedRock_{DateTime.Now.ToString("yyyyMMdd-HHmm")}.txt");

//Trace.Listeners.Add(new TextWriterTraceListener(System.IO.File.CreateText(tracePath)));
//Trace.AutoFlush = true;

// Services
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IProductLogic, ProductLogic>();

builder.Services.AddDbContext<LocalContext>();
builder.Services.AddScoped<ICarvedRockRepository, CarvedRockRepository>();

var app = builder.Build();
app.UseMiddleware<CriticalExceptionMiddleware>();
app.UseProblemDetails();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<LocalContext>();
    context.MigrateAndCreateData();
}

// HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapFallback(() => Results.Redirect("/swagger"));
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
