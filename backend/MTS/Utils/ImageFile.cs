using MTS.Exceptions;
using MTS.Services;
using SkiaSharp;

namespace MTS.Utils;

public enum ResizeMode
{
    Crop,
    Pad,
    Max,
    Min,
    Stretch,
}

public enum AnchorPositionMode
{
    Center,
    Top,
    Bottom,
    Left,
    Right,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
}

public sealed class ImageFile
{
    private byte[] data;
    private SKEncodedImageFormat format;

    public string FormatName => GetFormatName(format);
    public string MimeType => GetMimeType(format);
    public string FileExtension => GetFileExtension(format);
    public int Width { get; private set; }
    public int Height { get; private set; }
    public ReadOnlyMemory<byte> Data => data;

    public ImageFile(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new InvalidException("Dữ liệu ảnh không hợp lệ.");

        this.data = data.ToArray();
        using var codec = SKCodec.Create(new SKMemoryStream(this.data))
                          ?? throw new InvalidException("Dữ liệu ảnh không hợp lệ.");
        format = codec.EncodedFormat;
        Width = codec.Info.Width;
        Height = codec.Info.Height;
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

        using var source = SKBitmap.Decode(data)
                           ?? throw new InvalidException("Dữ liệu ảnh không hợp lệ.");
        var targetSize = GetTargetSize(source.Width, source.Height, width, height, mode);
        var sourceRect = GetSourceRect(source.Width, source.Height, width, height, mode, position);
        var destinationRect = GetDestinationRect(source.Width, source.Height, width, height, mode);

        using var target = new SKBitmap(targetSize.Width, targetSize.Height, source.ColorType, source.AlphaType);
        using var canvas = new SKCanvas(target);
        canvas.Clear(SKColors.Transparent);
        using var paint = new SKPaint
        {
            IsAntialias = true,
        };
        canvas.DrawBitmap(source, sourceRect, destinationRect, paint);

        using var image = SKImage.FromBitmap(target);
        using var encoded = image.Encode(format, 90)
                            ?? throw new InvalidException(
                                $"Không hỗ trợ encode định dạng {FormatName}.");
        data = encoded.ToArray();
        Width = target.Width;
        Height = target.Height;
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

    private static SKSizeI GetTargetSize(
        int sourceWidth,
        int sourceHeight,
        int width,
        int height,
        ResizeMode mode)
    {
        return mode switch
        {
            ResizeMode.Max => ScaleToFit(sourceWidth, sourceHeight, width, height, Math.Min),
            ResizeMode.Min => ScaleToFit(sourceWidth, sourceHeight, width, height, Math.Max),
            _ => new SKSizeI(width, height),
        };
    }

    private static SKSizeI ScaleToFit(
        int sourceWidth,
        int sourceHeight,
        int width,
        int height,
        Func<float, float, float> chooseScale)
    {
        var scale = chooseScale((float)width / sourceWidth, (float)height / sourceHeight);
        return new SKSizeI(
            Math.Max(1, (int)MathF.Round(sourceWidth * scale)),
            Math.Max(1, (int)MathF.Round(sourceHeight * scale)));
    }

    private static SKRect GetSourceRect(
        int sourceWidth,
        int sourceHeight,
        int width,
        int height,
        ResizeMode mode,
        AnchorPositionMode position)
    {
        if (mode != ResizeMode.Crop)
            return new SKRect(0, 0, sourceWidth, sourceHeight);

        var targetRatio = (float)width / height;
        var sourceRatio = (float)sourceWidth / sourceHeight;
        float cropWidth = sourceWidth;
        float cropHeight = sourceHeight;

        if (sourceRatio > targetRatio)
            cropWidth = sourceHeight * targetRatio;
        else
            cropHeight = sourceWidth / targetRatio;

        var left = GetAnchorOffset(sourceWidth, cropWidth, position, true);
        var top = GetAnchorOffset(sourceHeight, cropHeight, position, false);
        return new SKRect(left, top, left + cropWidth, top + cropHeight);
    }

    private static SKRect GetDestinationRect(
        int targetWidth,
        int targetHeight,
        int width,
        int height,
        ResizeMode mode)
    {
        if (mode != ResizeMode.Pad)
            return new SKRect(0, 0, targetWidth, targetHeight);

        var scale = Math.Min((float)width / targetWidth, (float)height / targetHeight);
        var drawWidth = targetWidth * scale;
        var drawHeight = targetHeight * scale;
        var left = (width - drawWidth) / 2;
        var top = (height - drawHeight) / 2;
        return new SKRect(left, top, left + drawWidth, top + drawHeight);
    }

    private static float GetAnchorOffset(
        int sourceSize,
        float cropSize,
        AnchorPositionMode position,
        bool horizontal)
    {
        var endAligned = horizontal
            ? position is AnchorPositionMode.Right or AnchorPositionMode.TopRight or AnchorPositionMode.BottomRight
            : position is AnchorPositionMode.Bottom or AnchorPositionMode.BottomLeft or AnchorPositionMode.BottomRight;
        if (endAligned)
            return sourceSize - cropSize;

        var startAligned = horizontal
            ? position is AnchorPositionMode.Left or AnchorPositionMode.TopLeft or AnchorPositionMode.BottomLeft
            : position is AnchorPositionMode.Top or AnchorPositionMode.TopLeft or AnchorPositionMode.TopRight;
        return startAligned ? 0 : (sourceSize - cropSize) / 2;
    }

    private static string GetFormatName(SKEncodedImageFormat imageFormat)
    {
        return imageFormat switch
        {
            SKEncodedImageFormat.Png => "PNG",
            SKEncodedImageFormat.Jpeg => "JPEG",
            SKEncodedImageFormat.Gif => "GIF",
            SKEncodedImageFormat.Webp => "WEBP",
            SKEncodedImageFormat.Bmp => "BMP",
            SKEncodedImageFormat.Ico => "ICO",
            SKEncodedImageFormat.Wbmp => "WBMP",
            SKEncodedImageFormat.Pkm => "PKM",
            SKEncodedImageFormat.Ktx => "KTX",
            SKEncodedImageFormat.Astc => "ASTC",
            SKEncodedImageFormat.Dng => "DNG",
            SKEncodedImageFormat.Heif => "HEIF",
            SKEncodedImageFormat.Avif => "AVIF",
            _ => imageFormat.ToString().ToUpperInvariant(),
        };
    }

    private static string GetMimeType(SKEncodedImageFormat imageFormat)
    {
        return imageFormat switch
        {
            SKEncodedImageFormat.Png => "image/png",
            SKEncodedImageFormat.Jpeg => "image/jpeg",
            SKEncodedImageFormat.Gif => "image/gif",
            SKEncodedImageFormat.Webp => "image/webp",
            SKEncodedImageFormat.Bmp => "image/bmp",
            SKEncodedImageFormat.Ico => "image/x-icon",
            SKEncodedImageFormat.Wbmp => "image/vnd.wap.wbmp",
            SKEncodedImageFormat.Heif => "image/heif",
            SKEncodedImageFormat.Avif => "image/avif",
            _ => "application/octet-stream",
        };
    }

    private static string GetFileExtension(SKEncodedImageFormat imageFormat)
    {
        return imageFormat switch
        {
            SKEncodedImageFormat.Png => "png",
            SKEncodedImageFormat.Jpeg => "jpg",
            SKEncodedImageFormat.Gif => "gif",
            SKEncodedImageFormat.Webp => "webp",
            SKEncodedImageFormat.Bmp => "bmp",
            SKEncodedImageFormat.Ico => "ico",
            SKEncodedImageFormat.Wbmp => "wbmp",
            SKEncodedImageFormat.Pkm => "pkm",
            SKEncodedImageFormat.Ktx => "ktx",
            SKEncodedImageFormat.Astc => "astc",
            SKEncodedImageFormat.Dng => "dng",
            SKEncodedImageFormat.Heif => "heif",
            SKEncodedImageFormat.Avif => "avif",
            _ => imageFormat.ToString().ToLowerInvariant(),
        };
    }
}
