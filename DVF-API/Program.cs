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
builder.Services.AddScoped<ICrudDatabaseRepository, CrudDatabaseRepository>();
builder.Services.AddScoped<ICrudFileRepository, CrudFileRepository>();
builder.Services.AddScoped<IHistoricWeatherDataRepository, HistoricWeatherDataRepository>();
builder.Services.AddScoped<ILocationRepository, CrudDatabaseRepository>();

builder.Services.AddScoped<IDataService, DataService>();
builder.Services.AddScoped<IDeveloperService, DeveloperService>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
                 
builder.Services.AddScoped<ISolarPositionManager, SolarPositionManager>();
builder.Services.AddScoped<IUtilityManager, UtilityManager>();

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
