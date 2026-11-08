using SkiaSharp;

namespace Api.Profile;

internal static class ProfileImage
{
    internal const int MaxFileSize = 5 * 1024 * 1024;
    private const int MaxDimension = 4096;
    private const int MaxPixels = 12_000_000;

    internal static byte[]? Normalize(byte[] bytes, int maxOutputDimension)
    {
        using var stream = new SKMemoryStream(bytes);
        using var codec = SKCodec.Create(stream);
        if (codec is null || codec.EncodedFormat is not (SKEncodedImageFormat.Jpeg
            or SKEncodedImageFormat.Png or SKEncodedImageFormat.Webp))
            return null;

        var (width, height) = (codec.Info.Width, codec.Info.Height);
        if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension
            || width * height > MaxPixels || codec.FrameCount > 1)
            return null;

        // Decode into a fresh bitmap, so only pixels reach the encoder.
        var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var bitmap = new SKBitmap(info);
        if (codec.GetPixels(info, bitmap.GetPixels()) != SKCodecResult.Success)
            return null;

        var transposed = (int)codec.EncodedOrigin >= (int)SKEncodedOrigin.LeftTop;
        var scale = Math.Min(1d, (double)maxOutputDimension / Math.Max(width, height));
        var outputWidth = Math.Max(1, (int)Math.Round((transposed ? height : width) * scale));
        var outputHeight = Math.Max(1, (int)Math.Round((transposed ? width : height) * scale));
        using var output = new SKBitmap(outputWidth, outputHeight, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(output);
        canvas.Clear(SKColors.Transparent);
        canvas.Scale((float)outputWidth / (transposed ? height : width),
            (float)outputHeight / (transposed ? width : height));
        canvas.Concat(Orientation(codec.EncodedOrigin, width, height));
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
        using var encoded = output.Encode(SKEncodedImageFormat.Webp, 85);
        return encoded is { Size: > 0 and <= MaxFileSize } ? encoded.ToArray() : null;
    }

    private static SKMatrix Orientation(SKEncodedOrigin origin, int width, int height) => origin switch
    {
        SKEncodedOrigin.TopRight => new(-1, 0, width, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new(-1, 0, width, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new(1, 0, 0, 0, -1, height, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new(0, -1, height, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new(0, -1, height, -1, 0, width, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new(0, 1, 0, -1, 0, width, 0, 0, 1),
        _ => SKMatrix.Identity
    };
}
