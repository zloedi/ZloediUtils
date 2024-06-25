using System;
using System.Globalization;

namespace GalliumMath
{


public struct Color {
    public float r, g, b, a;

    public static Color red     => new Color(1f, 0f, 0f, 1f);
    public static Color green   => new Color(0f, 1f, 0f, 1f);
    public static Color blue    => new Color(0f, 0f, 1f, 1f);
    public static Color white   => new Color(1f, 1f, 1f, 1f);
    public static Color black   => new Color(0f, 0f, 0f, 1f);
    public static Color yellow  => new Color(1f, 235f / 255f, 4f / 255f, 1f);
    public static Color cyan    => new Color(0f, 1f, 1f, 1f);
    public static Color magenta => new Color(1f, 0f, 1f, 1f);
    public static Color gray    => new Color(.5f, .5f, .5f, 1f);
    public static Color clear   => new Color(0f, 0f, 0f, 0f);

    public float grayscale => 0.299f * r + 0.587f * g + 0.114f * b;

    public Color( float r, float g, float b, float a ) {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    public Color( float r, float g, float b ) {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = 1.0f;
    }

    internal static string FtoA( float f ) {
        return f.ToString( "F3", CultureInfo.InvariantCulture.NumberFormat ); 
    }

    public override string ToString() {
        return string.Format( $"({FtoA( r )}, {FtoA( g )}, {FtoA( b )}, {FtoA( a )})" );
    }

    public static Color operator+( Color a, Color b ) => new Color( a.r + b.r, a.g + b.g, a.b + b.b, a.a + b.a );
    public static Color operator-( Color a, Color b ) => new Color( a.r - b.r, a.g - b.g, a.b - b.b, a.a - b.a );
    public static Color operator*( Color a, Color b ) => new Color( a.r * b.r, a.g * b.g, a.b * b.b, a.a * b.a );
    public static Color operator*( Color a, float b ) => new Color( a.r * b, a.g * b, a.b * b, a.a * b );
    public static Color operator*( float b, Color a ) => new Color( a.r * b, a.g * b, a.b * b, a.a * b );
    public static Color operator/( Color a, float b ) => new Color( a.r / b, a.g / b, a.b / b, a.a / b );

    public static Color Lerp( Color a, Color b, float t ) {
        return a + ( b - a ) * Mathf.Clamp01( t );
    }
}


}
