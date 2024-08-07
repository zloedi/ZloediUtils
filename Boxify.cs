#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER

using System.Collections.Generic;
using System.Diagnostics;
using System;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

using TVox = System.Byte;

public static class Boxify
{
    public static readonly Vector3 VoxelHalf = Vector3.one * 0.5f;

    public delegate void LogDelegate( string s, UnityEngine.Object o = null );

    public static LogDelegate Log = (s,o)=>{};
    public static LogDelegate Error = (s,o)=>{};
 
    public static Vector3 GetGeomCenter(MeshFilter mf)
    {
        return mf.transform.TransformPoint(mf.sharedMesh.bounds.center);
    }

    public static bool IntersectsBox( Vector3 a, Vector3 b, Vector3 c, Vector3 boxCenter, 
                                                                                Vector3 boxExtents )
    {
        // Translate triangle as conceptually moving AABB to origin
        var v0 = a - boxCenter;
        var v1 = b - boxCenter;
        var v2 = c - boxCenter;

        // Compute edge vectors for triangle
        var f0 = v1 - v0;
        var f1 = v2 - v1;
        var f2 = v0 - v2;

        // Test axis a00
        var a00 = new Vector3( 0, -f0.z, f0.y );
        var p0 = Vector3.Dot( v0, a00 );
        var p1 = Vector3.Dot( v1, a00 );
        var p2 = Vector3.Dot( v2, a00 );
        var r = boxExtents.y * Mathf.Abs( f0.z ) + boxExtents.z * Mathf.Abs( f0.y );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a01
        var a01 = new Vector3( 0, -f1.z, f1.y );
        p0 = Vector3.Dot( v0, a01 );
        p1 = Vector3.Dot( v1, a01 );
        p2 = Vector3.Dot( v2, a01 );
        r = boxExtents.y * Mathf.Abs( f1.z ) + boxExtents.z * Mathf.Abs( f1.y );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a02
        var a02 = new Vector3( 0, -f2.z, f2.y );
        p0 = Vector3.Dot( v0, a02 );
        p1 = Vector3.Dot( v1, a02 );
        p2 = Vector3.Dot( v2, a02 );
        r = boxExtents.y * Mathf.Abs( f2.z ) + boxExtents.z * Mathf.Abs( f2.y );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a10
        var a10 = new Vector3( f0.z, 0, -f0.x );
        p0 = Vector3.Dot( v0, a10 );
        p1 = Vector3.Dot( v1, a10 );
        p2 = Vector3.Dot( v2, a10 );
        r = boxExtents.x * Mathf.Abs( f0.z ) + boxExtents.z * Mathf.Abs( f0.x );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a11
        var a11 = new Vector3( f1.z, 0, -f1.x );
        p0 = Vector3.Dot( v0, a11 );
        p1 = Vector3.Dot( v1, a11 );
        p2 = Vector3.Dot( v2, a11 );
        r = boxExtents.x * Mathf.Abs( f1.z ) + boxExtents.z * Mathf.Abs( f1.x );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a12
        var a12 = new Vector3( f2.z, 0, -f2.x );
        p0 = Vector3.Dot( v0, a12 );
        p1 = Vector3.Dot( v1, a12 );
        p2 = Vector3.Dot( v2, a12 );
        r = boxExtents.x * Mathf.Abs( f2.z ) + boxExtents.z * Mathf.Abs( f2.x );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a20
        var a20 = new Vector3( -f0.y, f0.x, 0 );
        p0 = Vector3.Dot( v0, a20 );
        p1 = Vector3.Dot( v1, a20 );
        p2 = Vector3.Dot( v2, a20 );
        r = boxExtents.x * Mathf.Abs( f0.y ) + boxExtents.y * Mathf.Abs( f0.x );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a21
        var a21 = new Vector3( -f1.y, f1.x, 0 );
        p0 = Vector3.Dot( v0, a21 );
        p1 = Vector3.Dot( v1, a21 );
        p2 = Vector3.Dot( v2, a21 );
        r = boxExtents.x * Mathf.Abs( f1.y ) + boxExtents.y * Mathf.Abs( f1.x );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Test axis a22
        var a22 = new Vector3( -f2.y, f2.x, 0 );
        p0 = Vector3.Dot( v0, a22 );
        p1 = Vector3.Dot( v1, a22 );
        p2 = Vector3.Dot( v2, a22 );
        r = boxExtents.x * Mathf.Abs( f2.y ) + boxExtents.y * Mathf.Abs( f2.x );
        if( Mathf.Max( -fmax( p0, p1, p2 ), fmin( p0, p1, p2 ) ) > r )
        {
            return false;
        }

        // Exit if...
        // ... [-extents.x, extents.x] and [min(v0.x,v1.x,v2.x), max(v0.x,v1.x,v2.x)] do not overlap
        if( fmax( v0.x, v1.x, v2.x ) < -boxExtents.x || fmin( v0.x, v1.x, v2.x ) > boxExtents.x )
        {
            return false;
        }

        // ... [-extents.y, extents.y] and [min(v0.y,v1.y,v2.y), max(v0.y,v1.y,v2.y)] do not overlap
        if( fmax( v0.y, v1.y, v2.y ) < -boxExtents.y || fmin( v0.y, v1.y, v2.y ) > boxExtents.y )
        {
            return false;
        }
                
        // ... [-extents.z, extents.z] and [min(v0.z,v1.z,v2.z), max(v0.z,v1.z,v2.z)] do not overlap
        if( fmax( v0.z, v1.z, v2.z ) < -boxExtents.z || fmin( v0.z, v1.z, v2.z ) > boxExtents.z )
        {
            return false;
        }

        Vector3 planeNormal = Vector3.Cross( f0, f1 );

        // Compute the projection interval radius of b onto L(t) = b.c + t * p.n
        float radius = boxExtents.x * Mathf.Abs( planeNormal.x )
                        + boxExtents.y * Mathf.Abs( planeNormal.y )
                        + boxExtents.z * Mathf.Abs( planeNormal.z );
        float planeDistance = Vector3.Dot( planeNormal, a );
        float centerDistance = Vector3.Dot( planeNormal, boxCenter );
        float s = centerDistance - planeDistance;

        // Intersection occurs when plane distance falls within [-r,+r] interval
        return Mathf.Abs(s) <= radius;
    }

