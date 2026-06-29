using Azure.Identity;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel.Primitives;

#pragma warning disable OPENAI001

namespace AiToolboxWeb.Services
{
    public class TextService
    {
        private const string endpoint =
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
                    $"{nameof(prompt)} cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                throw new ArgumentException(
                    $"{nameof(systemPrompt)} cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException(
                    $"{nameof(modelName)} cannot be null or empty.");
            }

            temperature =
                Math.Clamp(
                    temperature,
                    0f,
                    1f);

            BearerTokenPolicy tokenPolicy = new(
                new DefaultAzureCredential(),
                "https://ai.azure.com/.default");

            ChatClient client = new(
                authenticationPolicy: tokenPolicy,
                model: modelName,
                options: new OpenAIClientOptions()
                {
                    Endpoint = new Uri(endpoint),
                });

            conversationHistory.Add(
                new UserChatMessage(prompt));

            while (conversationHistory.Count > MaxHistoryMessages)
            {
                conversationHistory.RemoveAt(0);
            }

            DateTime now = DateTime.Now;

            TimeZoneInfo localTimeZone =
                TimeZoneInfo.Local;

            string fullSystemPrompt =
                $"""
                {systemPrompt}

                Current local date and time:
                {now:dddd, MMMM d, yyyy h:mm:ss tt}

                Current time zone:
                {localTimeZone.DisplayName}

                Time zone ID:
                {localTimeZone.Id}
                """;

            List<ChatMessage> messages =
            [
                new SystemChatMessage(fullSystemPrompt)
            ];

            messages.AddRange(conversationHistory);

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
                string.Empty;

            foreach (ChatMessageContentPart part in completion.Content)
            {
                if (!string.IsNullOrWhiteSpace(part.Text))
                {
                    assistantResponse += part.Text;
                }
            }

            if (string.IsNullOrWhiteSpace(assistantResponse))
            {
                assistantResponse = "No response.";
            }

            conversationHistory.Add(
                new AssistantChatMessage(
                    assistantResponse));

            while (conversationHistory.Count > MaxHistoryMessages)
            {
                conversationHistory.RemoveAt(0);
            }

            return assistantResponse;
        }
    }
}