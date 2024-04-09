using System;
using System.Globalization;

namespace GalliumMath
{


public struct Vector3Int {
    public int x, y, z;

    public float magnitude => Mathf.Sqrt( ( float )( x * x + y * y + z * z ) );
    public int sqrMagnitude => x * x + y * y + z * z;

    public Vector3Int( int x, int y ) {
        this.x = x;
        this.y = y;
        this.z = 0;
    }

    public Vector3Int( int x, int y, int z ) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override int GetHashCode() {
        int yHash = y.GetHashCode();
        int zHash = z.GetHashCode();
        return x.GetHashCode()
                ^ ( yHash << 4 ) ^ ( yHash >> 28 )
                ^ ( zHash >> 4 ) ^ ( zHash << 28 );
    }

    public override bool Equals(object other) {
        return ( other is Vector3Int ovi ) && Equals( ovi );
    }

    public bool Equals(Vector3Int other) {
        return this == other;
    }

    public static Vector3Int operator+( Vector3Int a, Vector3Int b) {
        return new Vector3Int( a.x + b.x, a.y + b.y, a.z + b.z );
    }

    public static Vector3Int operator-( Vector3Int a, Vector3Int b) {
        return new Vector3Int( a.x - b.x, a.y - b.y, a.z - b.z );
    }

    public static Vector3Int operator*( Vector3Int a, Vector3Int b) {
        return new Vector3Int( a.x * b.x, a.y * b.y, a.z * b.z );
    }

    public static Vector3Int operator-( Vector3Int a ) {
        return new Vector3Int( -a.x, -a.y, -a.z );
    }

    public static Vector3Int operator*( Vector3Int a, int b ) {
        return new Vector3Int( a.x * b, a.y * b, a.z * b );
    }

    public static Vector3Int operator*(int a, Vector3Int b ) {
        return new Vector3Int( a * b.x, a * b.y, a * b.z );
    }

    public static Vector3Int operator/( Vector3Int a, int b ) {
        return new Vector3Int( a.x / b, a.y / b, a.z / b );
    }

    public static bool operator==( Vector3Int lhs, Vector3Int rhs ) {
        return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
    }

    public static bool operator!=( Vector3Int lhs, Vector3Int rhs ) {
        return ! ( lhs == rhs  );
    }

}


}
