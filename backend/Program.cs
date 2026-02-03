using System.Reflection;
using backend.Repositories;
using backend.Services;
using DotNetEnv;
using System.Text.Json.Serialization;

Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connetionString = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDB");
if (string.IsNullOrEmpty(connetionString))
{
    throw new Exception("Connection string not found. Ensure the .env file is correctly configured and placed in the root directory.");
}

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBlackboardService, BlackboardService>();
builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
builder.Services.AddScoped<backend.Data.MongoDbContext>();

builder.Services.AddDataProtection();
builder.Services.AddSingleton<ITokenProtector, TokenProtector>();

builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors(x => x.AllowAnyHeader().AllowAnyMethod().WithOrigins("*"));
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();