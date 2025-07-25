using HNTAS.Core.Api.Configuration;
using HNTAS.Core.Api.Interfaces;
using HNTAS.Core.Api.MappingProfiles;
using HNTAS.Core.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<NotificationSettings>(
    builder.Configuration.GetSection("NotificationSettings"));
// Configure db settings
builder.Services.Configure<AWSDocDbSettings>(
    builder.Configuration.GetSection("AWSDocDbSettings"));

// Register AutoMapper and scan for profiles
builder.Services.AddAutoMapper(typeof(UserMappingProfile));

builder.Services.AddSingleton<IUserService, UserService>();
builder.Services.AddSingleton<ICounterService, CounterService>();
builder.Services.AddSingleton<IGovUkNotifyService, GovUkNotifyService>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi("HNTAS.Core.Api");

builder.Logging.AddAWSProvider(builder.Configuration.GetAWSLoggingConfigSection());
builder.Logging.SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/HNTAS.Core.Api.json", "HNTAS Core API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
