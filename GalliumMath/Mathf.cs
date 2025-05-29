using System;

namespace GalliumMath
{


public partial struct Mathf {
    public static readonly float Epsilon = Single.Epsilon == 0 ? 1.17549435E-38f : Single.Epsilon;

    public const float PI = ( float )Math.PI;
    public const float Deg2Rad = PI * 2f / 360f;
    public const float Rad2Deg = 1f / Deg2Rad;

    public static float Sign( float f ) {
        return f >= 0f ? 1f : -1f;
    }

    public static float Sin( float f ) {
        return ( float )Math.Sin( f );
    }

    public static float Cos( float f ) {
        return ( float )Math.Cos( f );
    }

    public static float Tan(float f) {
        return ( float )Math.Tan(f);
    }

    public static float Asin(float f) {
        return ( float )Math.Asin(f);
    }

    public static float Acos(float f) {
        return ( float )Math.Acos(f); 
    }

    public static float Atan(float f) {
        return ( float )Math.Atan(f);
    }

    public static float Atan2( float y, float x ) {
        return ( float )Math.Atan2(y, x);
    }

    public static float Sqrt(float f) {
        return ( float )Math.Sqrt( f );
    }

    public static float Abs( float f ) {
        return ( float )Math.Abs( f );
    }

    public static int Abs( int value ) {
        return Math.Abs( value );
    }

    public static float Min( float a, float b ) {
        return a < b ? a : b;
    }

    public static int Min( int a, int b ) {
        return a < b ? a : b;
    }

    public static float Max( float a, float b ) {
        return a > b ? a : b;
    }

    public static int Max( int a, int b ) {
        return a > b ? a : b;
    }

    public static float Clamp( float val, float min, float max ) {
        return Max( min, Min( val, max ) );
    }

    public static int Clamp( int val, int min, int max ) {
        return Max( min, Min( val, max ) );
    }

    public static float Clamp01( float val ) {
        return Clamp( val, 0, 1 );
    }

    public static float Round( float f ) {
        return ( float )Math.Round( f );
    }

    public static float Floor( float f ) {
        return ( float )Math.Floor( f );
    }

    public static int FloorToInt( float f ) {
        return ( int )Math.Floor( f );
    }

    public static int CeilToInt( float f ) {
        return ( int )Math.Ceiling( f );
    }

    public static int RoundToInt( float f ) {
        return ( int )Math.Round( f );
    }
}


}
