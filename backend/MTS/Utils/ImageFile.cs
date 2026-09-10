using MTS.Exceptions;
using MTS.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SharpImage = SixLabors.ImageSharp.Image;

namespace MTS.Utils;

public sealed class ImageFile
{
    private byte[] data;
    private IImageFormat format;

    public string FormatName => format.Name;
    public string MimeType => format.DefaultMimeType;
    public string FileExtension => format.FileExtensions.First();
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ReadOnlyMemory<byte> Data => data;

    public ImageFile(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new InvalidException("Dữ liệu ảnh không hợp lệ.");

        this.data = data.ToArray();
        format = SharpImage.DetectFormat(this.data);
        using var image = SharpImage.Load(this.data);
        Width = image.Width;
        Height = image.Height;
    }

    public ImageFile Resize(
        int width,
        int height,
        ResizeMode mode = ResizeMode.Crop,
        AnchorPositionMode position = AnchorPositionMode.Center)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        using var image = SharpImage.Load(data);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(width, height),
            Mode = mode,
            Position = position,
        }));

        var encoder = image.Configuration.ImageFormatsManager.GetEncoder(format)
                      ?? throw new InvalidException(
                          $"Không hỗ trợ encode định dạng {format.Name}.");
        using var stream = new MemoryStream();
        image.Save(stream, encoder);
        data = stream.ToArray();
        Width = image.Width;
        Height = image.Height;
        return this;
    }

    public ImageFile ResizeTo256()
    {
        return Resize(256, 256);
    }

    public byte[] ToArray()
    {
        return data.ToArray();
    }

    public Task<R2UploadResult> UploadAsync(
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        return UploadAsync(Program.R2, objectKey, cancellationToken);
    }

    public Task<R2UploadResult> UploadAsync(
        CloudflareR2Service r2,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(r2);
        return r2.UploadAsync(data, objectKey, MimeType, cancellationToken);
    }
}
