using ToxicWasteOfTime.Services;
using ToxicWasteOfTime.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register the Xbox controller service as a singleton
builder.Services.AddSingleton<XboxControllerService>();

// Register database context
builder.Services.AddDbContext<RecordingDbContext>(options =>
{
    var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recordings.db");
    options.UseSqlite($"Data Source={dbPath}");
});

// Register recording service as a singleton
builder.Services.AddSingleton<ControllerRecordingService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Configure the server to run on port 5000
app.Urls.Add("http://localhost:5000");

// Run the web application
app.Run();
