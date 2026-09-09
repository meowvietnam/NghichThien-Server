using MTS.Services.Clans;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MTS.Tests.Clans;

public class ClanImageTests
{
    [Theory]
    [InlineData(256, 128)]
    [InlineData(64, 96)]
    public void Normalization_outputs_128_square_png(int width, int height)
    {
        using var input = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        input.SaveAsJpeg(stream);
        var bytes = ClanImageService.Normalize(stream.ToArray());
        Assert.Equal("PNG", Image.DetectFormat(bytes).Name);
        using var result = Image.Load(bytes);
        Assert.Equal(128, result.Width); Assert.Equal(128, result.Height);
        Assert.Single(result.Frames); Assert.Null(result.Metadata.ExifProfile);
    }

    [Fact]
    public void Oversized_dimensions_and_empty_files_are_rejected()
    {
        Assert.Equal("InvalidImageSize", Assert.Throws<ClanRuleException>(() => ClanImageService.Normalize([])).Code);
        using var input = new Image<Rgba32>(4097, 1);
        using var stream = new MemoryStream(); input.SaveAsPng(stream);
        Assert.Equal("ImageDimensionsTooLarge", Assert.Throws<ClanRuleException>(() => ClanImageService.Normalize(stream.ToArray())).Code);
    }

    [Fact]
    public void Non_image_data_is_rejected()
    {
        Assert.Throws<UnknownImageFormatException>(() => ClanImageService.Normalize("not an image"u8.ToArray()));
    }
}
