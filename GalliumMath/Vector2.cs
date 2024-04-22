using System;
using System.Globalization;

namespace GalliumMath
{


public struct Vector2 {
    const float EPS = 0.00001f;
    const float EPS_SQRT = 1e-15f;

    public float x;
    public float y;

    public float magnitude => ( float )Math.Sqrt( x * x + y * y );
    public float sqrMagnitude => x * x + y * y;
    public Vector2 normalized => new Vector2( x, y ) / magnitude;

    public static readonly Vector2 zero = new Vector2( 0f, 0f );
    public static readonly Vector2 one = new Vector2( 1f, 1f );
    public static readonly Vector2 up = new Vector2( 0f, 1f );
    public static readonly Vector2 down = new Vector2( 0f, -1f );
    public static readonly Vector2 left = new Vector2( -1f, 0f );
    public static readonly Vector2 righ = new Vector2( 1f, 0f );

    public Vector2( float x, float y ) {
        this.x = x; this.y = y;
    }

    public override int GetHashCode() {
        return x.GetHashCode() ^ ( y.GetHashCode() << 2 );
    }

    public override bool Equals( object other ) {
        return other is Vector2 ov && Equals( ov );
    }

    public bool Equals( Vector2 other ) {
        return x == other.x && y == other.y;
    }

    public static Vector2 operator+( Vector2 a, Vector2 b ) {
        return new Vector2(a.x + b.x, a.y + b.y );
    }

    public static Vector2 operator-( Vector2 a, Vector2 b ) {
        return new Vector2(a.x - b.x, a.y - b.y );
    }

    public static Vector2 operator*( Vector2 a, Vector2 b ) {
        return new Vector2(a.x * b.x, a.y * b.y );
    }

    public static Vector2 operator/( Vector2 a, Vector2 b ) {
        return new Vector2(a.x / b.x, a.y / b.y );
    }

    public static Vector2 operator-( Vector2 a ) {
        return new Vector2(-a.x, -a.y);
    }

    public static Vector2 operator*( Vector2 a, float d ) {
        return new Vector2( a.x * d, a.y * d  );
    }

    public static Vector2 operator*( float d, Vector2 a ) {
        return new Vector2( a.x * d, a.y * d );
    }

    public static Vector2 operator/( Vector2 a, float d ) {
        return new Vector2( a.x / d, a.y / d );
    }

    public static bool operator==( Vector2 a, Vector2 b ) {
        float dx = a.x - b.x;
        float dy = a.y - b.y;
        return ( dx * dx + dy * dy ) < EPS * EPS;
    }

    public static bool operator!=( Vector2 a, Vector2 b ) {
        return !( a == b );
    }

    public static implicit operator Vector2( Vector3 v ) {
        return new Vector2( v.x, v.y );
    }

    public static implicit operator Vector3( Vector2 v ) {
        return new Vector3( v.x, v.y, 0 );
    }

    public static Vector2 Lerp( Vector2 a, Vector2 b, float t ) {
        return a + ( b - a ) * Mathf.Clamp01( t );
    }

    public static float Dot( Vector2 a, Vector2 b ) {
        return a.x * b.x + a.y * b.y;
    }

    public static float Angle( Vector2 a, Vector2 b ) {
        float denominator = ( float )Math.Sqrt( a.sqrMagnitude * b.sqrMagnitude );
        if ( denominator < EPS_SQRT )
            return 0f;

        float dot = Mathf.Clamp( Dot( a, b ) / denominator, -1f, 1f );
        return ( float )Math.Acos( dot ) * Mathf.Rad2Deg;
    }

    public static float SignedAngle( Vector2 a, Vector2 b ) {
        float sign = Mathf.Sign( a.x * b.y - a.y * b.x );
        return Angle( a, b ) * sign;
    }
}


}
