using TicTacToe.Api.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Game state and scoreboard are held in memory for the lifetime of the process.
builder.Services.AddSingleton<GameStore>();

// The React dev server runs on a different origin, so the browser needs
// explicit permission to call this API.
const string CorsPolicy = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors(CorsPolicy);
app.MapControllers();

app.Run();
