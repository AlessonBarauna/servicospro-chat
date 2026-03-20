using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com");
    client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/chat", async (ChatRequest request, IHttpClientFactory factory, IConfiguration config) =>
{
    var apiKey = config["Anthropic:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.Problem(
            detail: "A variável ANTHROPIC__APIKEY não está configurada no servidor.",
            statusCode: 500);

    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "messages é obrigatório." });

    var client = factory.CreateClient("anthropic");
    client.DefaultRequestHeaders.Remove("x-api-key");
    client.DefaultRequestHeaders.Add("x-api-key", apiKey);

    var body = new
    {
        model      = "claude-3-5-sonnet-20241022",
        max_tokens = 1024,
        system     = request.System ?? string.Empty,
        messages   = request.Messages
    };

    var json    = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        var response     = await client.PostAsync("/v1/messages", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? Results.Content(responseBody, "application/json")
            : Results.Content(responseBody, "application/json", statusCode: (int)response.StatusCode);
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("Timeout ao aguardar resposta da Anthropic.", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Erro de conexão com a Anthropic: {ex.Message}", statusCode: 502);
    }
});

app.Run();

record Message(string Role, string Content);
record ChatRequest(string? System, List<Message> Messages);
