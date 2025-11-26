using Azure.AI.Inference;
using System.ClientModel;

namespace AppServiceDiagnostics.AIService;

public class JokeAgentService
{
    private readonly ILogger<JokeAgentService> _logger;
    private readonly ChatCompletionsClient _chatClient;

    public JokeAgentService(ChatCompletionsClient chatClient, ILogger<JokeAgentService> logger)
    {
        _logger = logger;
        _chatClient = chatClient;
    }

    public async Task<string> GetJokeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating joke using Azure AI Inference service");
        
        var options = new ChatCompletionsOptions
        {
            Messages =
            {
                new ChatRequestSystemMessage(
                    "You are a professional comedian AI. Generate clean, family-friendly jokes that are clever and funny. " +
                    "Keep each joke short and punchy. Only return the joke text, nothing else."),
                new ChatRequestUserMessage("Tell me a funny, clean joke!")
            },
            MaxTokens = 150,
            Temperature = 0.8f,
            Model = "chat"  // Specify the deployment name here
        };

        var response = await _chatClient.CompleteAsync(options, cancellationToken);
        
        var joke = response.Value.Content?.Trim();
        
        if (!string.IsNullOrEmpty(joke))
        {
            _logger.LogInformation("Successfully generated joke using Azure AI Inference");
            return joke;
        }
        
        throw new InvalidOperationException("Received empty response from Azure AI Inference service");
    }
}