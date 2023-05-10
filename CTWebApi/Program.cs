using CTWebApi.Services;
using Polly;
using Polly.Extensions.Http;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
/*
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    EnvironmentName = Environments.Production
});
*/

var logger = new LoggerConfiguration()
  .ReadFrom.Configuration(builder.Configuration)
  .Enrich.FromLogContext()
  .CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(logger);


builder.Services.AddScoped<IGitHubUserService, GitHubUserService>();

// Add services to the container.
builder.Services.AddHttpClient("default")
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
        .AddPolicyHandler(GetRetryPolicy());


IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

//app.UseHttpLogging();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseExceptionHandler("/error-development");
}
else
{
    app.UseExceptionHandler("/error");
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
