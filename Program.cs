using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("groq", client =>
{
    client.BaseAddress = new Uri("https://api.groq.com");
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/chat", async (ChatRequest request, IHttpClientFactory factory, IConfiguration config) =>
{
    var apiKey = config["Groq:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.Problem(
            detail: "A variável Groq__ApiKey não está configurada no servidor.",
            statusCode: 500);

    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "messages é obrigatório." });

    var client = factory.CreateClient("groq");
    client.DefaultRequestHeaders.Remove("Authorization");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

    // Groq usa formato OpenAI: system como primeira mensagem
    var messages = new List<object>
    {
        new { role = "system", content = request.System ?? string.Empty }
    };
    messages.AddRange(request.Messages.Select(m => (object)new { role = m.Role, content = m.Content }));

    var body = new
    {
        model      = "llama-3.3-70b-versatile",
        max_tokens = 1024,
        messages
    };

    var json    = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        var response     = await client.PostAsync("/openai/v1/chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return Results.Content(responseBody, "application/json", statusCode: (int)response.StatusCode);

        // Normaliza para o formato que o frontend espera: { content: [{ text }] }
        var groq = JsonNode.Parse(responseBody);
        var text = groq?["choices"]?[0]?["message"]?["content"]?.GetValue<string>() ?? "...";

        return Results.Ok(new { content = new[] { new { text } } });
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("Timeout ao aguardar resposta do Groq.", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Erro de conexão com o Groq: {ex.Message}", statusCode: 502);
    }
});

app.Run();

record Message(string Role, string Content);
record ChatRequest(string? System, List<Message> Messages);
