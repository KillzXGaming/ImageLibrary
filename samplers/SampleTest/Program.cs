
using ImageLibrary;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using ImageLibrary.PlatformSwizzle;
using ImageLibrary.Formats.Encoders;

var encoder = new Rgba16F(ChannelLayout.RGBA);

float[] input = { 1.0f, 0.5f, 0.25f, 1.0f };
byte[] encoded = encoder.EncodeFloats(input, 1, 1);

foreach (var b in encoded)
    Console.Write($"{b:X2} ");

// Unorm
byte[] decoded = encoder.Decode(encoded, 1, 1);

foreach (var f in decoded)
    Console.WriteLine(f);