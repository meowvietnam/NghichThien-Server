using MTS.Converter;
using Newtonsoft.Json;

namespace MTS.Game;

[JsonConverter(typeof(ColorArrayConverter))]
public struct Color : IEquatable<Color>
{
    public float R { get; set; }
    public float G { get; set; }
    public float B { get; set; }
    public float A { get; set; }

    public static Color Round(Color c, int digits = 3)
    {
        return new Color
        {
            R = (float)Math.Round(c.R, digits),
            G = (float)Math.Round(c.G, digits),
            B = (float)Math.Round(c.B, digits),
            A = (float)Math.Round(c.A, digits)
        };
    }

    public bool Equals(Color other)
    {
        return R == other.R &&
               G == other.G &&
               B == other.B &&
               A == other.A;
    }
    public override bool Equals(object obj)
    {
        return obj is Color other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    public static bool operator ==(Color left, Color right)
        => left.Equals(right);

    public static bool operator !=(Color left, Color right)
        => !left.Equals(right);
}