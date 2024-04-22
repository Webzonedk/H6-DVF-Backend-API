using DVF_API.Data.Interfaces;
using DVF_API.Data.Repositories;
using DVF_API.Domain.BusinessLogic;
using DVF_API.Domain.Interfaces;
using DVF_API.Services.Interfaces;
using DVF_API.Services.ServiceImplementation;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



builder.Configuration.GetConnectionString("WeatherDataDb");

//Dependency injections
builder.Services.AddTransient<IDatabaseRepository, CrudDatabaseRepository>();
builder.Services.AddTransient<IFileRepository, CrudFileRepository>();
builder.Services.AddTransient<IHistoricWeatherDataRepository, HistoricWeatherDataRepository>();

builder.Services.AddTransient<IAddWeatherDataService, AddWeatherDataService>();
builder.Services.AddTransient<IDataService, DataService>();
builder.Services.AddTransient<IDeveloperService, DeveloperService>();
builder.Services.AddTransient<IMaintenanceService, MaintenanceService>();

builder.Services.AddTransient<ISolarPositionManager, SolarPositionManager>();
builder.Services.AddTransient<IUtilityManager, UtilityManager>();
builder.Services.AddTransient<IBinaryConversionManager, BinaryConversionManager>();


var _allowAllOriginsForDevelopment = "_allowAllOriginsForDevelopment";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: _allowAllOriginsForDevelopment,
        builder =>
        {
            builder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
        });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(_allowAllOriginsForDevelopment);

app.UseAuthorization();

app.MapControllers();

app.Run();
