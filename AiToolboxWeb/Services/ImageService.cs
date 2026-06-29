using Azure.Core;
using Azure.Identity;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiToolboxWeb.Services
{
    public interface IGenerator
    {
        Task<IEnumerable<byte>> GenerateImageAsync(
            string prompt,
            string modelName);
    }

    public class ImageService : IGenerator
    {
        private const string OpenAiImageEndpoint =
            "https://training-east-resource.services.ai.azure.com/openai/v1/images/generations";

        private const string FluxImageEndpoint =
            "https://training-east-resource.services.ai.azure.com/providers/blackforestlabs/v1/flux-2-pro?api-version=preview";

        public async Task<IEnumerable<byte>> GenerateImageAsync(
            string prompt,
            string modelName)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException(
                    $"{nameof(prompt)} cannot be null or empty.");
            }

            if (string.IsNullOrWhiteSpace(modelName))
            {
                throw new ArgumentException(
                    $"{nameof(modelName)} cannot be null or empty.");
            }

            string cleanedModelName =
                modelName.Trim();

            bool isFlux =
                string.Equals(
                    cleanedModelName,
                    "FLUX.2-pro",
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    cleanedModelName,
                    "flux-2-pro",
                    StringComparison.OrdinalIgnoreCase);

            string endpoint =
                isFlux
                    ? FluxImageEndpoint
                    : OpenAiImageEndpoint;

            var credential =
                new DefaultAzureCredential();

            AccessToken token =
                credential.GetToken(
                    new TokenRequestContext(
                        new[]
                        {
                            "https://ai.azure.com/.default"
                        }));

            using HttpClient httpClient =
                new();

            httpClient
                .DefaultRequestHeaders
                .Authorization =
                    new AuthenticationHeaderValue(
                        "Bearer",
                        token.Token);

            object payload;

            if (isFlux)
            {
                payload = new
                {
                    prompt = prompt,
                    model = "FLUX.2-pro",
                    width = 1024,
                    height = 1024,
                    n = 1
                };
            }
            else
            {
                payload = new
                {
                    prompt = prompt,
                    model = cleanedModelName,
                    size = "1024x1024",
                    n = 1
                };
            }

            string json =
                JsonSerializer.Serialize(
                    payload);

            using StringContent content =
                new(
                    json,
                    Encoding.UTF8,
                    "application/json");

            HttpResponseMessage response =
                await httpClient.PostAsync(
                    endpoint,
                    content);

            string responseText =
                await response
                    .Content
                    .ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    responseText);
            }

            using JsonDocument doc =
                JsonDocument.Parse(
                    responseText);

            string base64Image =
                doc.RootElement
                    .GetProperty("data")[0]
                    .GetProperty("b64_json")
                    .GetString()!;

            byte[] imageBytes =
                Convert.FromBase64String(
                    base64Image);

            if (imageBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "No image bytes were returned.");
            }

            return imageBytes;
        }
    }
}