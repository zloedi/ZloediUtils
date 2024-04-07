using System;
using System.Runtime.InteropServices;
using scm = System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace GalliumMath
{


public partial struct Vector3 {
    public const float EPS = 0.00001F;

    public float x;
    public float y;
    public float z;

    public Vector3( float x, float y, float z ) {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public Vector3( float x, float y ) {
        this.x = x;
        this.y = y;
        z = 0f;
    }

    public override int GetHashCode() {
        return x.GetHashCode() ^ ( y.GetHashCode() << 2 ) ^ ( z.GetHashCode() >> 2 );
    }

    public override bool Equals(object other) {
        return ( other is Vector3 ov ) && Equals( ov );
    }

    public bool Equals( Vector3 other ) {
        return x == other.x && y == other.y && z == other.z;
    }

    public static Vector3 operator+(Vector3 a, Vector3 b) {
        return new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static Vector3 operator-(Vector3 a, Vector3 b) {
        return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }

    public static Vector3 operator-(Vector3 a) {
        return new Vector3(-a.x, -a.y, -a.z);
    }

    public static Vector3 operator*(Vector3 a, float d) {
        return new Vector3(a.x * d, a.y * d, a.z * d);
    }

    public static Vector3 operator*(float d, Vector3 a) {
        return new Vector3(a.x * d, a.y * d, a.z * d);
    }

    public static Vector3 operator/(Vector3 a, float d) {
        return new Vector3(a.x / d, a.y / d, a.z / d);
    }

    public static bool operator==( Vector3 lhs, Vector3 rhs ) {
        float diff_x = lhs.x - rhs.x;
        float diff_y = lhs.y - rhs.y;
        float diff_z = lhs.z - rhs.z;
        float sqrmag = diff_x * diff_x + diff_y * diff_y + diff_z * diff_z;
        return sqrmag < EPS * EPS;
    }

    public static bool operator!=(Vector3 lhs, Vector3 rhs) {
        return !( lhs == rhs );
    }

    public static Vector3 Lerp( Vector3 a, Vector3 b, float t ) {
        return a + ( b - a ) * Mathf.Clamp01( t );
    }
}


}
