using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("gemini", client =>
{
    client.BaseAddress = new Uri("https://generativelanguage.googleapis.com");
    client.Timeout = TimeSpan.FromSeconds(60);
});

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/chat", async (ChatRequest request, IHttpClientFactory factory, IConfiguration config) =>
{
    var apiKey = config["Gemini:ApiKey"];

    if (string.IsNullOrWhiteSpace(apiKey))
        return Results.Problem(
            detail: "A variável Gemini__ApiKey não está configurada no servidor.",
            statusCode: 500);

    if (request.Messages is null || request.Messages.Count == 0)
        return Results.BadRequest(new { error = "messages é obrigatório." });

    var client = factory.CreateClient("gemini");

    // Gemini usa "user" e "model" (não "assistant")
    var contents = request.Messages.Select(m => new
    {
        role  = m.Role == "assistant" ? "model" : "user",
        parts = new[] { new { text = m.Content } }
    });

    var body = new
    {
        system_instruction = new
        {
            parts = new[] { new { text = request.System ?? string.Empty } }
        },
        contents,
        generationConfig = new { maxOutputTokens = 1024, temperature = 0.7 }
    };

    var json    = JsonSerializer.Serialize(body, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        var response     = await client.PostAsync($"/v1beta/models/gemini-1.5-flash:generateContent?key={apiKey}", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return Results.Content(responseBody, "application/json", statusCode: (int)response.StatusCode);

        // Normaliza resposta para o mesmo formato que o frontend espera
        var gemini = JsonNode.Parse(responseBody);
        var text   = gemini?["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.GetValue<string>() ?? "...";

        return Results.Ok(new { content = new[] { new { text } } });
    }
    catch (TaskCanceledException)
    {
        return Results.Problem("Timeout ao aguardar resposta do Gemini.", statusCode: 504);
    }
    catch (HttpRequestException ex)
    {
        return Results.Problem($"Erro de conexão com o Gemini: {ex.Message}", statusCode: 502);
    }
});

app.Run();

record Message(string Role, string Content);
record ChatRequest(string? System, List<Message> Messages);
