using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GalliumMath
{
    

public struct Vector2Int {
    public int x, y;

    public static readonly Vector2Int zero = new Vector2Int( 0, 0 );
    public static readonly Vector2Int one = new Vector2Int( 1, 1 );
    public static readonly Vector2Int up = new Vector2Int( 0, 1 );
    public static readonly Vector2Int down = new Vector2Int( 0, -1 );
    public static readonly Vector2Int left = new Vector2Int( -1, 0 );
    public static readonly Vector2Int right = new Vector2Int( 1, 0 );

    public Vector2Int( int x, int y ) {
        this.x = x;
        this.y = y;
    }

    public override bool Equals(object other) {
        return other is Vector2Int ovi && Equals( ovi );
    }

    public bool Equals(Vector2Int other) {
        return x == other.x && y == other.y;
    }

    public override int GetHashCode() {
        return x.GetHashCode() ^ ( y.GetHashCode() << 2 );
    }

    public static implicit operator Vector2( Vector2Int v ) {
        return new Vector2( v.x, v.y );
    }

    public static Vector2Int operator-( Vector2Int v ) {
        return new Vector2Int( -v.x, -v.y );
    }

    public static Vector2Int operator+( Vector2Int a, Vector2Int b ) {
        return new Vector2Int( a.x + b.x, a.y + b.y );
    }

    public static Vector2Int operator-( Vector2Int a, Vector2Int b ) {
        return new Vector2Int( a.x - b.x, a.y - b.y );
    }

    public static Vector2Int operator*( Vector2Int a, Vector2Int b ) {
        return new Vector2Int( a.x * b.x, a.y * b.y );
    }

    public static Vector2Int operator*( int a, Vector2Int b ) {
        return new Vector2Int( a * b.x, a * b.y );
    }

    public static Vector2Int operator*( Vector2Int a, int b ) {
        return new Vector2Int( a.x * b, a.y * b );
    }

    public static Vector2Int operator/( Vector2Int a, int b ) {
        return new Vector2Int( a.x / b, a.y / b );
    }

    public static bool operator==( Vector2Int lhs, Vector2Int rhs ) {
        return lhs.x == rhs.x && lhs.y == rhs.y;
    }

    public static bool operator!=( Vector2Int lhs, Vector2Int rhs ) {
        return ! ( lhs == rhs );
    }
}


}
