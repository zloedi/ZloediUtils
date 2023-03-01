#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER

using System.Collections.Generic;
using System.Diagnostics;
using System;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

using TVox = System.Int32;

public static class Boxify
{
    public static readonly Vector3 VoxelHalf = Vector3.one * 0.5f;
 
    public class PerfTimerLogger : IDisposable
    {
        public PerfTimerLogger(string message, bool skipLogging = false, UnityEngine.Object obj = null, Action<float> onDispose = null)
        {
#if UNITY_EDITOR
            this._message = message;
            this._timer = new Stopwatch();
            this._timer.Start();
            this._skipLogging = skipLogging;
            this._obj = obj;
            this._onDispose = onDispose;
#endif
        }

        string _message;
        Stopwatch _timer;
        bool _skipLogging;
        UnityEngine.Object _obj;
        Action<float> _onDispose;
     
        public void Dispose()
        {
#if UNITY_EDITOR
            long nanosecPerTick = (1000L*1000L*1000L) / Stopwatch.Frequency;
            this._timer.Stop();
            long ns = this._timer.ElapsedTicks * nanosecPerTick;
            if (! _skipLogging) {
                Debug.Log(
                    string.Format("{0} - Elapsed Milliseconds: {1}", this._message, ns / 1000000.0f),
                    _obj
                );
            }
            if (_onDispose != null) {
                _onDispose(ns/1000000.0f);
            }
#endif
        }
    }

#if UNITY_EDITOR
    private static bool _enableUpdateCallbacks;
    private class Future {
        public float TriggerTime;
        public Action Action;
    }
    private static List<Future> _futures = new List<Future>();

    private static void OnUpdate()
    {
        if (! EditorApplication.isPlaying) {
            EditorApplication.update -= OnUpdate;
            _enableUpdateCallbacks = false;
        } else {
            for (int i = _futures.Count - 1; i >= 0; i--) {
                Future f = _futures[i];
                if (f.TriggerTime <= Time.time) {
                    Debug.Log("Execute future, Time: " + Time.time);
                    f.Action();
                    _futures.RemoveAt(i);
                }
            }
        }
    }

    public static void RegisterFuture(float delay, Action a)
    {
        if (! _enableUpdateCallbacks) {
            EditorApplication.update += OnUpdate;
            _enableUpdateCallbacks = true;
        }
        if (delay == 0) {
            a();
        } else {
            _futures.Add( new Future { TriggerTime = Time.time + delay, Action = a } );
        }
    }   
#endif

    public static float atof( string s )
    {
        float res;
        if (float.TryParse(s, out res)) {
            return res;
        }
        return 0;
    }

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

        //Debug.Log("Rend Min: " + minmaxScaled[0].ToString("F4"));
        //Debug.Log("Rend Max: " + minmaxScaled[1].ToString("F4"));

        Vector3Int [] result = new Vector3Int[2];

        //char [] xyz = {'x', 'y', 'z'};
        //string [] minmaxs = {"Min", "Max"};

