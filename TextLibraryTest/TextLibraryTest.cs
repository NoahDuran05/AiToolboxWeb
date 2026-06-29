using AiToolboxWeb.Services;
using NUnit.Framework;
using System.Threading.Tasks;

namespace TextLibraryTest;

[TestFixture]
public class TextLibraryTests
{
    private TextServiceLibrary _textService = null!;

    [SetUp]
    public void SetUp()
    {
        _textService = new TextServiceLibrary();
    }

    [Test]
    [Category("Integration")]
    public void EmptyUserPrompt_ShouldThrow()
    {
        Assert.ThrowsAsync<System.Exception>(async () =>
            await _textService.GenerateTextAsync(
                "",
                "You are a helpful AI assistant.",
                "DeepSeek-V4-Pro",
                0.7f));
    }

    [Test]
    [Category("Integration")]
    public void EmptySystemPrompt_ShouldThrow()
    {
        Assert.ThrowsAsync<System.Exception>(async () =>
            await _textService.GenerateTextAsync(
                "Say hello.",
                "",
                "DeepSeek-V4-Pro",
                0.7f));
    }

    [Test]
    [Category("Integration")]
    public void EmptyModelName_ShouldThrow()
    {
        Assert.ThrowsAsync<System.Exception>(async () =>
            await _textService.GenerateTextAsync(
                "Say hello.",
                "You are a helpful AI assistant.",
                "",
                0.7f));
    }

    [Category("Integration")]
    [TestCase(
        "Reply with exactly this word: pineapple",
        "You are a helpful AI assistant.",
        "DeepSeek-V4-Pro",
        0.7f)]
    [TestCase(
        "What color is the sky?",
        "Reply with exactly one word.",
        "DeepSeek-V4-Pro",
        0.7f)]
    [TestCase(
        "Write one short sentence about robots.",
        "You are a helpful AI assistant.",
        "DeepSeek-V4-Pro",
        0.0f)]
    [TestCase(
        "Write one short sentence about robots.",
        "You are a helpful AI assistant.",
        "DeepSeek-V4-Pro",
        0.7f)]
    [TestCase(
        "Write one short sentence about robots.",
        "You are a helpful AI assistant.",
        "DeepSeek-V4-Pro",
        1.0f)]
    [TestCase(
        "Reply with exactly this word: low",
        "You are a helpful AI assistant.",
        "DeepSeek-V4-Pro",
        -5f)]
    [TestCase(
        "Reply with exactly this word: high",
        "You are a helpful AI assistant.",
        "DeepSeek-V4-Pro",
        99f)]
    public async Task GenerateTextAsync_WithValidInput_ReturnsText(
        string prompt,
        string systemPrompt,
        string modelName,
        float temperature)
    {
        string result =
            await _textService.GenerateTextAsync(
                prompt,
                systemPrompt,
                modelName,
                temperature);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Not.Empty);
        Assert.That(result.Trim(), Is.Not.Empty);
    }

    [Category("Integration")]
    [TestCase("grok-4-20-reasoning")]
    [TestCase("DeepSeek-V4-Pro")]
    [TestCase("gpt-5.4")]
    public async Task GenerateTextAsync_WithSupportedModel_ReturnsText(
        string modelName)
    {
        string result =
            await _textService.GenerateTextAsync(
                "Reply with exactly this word: hello",
                "You are a helpful AI assistant.",
                modelName,
                0.7f);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Trim(), Is.Not.Empty);
    }
}