    public static Vector3 AABBDist(Vector3 centerA, Vector3 extA, Vector3 centerB, Vector3 extB)
    {
        Vector3 d = centerB - centerA;
        return new Vector3(Mathf.Abs(d.x), Mathf.Abs(d.y), Mathf.Abs(d.z)) - (extA + extB);
    }

    private static float fmin( float a, float b, float c )
    {
        return Mathf.Min( a, Mathf.Min( b, c ) );
    }

    private static float fmax( float a, float b, float c )
    {
        return Mathf.Max( a, Mathf.Max( b, c ) ); 
    }

    private static Vector3 Round(Vector3 v)
    {
        return new Vector3(Mathf.Round(v.x),
                            Mathf.Round(v.y),
                            Mathf.Round(v.z));
    }

    private static void BoundsInVoxels( Vector3 min, Vector3 max, float voxelSize,
                                                out Vector3Int floorMin, out Vector3Int floorMax ) {
        // allow a bit deviation from the grid, so stuff don't get stuck in the floor if tiny clip
        const float allowedError = 0.02f;
        const float aeSqr = allowedError * allowedError;

        Vector3 [] minmaxScaled = {
            min / voxelSize,
            max / voxelSize,
        };

        Vector3 [] minmaxRounded = {
            Round(minmaxScaled[0]),
            Round(minmaxScaled[1]),
        };

        float [] minmaxBump = {
            0,
            1,
        };

        //Log("Rend Min: " + minmaxScaled[0].ToString("F4"));
        //Log("Rend Max: " + minmaxScaled[1].ToString("F4"));

        Vector3Int [] result = new Vector3Int[2];

        //char [] xyz = {'x', 'y', 'z'};
        //string [] minmaxs = {"Min", "Max"};

        for (int i = 0; i < 2; i++) {
            Vector3 vRound = minmaxRounded[i];
            Vector3 vScale = minmaxScaled[i];
            //Log(minmaxs[i] + " Scale: " + vScale.ToString("F4"));
            //Log(minmaxs[i] + " Round: " + vRound.ToString("F4"));
            for (int j = 0; j < 3; j++) {
                float sj = vScale[j];
                float rj = vRound[j];
                float e = sj - rj;
                //Log("Error " + xyz[j] + ": " + Mathf.Abs(e));
                if ( e * e < aeSqr ) {
                    result[i][j] = (int)Mathf.Floor(rj - minmaxBump[i]);
                } else {
                    result[i][j] = (int)Mathf.Floor(sj);
                }
            }
        }

        floorMin = result[0];
        floorMax = new Vector3Int( Mathf.Max( floorMin.x, result[1].x ),
                                    Mathf.Max( floorMin.y, result[1].y ),
                                    Mathf.Max( floorMin.z, result[1].z) );
    }

    public static void BoundsInGrid(Vector3 min, Vector3 max, Vector3Int gridMin,
                                                Vector3Int gridSz, float voxelSize,
                                                out Vector3Int floorMin, out Vector3Int floorMax) {
        BoundsInVoxels( min, max, voxelSize, out floorMin, out floorMax );
        floorMin -= gridMin;
        floorMax -= gridMin;

        floorMin.x = Mathf.Clamp( floorMin.x, 0, gridSz.x - 1 );
        floorMin.y = Mathf.Clamp( floorMin.y, 0, gridSz.y - 1 );
        floorMin.z = Mathf.Clamp( floorMin.z, 0, gridSz.z - 1 );

        floorMax.x = Mathf.Clamp( floorMax.x, 0, gridSz.x - 1 );
        floorMax.y = Mathf.Clamp( floorMax.y, 0, gridSz.y - 1 );
        floorMax.z = Mathf.Clamp( floorMax.z, 0, gridSz.z - 1 );

    }

