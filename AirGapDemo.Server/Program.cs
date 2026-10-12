using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(IPAddress.Any, 5000);
});

var app = builder.Build();

app.MapGet("/", () => "JudgeAid Server");

app.MapGet("/api/status", () =>
{
    return Results.Ok(new
    {
        Status = "Connected",
        ServerName = Environment.MachineName,
        ServerTime = DateTime.Now
    });
});

app.MapPost("/api/message", (MessageRequest request) =>
{
    Console.WriteLine($"Received from {request.ClientName}: {request.Message}");

    return Results.Ok(new MessageResponse
    {
        Success = true,
        Message = $"Server received: {request.Message}",
        ServerTime = DateTime.Now
    });
});

app.Run();

public class MessageRequest
{
    public string ClientName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class MessageResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime ServerTime { get; set; }
}
