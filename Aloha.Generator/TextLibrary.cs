using Azure.Identity;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel.Primitives;

#pragma warning disable OPENAI001

namespace Aloha.Generator;

public interface ITextGenerator
{
    Task<string> GenerateTextAsync(
        string prompt,
        string systemPrompt,
        string modelName,
        float temperature);
}

public class TextServiceLibrary : ITextGenerator
{
    private const string Endpoint =
        "https://training-east-resource.services.ai.azure.com/openai/v1";

    private const int MaxHistoryMessages = 100;

    private readonly List<ChatMessage> conversationHistory = [];

    public async Task<string> GenerateTextAsync(
        string prompt,
        string systemPrompt,
        string modelName,
        float temperature)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException(
                $"{nameof(prompt)} cannot be null or empty.",
                nameof(prompt));
        }

        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            throw new ArgumentException(
                $"{nameof(systemPrompt)} cannot be null or empty.",
                nameof(systemPrompt));
        }

        if (string.IsNullOrWhiteSpace(modelName))
        {
            throw new ArgumentException(
                $"{nameof(modelName)} cannot be null or empty.",
                nameof(modelName));
        }

        temperature = Math.Clamp(
            temperature,
            0f,
            1f);

        BearerTokenPolicy tokenPolicy =
            new(
                new DefaultAzureCredential(),
                "https://ai.azure.com/.default");

        ChatClient client =
            new(
                authenticationPolicy: tokenPolicy,
                model: modelName.Trim(),
                options: new OpenAIClientOptions
                {
                    Endpoint = new Uri(Endpoint)
                });

        conversationHistory.Add(
            new UserChatMessage(prompt));

        TrimConversationHistory();

        DateTime now = DateTime.Now;
        TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

        string fullSystemPrompt =
            systemPrompt
            + Environment.NewLine
            + Environment.NewLine
            + "Current local date and time:"
            + Environment.NewLine
            + now.ToString("dddd, MMMM d, yyyy h:mm:ss tt")
            + Environment.NewLine
            + Environment.NewLine
            + "Current time zone:"
            + Environment.NewLine
            + localTimeZone.DisplayName
            + Environment.NewLine
            + Environment.NewLine
            + "Time zone ID:"
            + Environment.NewLine
            + localTimeZone.Id;

        List<ChatMessage> messages =
        [
            new SystemChatMessage(fullSystemPrompt),
            .. conversationHistory
        ];

        ChatCompletionOptions chatOptions =
            new()
            {
                Temperature = temperature
            };

        ChatCompletion completion =
            await client.CompleteChatAsync(
                messages,
                chatOptions);

        string assistantResponse =
            string.Concat(
                completion.Content
                    .Where(part =>
                        !string.IsNullOrWhiteSpace(part.Text))
                    .Select(part => part.Text));

        if (string.IsNullOrWhiteSpace(assistantResponse))
        {
            assistantResponse = "No response.";
        }

        conversationHistory.Add(
            new AssistantChatMessage(
                assistantResponse));

        TrimConversationHistory();

        return assistantResponse;
    }

    private void TrimConversationHistory()
    {
        while (conversationHistory.Count > MaxHistoryMessages)
        {
            conversationHistory.RemoveAt(0);
        }
    }
}