        for (int i = 0; i < 2; i++) {
            Vector3 vRound = minmaxRounded[i];
            Vector3 vScale = minmaxScaled[i];
            //Debug.Log(minmaxs[i] + " Scale: " + vScale.ToString("F4"));
            //Debug.Log(minmaxs[i] + " Round: " + vRound.ToString("F4"));
            for (int j = 0; j < 3; j++) {
                float sj = vScale[j];
                float rj = vRound[j];
                float e = sj - rj;
                //Debug.Log("Error " + xyz[j] + ": " + Mathf.Abs(e));
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
        if (GetBoundsOfRenderers(rends, voxelSize, out min, out max)) {
            Debug.Log("Grid min: " + min);
            Debug.Log("Grid max: " + max);
            Vector3Int sz = MinMaxToSize(min, max);
            grid = new TVox [sz.x * sz.y * sz.z];
            Debug.Log("Allocated grid with size " + sz.x + "," + sz.y + "," + sz.z);
            return true;
        }
        Debug.Log("No renderers to boxify in selection.");
        grid = null;
        return false;
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

    private static bool IsSolid(TVox voxel)
    {
        return voxel != 0;
    }

    private static bool IsHollow(TVox voxel)
    {
        return ! IsSolid(voxel);
    }

    private static void TurnSolid(ref TVox voxel)
    {
        voxel = 1;
    }

    // FIXME: use in fracture too
    public static Vector3Int Vector3IntCast(Vector3 v)
    {
        return new Vector3Int((int)v.x, (int)v.y, (int)v.z);
    }

    // assumes local to grid a, b and c
    private static int TraceTriangle( Vector3Int gridSz, Vector3 a, Vector3 b, Vector3 c,
                                                                                    TVox [] grid ) {
        int numBoxesCreated = 0;

        float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
        float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
        float minZ = Mathf.Min(a.z, Mathf.Min(b.z, c.z));

        float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
        float maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
        float maxZ = Mathf.Max(a.z, Mathf.Max(b.z, c.z));

        Vector3Int min = Vector3Int.FloorToInt(new Vector3(minX, minY, minZ));
        Vector3Int max = Vector3Int.FloorToInt(new Vector3(maxX, maxY, maxZ));

        //BoundsInGrid( new Vector3( minX, minY, minZ ), new Vector3( maxX, maxY, maxZ ),
        //                                                   gridMin, voxelSize, out min, out max );

        min.x = Mathf.Clamp( min.x, 0, gridSz.x - 1 );
        min.y = Mathf.Clamp( min.y, 0, gridSz.y - 1 );
        min.z = Mathf.Clamp( min.z, 0, gridSz.z - 1 );

        max.x = Mathf.Clamp( max.x, 0, gridSz.x - 1 );
        max.y = Mathf.Clamp( max.y, 0, gridSz.y - 1 );
        max.z = Mathf.Clamp( max.z, 0, gridSz.z - 1 );

        //Qonsole.Log( "triangle min: " + min );
        //Qonsole.Log( "triangle max: " + max );
        //Qonsole.Log( "===" );

        Vector3Int vi = Vector3Int.zero;
        for(vi.z = min.z; vi.z <= max.z; vi.z++) {
            for(vi.y = min.y; vi.y <= max.y; vi.y++) {
                for(vi.x = min.x; vi.x <= max.x; vi.x++) {
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

    public static void GetTriangle(float voxelSize, Transform rendTransform, 
                                    int i, List<int> tris, 
                                    Vector3 [] verts, 
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

        foreach (var f in filters) {
            Mesh mesh = f.sharedMesh;
            if (mesh) {
                // FIXME: use mesh.gettriangles?
                List<int> tris = new List<int>(mesh.triangles);
                int n = tris.Count;
                numTrianglesProcessed += n / 3;
                Vector3 [] verts = mesh.vertices;
                for (int i = 0; i < n; i += 3) {
                    Vector3 a, b, c;
                    GetTriangle(voxelSize, rend.transform, i, tris, verts, gridMinf, 
                                    localRendMin, localRendMax, out a, out b, out c);
                    numBoxesCreated += TraceTriangle(gridSz, a, b, c, grid);
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
        return new Vector3Int(max.x - min.x + 1,
                                max.y - min.y + 1,
                                max.z - min.z + 1);
    }

    private static void CreateCubes(float voxelSize, Vector3Int gridMin, Vector3Int gridMax, TVox [] grid, 
                                            bool keepColliders = false)
    {
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
                    if (IsSolid(vxl)) {
                        CreateCube(vi, voxelSize, root, keepColliders);
                        //var c = CreateCube(vi, voxelSize, root, keepColliders);
                        //mpb.SetColor("_Color", Color.white);
                        //c.GetComponent<Renderer>().SetPropertyBlock(mpb);
                    }
                }
            }
        }
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

    private static bool TraceShell(Renderer [] rends, float voxelSize, out TVox [] grid, out Vector3Int gridMin, 
                                    out Vector3Int gridMax, out int numTrianglesProcessed, out int numBoxesFilled)
    {
        numBoxesFilled = 0;
        numTrianglesProcessed = 0;
        if (AllocateGrid(rends, voxelSize, out gridMin, out gridMax, out grid)) {
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

#if UNITY_EDITOR
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

    public static MeshRenderer [] GetRenderers()
    {
        return GetRenderers(Selection.activeTransform);
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
            Debug.LogError("Boxify: Renderer has no mesh filters " + rend, rend);
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
                            Debug.LogError("Boxify: There is a NaN vertex in " + rend + " at index " + idx, rend);
                            return false;
                        }
                    }
                }
            }
        }
        return true;
    }

    public static bool BoxifyGeometry(Transform root, float voxelSize, bool fill, bool createVisuals, 
                                                                                out TVox [] grid)
    {
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
            Debug.Log("Boxify took " + total * 1000 + " milliseconds.");
            Debug.Log("Num triangles processed: " + numTrianglesProcessed);
            Debug.Log("Num boxes lit: " + numBoxesFilled);
            if (createVisuals) {
                CreateCubes(voxelSize, min, max, grid);
            }
        } else {
            Debug.Log("There were errors.");
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
