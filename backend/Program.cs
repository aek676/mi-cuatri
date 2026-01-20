using backend.Repositories;
using backend.Services;
using DotNetEnv;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

var connetionString = Environment.GetEnvironmentVariable("ConnectionStrings__MongoDB");
if (string.IsNullOrEmpty(connetionString))
{
    throw new Exception("Connection string not found. Ensure the .env file is correctly configured and placed in the root directory.");
}

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IBlackboardService, BlackboardService>();
builder.Services.AddScoped<backend.Data.MongoDbContext>();
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