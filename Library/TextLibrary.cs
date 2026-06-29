using Azure.Identity;
using OpenAI;
using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.ClientModel.Primitives;
using System.Threading.Tasks;

#pragma warning disable OPENAI001

namespace AiToolboxWeb.Services
{
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
        private const string endpoint =
            "https://training-east-resource.services.ai.azure.com/openai/v1";

        private const int MaxHistoryMessages = 100;

        private readonly List<ChatMessage> conversationHistory =
            new List<ChatMessage>();

        public async Task<string> GenerateTextAsync(
            string prompt,
            string systemPrompt,
            string modelName,
            float temperature)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException(
                    string.Format("{0} cannot be null or empty.", nameof(prompt)));
            }

            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                throw new ArgumentException(
                    string.Format("{0} cannot be null or empty.", nameof(systemPrompt)));
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException(
                    string.Format("{0} cannot be null or empty.", nameof(modelName)));
            }

            temperature =
                Clamp(
                    temperature,
                    0f,
                    1f);

            BearerTokenPolicy tokenPolicy =
                new BearerTokenPolicy(
                    new DefaultAzureCredential(),
                    "https://ai.azure.com/.default");

            ChatClient client =
                new ChatClient(
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

            DateTime now =
                DateTime.Now;

            TimeZoneInfo localTimeZone =
                TimeZoneInfo.Local;

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
                new List<ChatMessage>();

            messages.Add(
                new SystemChatMessage(
                    fullSystemPrompt));

            messages.AddRange(
                conversationHistory);

            ChatCompletionOptions chatOptions =
                new ChatCompletionOptions()
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
                assistantResponse =
                    "No response.";
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

        private static float Clamp(
            float value,
            float min,
            float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }


}
