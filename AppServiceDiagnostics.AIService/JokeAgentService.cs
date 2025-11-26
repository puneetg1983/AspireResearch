using Azure.AI.Inference;
using System.ClientModel;

namespace AppServiceDiagnostics.AIService;

public class JokeAgentService
{
    private readonly ILogger<JokeAgentService> _logger;
    private readonly ChatCompletionsClient _chatClient;
    private readonly Random _random = new();
    private readonly List<string> _recentJokes = new();
    private readonly string[] _jokeTypes = 
    {
        "pun", "dad joke", "one-liner", "knock-knock joke", "wordplay joke",
        "tech joke", "animal joke", "food joke", "workplace joke", "travel joke"
    };

    public JokeAgentService(ChatCompletionsClient chatClient, ILogger<JokeAgentService> logger)
    {
        _logger = logger;
        _chatClient = chatClient;
    }

    public async Task<string> GetJokeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating joke using Azure AI Inference service");
        
        // Select a random joke type for variety
        var jokeType = _jokeTypes[_random.Next(_jokeTypes.Length)];
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        
        // Build system message with context to avoid repetition
        var systemMessage = "You are a professional comedian AI. Generate clean, family-friendly jokes that are clever and funny. " +
                           "Keep each joke short and punchy. Only return the joke text, nothing else. " +
                           "Make sure each joke is unique and different from previous ones.";
        
        // Add recent jokes context if we have any
        if (_recentJokes.Any())
        {
            systemMessage += $" Avoid repeating these recent jokes: {string.Join("; ", _recentJokes)}";
        }
        
        var userMessage = $"Tell me a funny, clean {jokeType}! (Request ID: {uniqueId})";
        
        var options = new ChatCompletionsOptions
        {
            Messages =
            {
                new ChatRequestSystemMessage(systemMessage),
                new ChatRequestUserMessage(userMessage)
            },
            MaxTokens = 150,
            Temperature = 0.9f,  // Increased for more variety
            Model = "chat"  // Specify the deployment name here
        };

        var response = await _chatClient.CompleteAsync(options, cancellationToken);
        
        var joke = response.Value.Content?.Trim();
        
        if (!string.IsNullOrEmpty(joke))
        {
            _logger.LogInformation("Successfully generated {JokeType} joke using Azure AI Inference", jokeType);
            
            // Store joke to avoid repetition (keep last 10)
            _recentJokes.Add(joke);
            if (_recentJokes.Count > 10)
            {
                _recentJokes.RemoveAt(0);
            }
            
            return joke;
        }
        
        throw new InvalidOperationException("Received empty response from Azure AI Inference service");
    }
}