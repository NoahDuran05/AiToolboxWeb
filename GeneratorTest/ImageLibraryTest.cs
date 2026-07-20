using Aloha.Generator;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aloha.Generator.Test.Services;

[TestFixture]
public class ImageLibraryTests
{
    private ImageServiceLibrary _imageService = null!;

    [SetUp]
    public void SetUp()
    {
        _imageService = new ImageServiceLibrary();
    }

    [Test]
    public void EmptyPrompt_ShouldThrow()
    {
        Assert.ThrowsAsync<System.ArgumentException>(async () =>
            await _imageService.GenerateImageAsync(
                "",
                "FLUX.2-pro"));
    }

    [Test]
    public void EmptyModelName_ShouldThrow()
    {
        Assert.ThrowsAsync<System.ArgumentException>(async () =>
            await _imageService.GenerateImageAsync(
                "A test image of a robot",
                ""));
    }

    [TestCase(
        "A simple red apple on a white table",
        "FLUX.2-pro")]
    [TestCase(
        "A simple blue cube on a white table",
        "flux-2-pro")]
    [TestCase(
        "A simple green tree on a white background",
        "gpt-image-2")]
    [Category("Integration")]
    [Ignore("Requires Azure credentials; skipped in GitHub Actions.")]
    public async Task GenerateImageAsync_ReturnsImageBytes(
        string prompt,
        string modelName)
    {
        IEnumerable<byte> result =
            await _imageService.GenerateImageAsync(
                prompt,
                modelName);

        Assert.That(result, Is.Not.Null);

        byte[] bytes = result.ToArray();

        Assert.That(bytes.Length, Is.GreaterThan(0));
    }
}