    public static bool GetBoundsOfRenderers(Renderer [] rends, out Bounds result) 
    {
        if (rends.Length > 0) {
            result = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) {
                result.Encapsulate(rends[i].bounds);
            }
            return true;
        }
        result = new Bounds();
        return false;
    }

    public static bool GetBoundsOfColliders(Collider [] colliders, out Bounds result) 
    {
        if (colliders.Length > 0) {
            result = colliders[0].bounds;
            for (int i = 1; i < colliders.Length; i++) {
                result.Encapsulate(colliders[i].bounds);
            }
            return true;
        }
        result = new Bounds();
        return false;
    }

    public static bool GetBoundsOfRenderers(Renderer [] rends, float voxelSize, 
                                                out Vector3Int min, out Vector3Int max) 
    {
        Bounds bounds;
        if (GetBoundsOfRenderers(rends, out bounds)) {
            BoundsInVoxels(bounds.min, bounds.max, voxelSize, out min, out max);
            return true;
        }
        min = max = Vector3Int.zero;
        return false;
    }

    private static bool AllocateGrid(Renderer [] rends, float voxelSize, 
                                    out Vector3Int min, out Vector3Int max, out TVox [] grid) 
    {
        if ( GetBoundsOfRenderers( rends, voxelSize, out min, out max ) ) {
            Log("Grid min: " + min);
            Log("Grid max: " + max);
            Vector3Int sz = MinMaxToSize(min, max);
            grid = new TVox [sz.x * sz.y * sz.z];
            Log("Allocated grid with size " + sz.x + "," + sz.y + "," + sz.z);
            return true;
        }
        Log( "No renderers to boxify in selection." );
        grid = null;
        return false;
    }

    private static Color32 [] AllocateColors( TVox [] grid ) {
        return new Color32 [grid.Length];
    }

    private static bool AllocateGrid(Transform [] objects, float voxelSize, out Vector3Int min, 
                                        out Vector3Int max, out TVox [] grid) 
    {
        var rends = new List<Renderer>();
        foreach(Transform t in objects) {
            rends.AddRange(t.GetComponentsInChildren<Renderer>());
        }
        return AllocateGrid(rends.ToArray(), voxelSize, out min, out max, out grid);
    }

    public static Vector3Int GridCoord(int idx, Vector3Int gridSz) 
    {
        int x = idx % gridSz.x;
        idx /= gridSz.x;
        int y = idx % gridSz.y;
        idx /= gridSz.y;
        int z = idx;
        return new Vector3Int(x,y,z);
    }

    public static int GridIdx(float x, float y, float z, Vector3Int gridSz)
    {
        int ix = (int)x;
        int iy = (int)y;
        int iz = (int)z;
        return ix + iy * gridSz.x + iz * gridSz.x * gridSz.y;
    }

    public static int GridIdx(Vector3Int vi, Vector3Int gridSz)
    {
        return vi.x + vi.y * gridSz.x + vi.z * gridSz.x * gridSz.y;
    }

    public static Vector3 ClampVertexToRendBox(Vector3 v, Vector3Int min, Vector3Int max)
    {
#if true
        return new Vector3(Mathf.Clamp(v.x, min.x, max.x),
                            Mathf.Clamp(v.y, min.y, max.y),
                            Mathf.Clamp(v.z, min.z, max.z));
#else
        // FIXME: there is something fishy going on when boxifying with big errors
        return new Vector3(Mathf.Clamp(v.x, min.x + 0.001f, max.x + 0.99f),
                            Mathf.Clamp(v.y, min.y + 0.001f, max.y + 0.99f),
                            Mathf.Clamp(v.z, min.z + 0.001f, max.z + 0.99f));
#endif
    }

    private static bool IsSolid(TVox voxel) {
        return voxel != 0;
    }

    private static bool IsHollow(TVox voxel) {
        return voxel == 0;
    }

    private static void TurnSolid(ref TVox voxel) {
        voxel = 1;
    }

    private static Color SampleTexture( Vector3 p, Vector3 a, Vector3 b, Vector3 c,
                                            Vector2 aUV, Vector2 bUV, Vector2 cUV, Texture2D tex ) {
        Vector3 n = Vector3.Cross( a - c, b - c ).normalized;
        float dt = Vector3.Dot( p - c, n );
        Vector3 projected = p - n * dt;
        Vector2 uv = UVForPoint( projected, a, b, c, aUV, bUV, cUV );
        float w = tex.width;
        float h = tex.height;
        Color cl = tex.GetPixel( ( int )( uv.x * w ), ( int )( uv.y * h ) );
        float cr = Mathf.GammaToLinearSpace( cl.r );
        float cg = Mathf.GammaToLinearSpace( cl.g );
        float cb = Mathf.GammaToLinearSpace( cl.b );
        cl = new Color( cr, cg, cb, 1 );
        return cl;
    }

    // FIXME: use in fracture too
    public static Vector3Int Vector3IntCast(Vector3 v)
    {
        return new Vector3Int((int)v.x, (int)v.y, (int)v.z);
    }

    // Compute barycentric coordinates (u, v, w) for
    // point p with respect to triangle (a, b, c)
    static void Barycentric( Vector3 p, Vector3 a, Vector3 b, Vector3 c,
                                                        out float u, out float v, out float w ) {
        Vector3 v0 = b - a, v1 = c - a, v2 = p - a;
        float d00 = Vector3.Dot( v0, v0 );
        float d01 = Vector3.Dot( v0, v1 );
        float d11 = Vector3.Dot( v1, v1 );
        float d20 = Vector3.Dot( v2, v0 );
        float d21 = Vector3.Dot( v2, v1 );
        float denom = d00 * d11 - d01 * d01;
        v = ( d11 * d20 - d01 * d21 ) / denom;
        w = ( d00 * d21 - d01 * d20 ) / denom;
        u = 1.0f - v - w;
    }

    static Vector2 UVForPoint( Vector3 p, Vector3 a, Vector3 b, Vector3 c,
                                                        Vector2 aUV, Vector2 bUV, Vector2 cUV ) {
        Barycentric( p, a, b, c, out float u, out float v, out float w );
        return aUV * u  + bUV * v + cUV * w;
    }

    static void GetTriangleBounds( Vector3Int gridSz, Vector3 a, Vector3 b, Vector3 c,
                                                        out Vector3Int min, out Vector3Int max ) {
        float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
        float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));

        float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        float maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
        float maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));

        min = Vector3Int.FloorToInt(new Vector3(minX, minY, minZ));
        max = Vector3Int.FloorToInt(new Vector3(maxX, maxY, maxZ));

        //BoundsInGrid( new Vector3( minX, minY, minZ ), new Vector3( maxX, maxY, maxZ ),
        //                                                   gridMin, voxelSize, out min, out max );

        min.x = Mathf.Clamp( min.x, 0, gridSz.x - 1 );
        min.y = Mathf.Clamp( min.y, 0, gridSz.y - 1 );
        min.z = Mathf.Clamp( min.z, 0, gridSz.z - 1 );

        max.x = Mathf.Clamp( max.x, 0, gridSz.x - 1 );
        max.y = Mathf.Clamp( max.y, 0, gridSz.y - 1 );
        max.z = Mathf.Clamp( max.z, 0, gridSz.z - 1 );
    }

    // assumes local to grid a, b and c
    private static int TraceTriangle( Vector3Int gridSz, Vector3 a, Vector3 b, Vector3 c,
                                                                                    TVox [] grid ) {
        int numBoxesCreated = 0;
        GetTriangleBounds( gridSz, a, b, c, out Vector3Int min, out Vector3Int max );

        //Qonsole.Log( "triangle min: " + min );
        //Qonsole.Log( "triangle max: " + max );
        //Qonsole.Log( "===" );

        Vector3Int vi = Vector3Int.zero;
        for( vi.z = min.z; vi.z <= max.z; vi.z++ ) {
            for( vi.y = min.y; vi.y <= max.y; vi.y++ ) {
                for( vi.x = min.x; vi.x <= max.x; vi.x++ ) {
                    int idx = GridIdx(vi, gridSz);
                    if ( IsHollow( grid[idx] ) ) {
                        if ( IntersectsBox( a, b, c, vi + VoxelHalf, VoxelHalf ) ) {
                            numBoxesCreated++;
                            TurnSolid(ref grid[idx]);
                        }
                    }
                }
            }
        }

        //var ai = Boxify.GridIdx(Vector3IntCast(a), gridSz);
        //var bi = Boxify.GridIdx(Vector3IntCast(b), gridSz);
        //var ci = Boxify.GridIdx(Vector3IntCast(c), gridSz);
        //TurnSolid(ref grid[ai]);
        //TurnSolid(ref grid[bi]);
        //TurnSolid(ref grid[ci]);

        return numBoxesCreated;
    }

    private static int TraceTriangleWithColor( Vector3Int gridSz, Vector3 a, Vector3 b, Vector3 c,
                                    Vector2 aUV, Vector2 bUV, Vector2 cUV, 
                                    TVox [] grid, Color32 [] colors, Texture2D tex, Color color ) {
        int numBoxesCreated = 0;
        GetTriangleBounds( gridSz, a, b, c, out Vector3Int min, out Vector3Int max );

        Vector3Int vi = Vector3Int.zero;
        for( vi.z = min.z; vi.z <= max.z; vi.z++ ) {
            for( vi.y = min.y; vi.y <= max.y; vi.y++ ) {
                for( vi.x = min.x; vi.x <= max.x; vi.x++ ) {
                    int idx = GridIdx(vi, gridSz);
                    if ( IsHollow( grid[idx] ) ) {
                        Vector3 boxCenter = vi + VoxelHalf;
                        if ( IntersectsBox( a, b, c, boxCenter, VoxelHalf ) ) {
                            numBoxesCreated++;
                            TurnSolid( ref grid[idx] );
                            colors[idx]
                                = color * SampleTexture( boxCenter, a, b, c, aUV, bUV, cUV, tex );
                        }
                    }
                }
            }
        }

        return numBoxesCreated;
    }

    public static void GetTriangle(float voxelSize, Transform rendTransform, 
                                    int i, List<int> tris, 
                                    List<Vector3> verts, 
                                    Vector3 gridMinf,
                                    Vector3Int localRendMin, Vector3Int localRendMax, 
                                    out Vector3 a, out Vector3 b, out Vector3 c)
    {
        int ia = tris[i + 0];
        int ib = tris[i + 1];
        int ic = tris[i + 2];

        //Qonsole.Log("World Tri A: " + rendTransform.TransformPoint(verts[ia]) / voxelSize);
        //Qonsole.Log("World Tri B: " + rendTransform.TransformPoint(verts[ib]) / voxelSize);
        //Qonsole.Log("World Tri C: " + rendTransform.TransformPoint(verts[ic]) / voxelSize);

        a = rendTransform.TransformPoint(verts[ia]) / voxelSize - gridMinf;
        b = rendTransform.TransformPoint(verts[ib]) / voxelSize - gridMinf;
        c = rendTransform.TransformPoint(verts[ic]) / voxelSize - gridMinf;
        
        // our levels prefer tighter boxes over total coverage
        a = ClampVertexToRendBox(a, localRendMin, localRendMax);
        b = ClampVertexToRendBox(b, localRendMin, localRendMax);
        c = ClampVertexToRendBox(c, localRendMin, localRendMax);
    }

    static List<int> _tris = new List<int>();
    static List<Vector3> _verts = new List<Vector3>();
    static List<Vector2> _uvs = new List<Vector2>();
    private static bool Insert(Renderer rend, float voxelSize, Vector3Int gridMin, 
                                Vector3Int gridMax, TVox [] grid, out int numTrianglesProcessed, 
                                out int numBoxesCreated)
    {
        numTrianglesProcessed = 0;
        numBoxesCreated = 0;

        MeshFilter [] filters = rend.GetComponents<MeshFilter>();
        if (filters.Length == 0) {
            return false;
        }

        Vector3Int gridSz = MinMaxToSize(gridMin, gridMax);
        Vector3 gridMinf = gridMin;

        Vector3Int localRendMin, localRendMax;
        BoundsInGrid( rend.bounds.min, rend.bounds.max, gridMin, gridSz, voxelSize, 
                                                            out localRendMin, out localRendMax );

        //Qonsole.Log("Grid Min: " + gridMin);
        //Qonsole.Log("Grid Max: " + gridMax);
        //Qonsole.Log("Renderer Min: " + rend.bounds.min.ToString("F4"));
        //Qonsole.Log("Renderer Max: " + rend.bounds.max.ToString("F4"));
        //Qonsole.Log("Local Renderer Min: " + localRendMin.ToString("F4"));
        //Qonsole.Log("Local Renderer Max: " + localRendMax.ToString("F4"));

        foreach ( var f in filters ) {
            Mesh mesh = f.sharedMesh;
            if ( mesh ) {
                mesh.GetVertices( _verts );
                for ( int sub = 0; sub < mesh.subMeshCount; sub++ ) {
                    mesh.GetTriangles( _tris, sub );
                    int n = _tris.Count;
                    numTrianglesProcessed += n / 3;
                    for ( int i = 0; i < n; i += 3 ) {
                        GetTriangle( voxelSize, rend.transform, i, _tris, _verts, gridMinf, 
                                                    localRendMin, localRendMax,
                                                    out Vector3 a, out Vector3 b, out Vector3 c );
                        numBoxesCreated += TraceTriangle( gridSz, a, b, c, grid );
                    }
                }
            }
        }
        return true;
    }

    private static bool InsertWithColor( Renderer rend, float voxelSize, Vector3Int gridMin, 
                                        Vector3Int gridMax, TVox [] grid, Color32 [] colors,
                                        out int numTrianglesProcessed, out int numBoxesCreated ) {
        numTrianglesProcessed = 0;
        numBoxesCreated = 0;

        MeshFilter [] filters = rend.GetComponents<MeshFilter>();
        if (filters.Length == 0) {
            return false;
        }

        Vector3Int gridSz = MinMaxToSize( gridMin, gridMax );
        Vector3 gridMinf = gridMin;

        Vector3Int localRendMin, localRendMax;
        BoundsInGrid( rend.bounds.min, rend.bounds.max, gridMin, gridSz, voxelSize, 
                                                            out localRendMin, out localRendMax );
        var mats = rend.sharedMaterials;

        foreach ( var f in filters ) {
            Mesh mesh = f.sharedMesh;
            if ( mesh ) {
                mesh.GetVertices( _verts );
                mesh.GetUVs( 0, _uvs );
                int mcount = Mathf.Min( mesh.subMeshCount, mats.Length );
                for ( int sub = 0; sub < mcount; sub++ ) {
                    Texture2D tex = mats[sub].GetTexture( "_MainTex" ) as Texture2D;
                    Color color = Color.white;
                    if ( mats[sub].HasProperty( "_Color" ) ) {
                        color = mats[sub].GetColor( "_Color" );
                        //color = mats[sub].GetColor( "_SpecColor" );
                    }

                    if ( ! tex ) {
                        Log( $"No texture on {mesh.name}({sub})" );
                        // FIXME: should just trace with solid color
                        continue;
                        //tex = mats[0].GetTexture( "_MainTex" ) as Texture2D;
                        //if ( ! tex ) {
                        //    Error( $"No texture on {mesh.name}({sub})" );
                        //    continue;
                        //}
                    }

                    if ( tex && ! tex.isReadable ) {
                        Error( $"Couldn't sample texture {tex}, it's read-only" );
                        continue;
                    }
#if false
                    if ( ! tex.isReadable ) {
                        var origTexPath = AssetDatabase.GetAssetPath( tex );
                        ti = ( TextureImporter )AssetImporter.GetAtPath( texPath );
                        ti.isReadable = true;
                        ti.SaveAndReimport();
                        if ( ! tex.isReadable ) {
                            continue;
                        }
                    }
#endif
                    mesh.GetTriangles( _tris, sub );
                    int n = _tris.Count;
                    numTrianglesProcessed += n / 3;
                    for ( int i = 0; i < n; i += 3 ) {
                        GetTriangle( voxelSize, rend.transform, i, _tris, _verts, gridMinf, 
                                                    localRendMin, localRendMax,
                                                    out Vector3 a, out Vector3 b, out Vector3 c );
                        Vector2 aUV = _uvs[_tris[i + 0]];
                        Vector2 bUV = _uvs[_tris[i + 1]];
                        Vector2 cUV = _uvs[_tris[i + 2]];
                        numBoxesCreated += TraceTriangleWithColor( gridSz, a, b, c, aUV, bUV, cUV,
                                                                        grid, colors, tex, color );
                    }
                }
            }
        }
        return true;
    }

    private static void FloodFill(TVox [] slice, int width, int height, Vector2Int pt)
    {
        Stack<Vector2Int> pixels = new Stack<Vector2Int>();
        pixels.Push(pt);

        while (pixels.Count > 0)
        {
            Vector2Int a = pixels.Pop();
            if (a.x < width && a.x >= 0 && a.y < height && a.y >= 0)//make sure we stay within bounds
            {
                int idx = a.x + a.y * width;
                if (IsHollow(slice[idx]))
                {
                    TurnSolid(ref slice[idx]);
                    pixels.Push(new Vector2Int(a.x - 1, a.y));
                    pixels.Push(new Vector2Int(a.x + 1, a.y));
                    pixels.Push(new Vector2Int(a.x, a.y - 1));
                    pixels.Push(new Vector2Int(a.x, a.y + 1));
                }
            }
        }
    }

    // FIXME: this is broken
    // FIXME: implement span flood fill someday
    //private static void FloodFill(TVox [] slice, int width, int height, Vector2Int pt)
    //{
    //    Func<int, int, int> getPixel = (x, y) => { return slice[x + y * width]; };
    //    Action<int, int, int> setPixel = (x, y, val) => { slice[x + y * width] = val; };

    //    int targetVal = 0;
    //    int replacementVal = 1;

    //    Stack<Vector2Int> pixels = new Stack<Vector2Int>();

    //    pixels.Push(pt);
    //    while (pixels.Count != 0)
    //    {
    //        Vector2Int temp = pixels.Pop();
    //        int y1 = temp.y;
    //        while (y1 >= 0 && getPixel(temp.x, y1) == targetVal)
    //        {
    //            y1--;
    //        }
    //        y1++;
    //        bool spanLeft = false;
    //        bool spanRight = false;
    //        while (y1 < height && getPixel(temp.x, y1) == targetVal)
    //        {
    //            setPixel(temp.x, y1, replacementVal);

    //            if (!spanLeft && temp.x > 0 && getPixel(temp.x - 1, y1) == targetVal)
    //            {
    //                pixels.Push(new Vector2Int(temp.x - 1, y1));
    //                spanLeft = true;
    //            }
    //            else if(spanLeft && temp.x - 1 == 0 && getPixel(temp.x - 1, y1) != targetVal)
    //            {
    //                spanLeft = false;
    //            }
    //            if (!spanRight && temp.x < width - 1 && getPixel(temp.x + 1, y1) == targetVal)
    //            {
    //                pixels.Push(new Vector2Int(temp.x + 1, y1));
    //                spanRight = true;
    //            }
    //            else if (spanRight && temp.x < width - 1 && getPixel(temp.x + 1, y1) != targetVal)
    //            {
    //                spanRight = false;
    //            } 
    //            y1++;
    //        }

    //    }
    //}

    private static bool InsertAndFill(Renderer rend, float voxelSize, Vector3Int gridMin,
                                        Vector3Int gridMax, TVox [] grid, 
                                        out int numTrianglesProcessed, out int numBoxesCreated)
    {
        if (Insert(rend, voxelSize, gridMin, gridMax, grid, out numTrianglesProcessed, out numBoxesCreated)) {
            Vector3Int rendMin, rendMax;
            Vector3Int gridSz = MinMaxToSize( gridMin, gridMax );
            BoundsInGrid( rend.bounds.min, rend.bounds.max, gridMin, gridSz, voxelSize,
                                                                        out rendMin, out rendMax );

            Vector3Int castMin = rendMin - Vector3Int.one;
            Vector3Int castMax = rendMax + Vector3Int.one;

            int castW = castMax.x - castMin.x + 1;
            int castH = castMax.y - castMin.y + 1;

            TVox [] slice = new TVox [castW * castH];
            Vector3Int vi = Vector3Int.zero;
            for(vi.z = rendMin.z; vi.z <= rendMax.z; vi.z++) {
                // clear the bands at the edges of the slice
                for(int i = 0; i < castW; i++) {
                    slice[i] = 0;
                    slice[i + (castH - 1) * castW] = 0;
                }
                for(int i = 0; i < castH; i++) {
                    slice[i * castW] = 0;
                    slice[(castW - 1) + i * castW] = 0;
                }

                // copy from grid in slice center
                for(vi.y = rendMin.y; vi.y <= rendMax.y; vi.y++) {
                    for(vi.x = rendMin.x; vi.x <= rendMax.x; vi.x++) {
                        int sliceX = vi.x - rendMin.x + 1;
                        int sliceY = vi.y - rendMin.y + 1;
                        int sliceIdx = sliceX + sliceY * castW;
                        // FIXME: each renderer should be cast in a separate voxel grid before being merged in the world?
                        slice[sliceIdx] = grid[GridIdx(vi, gridSz)];
                    }
                }

                FloodFill(slice, castW, castH, Vector2Int.zero);

                // copy from slice back to grid inverted
                for(vi.y = castMin.y; vi.y <= castMax.y; vi.y++) {
                    for(vi.x = castMin.x; vi.x <= castMax.x; vi.x++) {
                        int sliceIdx = (vi.x - castMin.x) + (vi.y - castMin.y) * castW;
                        if (IsHollow(slice[sliceIdx])) {
                            int gridIdx = GridIdx(vi, gridSz);
                            if (IsHollow(grid[gridIdx])) {
                                TurnSolid(ref grid[gridIdx]);
                                numBoxesCreated++;
                            }
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
    
    public static Vector3 GetVoxCenter(Vector3Int vi, Vector3Int gridOrigin)
    {
        return (vi + gridOrigin) + VoxelHalf;
    }

    private static GameObject CreateVoxColliderGO(Vector3Int vi, Vector3Int gridOrigin, float voxelSize)
    {
        GameObject vox = new GameObject();
#if UNITY_EDITOR
        vox.name = "Vox";
#endif
        // don't touch the scale, messes up the physics
        vox.transform.transform.localScale = new Vector3(1,1,1);
        vox.transform.position = GetVoxCenter(vi, gridOrigin) * voxelSize;
        return vox;
    }

    public static SphereCollider CreateVoxColliderSphere(Vector3Int vi, Vector3Int gridOrigin, float voxelSize)
    {
        GameObject vox = CreateVoxColliderGO(vi, gridOrigin, voxelSize);
        var sph = vox.AddComponent<SphereCollider>();
        sph.radius = voxelSize / 2.01f;// / 1.55f;
        return sph;
    }

    public static Vector3 GetColliderSizeBox(float voxelSize)
    {
        return new Vector3(voxelSize, voxelSize, voxelSize) * 0.75f;
    }

    public static BoxCollider CreateVoxColliderBox(Vector3Int vi, Vector3Int gridOrigin, float voxelSize)
    {
        GameObject vox = CreateVoxColliderGO(vi, gridOrigin, voxelSize);
        var box = vox.AddComponent<BoxCollider>();
        box.size = GetColliderSizeBox(voxelSize);
        return box;
    }

    public static void ReparentVoxCollider(Transform parent, Collider collider)//, float voxelSize)
    {
        collider.transform.parent = parent;
        // FIXME: leaving this on leads to 30% perf drop :(
        // FIXME: we should not have scales
        //collider.transform.transform.localScale = new Vector3(1,1,1);
        //// ugh, please don't use non uniform scales
        //collider.radius = voxelSize / 2.01f / parent.lossyScale.x; 
    }

    public static GameObject CreateCube(Vector3Int vi, float voxelSize, Transform parent = null, 
                                            bool keepCollider = false, bool keepRenderer = true)
    {
        Vector3 voxelCenter = vi + VoxelHalf;
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.parent = parent;
        cube.transform.localPosition = voxelCenter * voxelSize;
        cube.transform.localScale = Vector3.one * voxelSize;
        if (! keepCollider) {
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Collider>());
        }
        if (! keepRenderer) {
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<Renderer>());
            UnityEngine.Object.DestroyImmediate(cube.GetComponent<MeshFilter>());
        }
        return cube;
    }

    public static Vector3Int MinMaxToSize(Vector3Int min, Vector3Int max)
    {
        return new Vector3Int( max.x - min.x + 1,
                                max.y - min.y + 1,
                                max.z - min.z + 1 );
    }

    public static void CreateCubes( float voxelSize, Vector3Int gridMin, Vector3Int gridMax,
                                                        TVox [] grid, bool keepColliders = false) {
        var root = new GameObject().transform;
        root.name = "Cubes";
        root.transform.position = (Vector3)gridMin * voxelSize;
        //var mpb = new MaterialPropertyBlock();
        Vector3Int gridSz = MinMaxToSize(gridMin, gridMax);
        Vector3Int vi = Vector3Int.zero;
        for(vi.z = 0; vi.z < gridSz.z; vi.z++) {
            for(vi.y = 0; vi.y < gridSz.y; vi.y++) {
                for(vi.x = 0; vi.x < gridSz.x; vi.x++) {
                    int idx = vi.x + vi.y * gridSz.x + vi.z * gridSz.x * gridSz.y;
                    TVox vxl = grid[idx];
                    if ( IsSolid( vxl ) ) {
                        CreateCube( vi, voxelSize, root, keepColliders );
                        //var c = CreateCube(vi, voxelSize, root, keepColliders);
                        //mpb.SetColor("_Color", Color.white);
                        //c.GetComponent<Renderer>().SetPropertyBlock(mpb);
                    }
                }
            }
        }
    }

    public static bool CreateTexture3DMono( float voxelSize, Vector3Int gridMin, Vector3Int gridMax,
                                                                TVox [] grid, out Texture3D tex ) {
        Vector3Int gridSz = MinMaxToSize( gridMin, gridMax );
        tex = new Texture3D( gridSz.x, gridSz.y, gridSz.z, TextureFormat.ARGB32, mipChain: false );
        if ( tex == null ) {
            return false;
        }

        Vector3Int vi = Vector3Int.zero;
        for(vi.z = 0; vi.z < gridSz.z; vi.z++) {
            for(vi.y = 0; vi.y < gridSz.y; vi.y++) {
                for(vi.x = 0; vi.x < gridSz.x; vi.x++) {
                    int idx = vi.x + vi.y * gridSz.x + vi.z * gridSz.x * gridSz.y;
                    TVox vxl = grid[idx];
                    if ( IsSolid( vxl ) ) {
                        tex.SetPixel( vi.x, vi.y, vi.z, Color.white );
                    } else {
                        tex.SetPixel( vi.x, vi.y, vi.z, Color.clear );
                    }
                }
            }
        }
        tex.Apply();
        Log( $"Created Mono texture 3d: {tex}", tex );

        return true;
    }

    public static bool CreateTexture3DColor( float voxelSize, Vector3Int gridMin,
                                        Vector3Int gridMax, Color32 [] colors, out Texture3D tex ) {
        Vector3Int gridSz = MinMaxToSize( gridMin, gridMax );
        tex = new Texture3D( gridSz.x, gridSz.y, gridSz.z, TextureFormat.ARGB32, mipChain: false );
        if ( tex == null ) {
            return false;
        }
        tex.SetPixels32( colors, 0 );
        tex.Apply();
        Log( $"Created Color texture 3d: {tex}", tex );

        return true;
    }

    private static bool TraceShellAndFill(MeshRenderer [] rends, float voxelSize, out TVox [] grid, 
                                            out Vector3Int gridMin, out Vector3Int gridMax, 
                                            out int numTrianglesProcessed, out int numFilledBoxes)
    {
        numFilledBoxes = 0;
        numTrianglesProcessed = 0;
        if (AllocateGrid(rends, voxelSize, out gridMin, out gridMax, out grid)) {
            foreach (var r in rends) {
                int ntp, nbc;
                InsertAndFill(r, voxelSize, gridMin, gridMax, grid, out ntp, out nbc);
                numTrianglesProcessed += ntp;
                numFilledBoxes += nbc;
            }
            return true;
        }
        return false;
    }

    public static bool TraceShell( Renderer [] rends, float voxelSize, out TVox [] grid,
                                                    out Vector3Int gridMin, out Vector3Int gridMax,
                                                    out int numTrianglesProcessed,
                                                    out int numBoxesFilled ) {
        numBoxesFilled = 0;
        numTrianglesProcessed = 0;
        if ( AllocateGrid(rends, voxelSize, out gridMin, out gridMax, out grid)) {
            foreach (var r in rends) {
                int ntp, nbc;
                Insert(r, voxelSize, gridMin, gridMax, grid, out ntp, out nbc);
                numTrianglesProcessed += ntp;
                numBoxesFilled += nbc;
            }
            return true;
        }
        return false;
    }

    public static bool TraceShellWithColor( Renderer [] rends, float voxelSize,
                                                    out TVox [] grid, out Color32 [] colors,
                                                    out Vector3Int gridMin, out Vector3Int gridMax,
                                                    out int numTrianglesProcessed,
                                                    out int numBoxesFilled ) {
        numBoxesFilled = 0;
        numTrianglesProcessed = 0;
        if ( AllocateGrid(rends, voxelSize, out gridMin, out gridMax, out grid)) {
            colors = AllocateColors( grid );
            foreach (var r in rends) {
                int ntp, nbc;
                InsertWithColor( r, voxelSize, gridMin, gridMax, grid, colors, out ntp, out nbc );
                numTrianglesProcessed += ntp;
                numBoxesFilled += nbc;
            }
            Log( "Traced shell into colors." );
            return true;
        }
        Log( "Failed to allocate grid." );
        colors = null;
        return false;
    }

    private static bool HasNaN(Vector3 v)
    {
        return float.IsNaN(v.x)
                || float.IsNaN(v.y)
                || float.IsNaN(v.z);
    }

    public static MeshRenderer [] GetRenderers(Transform root)
    {
        MeshRenderer [] rs = root.GetComponentsInChildren<MeshRenderer>();
        Validate(rs);
        return rs;
    }

    private static void Validate(MeshRenderer [] rends)
    {
        foreach(var r in rends) {
            Validate(r);
        }
    }

    private static bool Validate(MeshRenderer rend)
    {
        MeshFilter [] filters = rend.GetComponents<MeshFilter>();
        if (filters.Length == 0) {
            Error("Boxify: Renderer has no mesh filters " + rend, rend);
            return false;
        }
        foreach (var f in filters) {
            Mesh mesh = f.sharedMesh;
            if (mesh) {
                var tris = mesh.triangles;
                int n = tris.Length;
                Vector3 [] verts = mesh.vertices;
                for (int i = 0; i < n; i += 3) {
                    for (int j = 0; j < 3; j++) {
                        int idx = tris[i + j];
                        Vector3 v = verts[idx];
                        if (HasNaN(v)) {
                            Error("Boxify: There is a NaN vertex in " + rend + " at index " + idx, rend);
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

#if UNITY_EDITOR
    public static MeshRenderer [] GetRenderers()
    {
        return GetRenderers(Selection.activeTransform);
    }

    public static bool BoxifyGeometry( Transform root, float voxelSize, bool fill,
                                                            bool createVisuals, out TVox [] grid ) {
        var t = Time.realtimeSinceStartup;
        Vector3Int min, max;
        int numBoxesFilled = 0;
        int numTrianglesProcessed = 0;
        bool result;
        if (fill) {
            result = TraceShellAndFill( GetRenderers(root), voxelSize, out grid, out min, out max, 
                                                    out numTrianglesProcessed, out numBoxesFilled );
        } else {
            result = TraceShell( GetRenderers(root), voxelSize, out grid, out min, out max, 
                                                    out numTrianglesProcessed, out numBoxesFilled );
        }
        if (result) {
            float total = Time.realtimeSinceStartup - t;
            Log("Boxify took " + total * 1000 + " milliseconds.");
            Log("Num triangles processed: " + numTrianglesProcessed);
            Log("Num boxes lit: " + numBoxesFilled);
            if (createVisuals) {
                CreateCubes(voxelSize, min, max, grid);
            }
        } else {
            Log("There were errors.");
        }
        return result;
    }

    private static void BoxifySelection(bool fill, bool createVisuals)
    {
        TVox [] grid;
        BoxifyGeometry(root: Selection.activeTransform, voxelSize: 1f, fill: fill, 
                        createVisuals: createVisuals, out grid);
    }

    [MenuItem("Snapshot/Boxify/Boxify Selection")]
    public static void BoxifySelection()
    {
        BoxifySelection(fill: true, createVisuals: true);
    }

    [MenuItem("Snapshot/Boxify/Boxify Selection Shell")]
    public static void BoxifySelectionShell()
    {
        BoxifySelection(fill: false, createVisuals: true);
    }

    [MenuItem("Snapshot/Boxify/Boxify Selection Shell (No Visuals)")]
    public static void BoxifySelectionShellNoVisuals()
    {
        BoxifySelection(fill: false, createVisuals: false);
    }

    [MenuItem("Snapshot/Boxify/Boxify Selection", true)]
    [MenuItem("Snapshot/Boxify/Boxify Selection Shell", true)]
    public static bool CheckSelectionIsValid()
    {
        return Selection.activeTransform != null;
    }
#endif
}

#endif
