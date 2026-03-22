using MTS.Converter;
using Newtonsoft.Json;

namespace MTS.Game;
[JsonConverter(typeof(ColorArrayConverter))]
public struct Color
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
}