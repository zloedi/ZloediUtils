using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System;

// this module supplies procedurally generated geometry (lines, extruded 3dsurfaces, etc.)

public static class Tesselator {
    private static readonly Vector2 [] _hexAxes = {
        new Vector2( 1.000000f, 0.000000f ),
        new Vector2( 0.500000f, 0.866025f ),
        new Vector2( -0.500000f, 0.866025f ),
        new Vector2( -1.000000f, 0.000000f ),
        new Vector2( -0.500000f, -0.866025f ),
        new Vector2( 0.500000f, -0.866025f ),
    };

    private static Vector2[] GetHexRange12th( int range, int hexRadius ) {
        Vector2 [] contourVectors = {
            new Vector2( -0.866025f * hexRadius, 0.500000f * hexRadius ),
            new Vector2( 0 * hexRadius, 1 * hexRadius ),
        };

        float offsetScale = ( range * 2 + 1 ) * ( 0.866025f * hexRadius );
        Vector2 offset = new Vector2( 1 * offsetScale, 0 * offsetScale );

        int numSplinePoints = range + 1;
        Vector2 [] spline = new Vector2[numSplinePoints];
        Vector2 d = new Vector2( contourVectors[1].x * 0.5f, contourVectors[1].y * 0.5f );
        spline[0] = offset + d;

        for ( int i = 1; i < numSplinePoints; i++ ) {
            int flip = ( i + 1 ) & 1;
            spline[i] = spline[i - 1] + contourVectors[flip];
        }

        return spline;
    }

    //private static Vector2 SnapToHexAxis( int axisIndex, Vector2 vector ) {
    //    Vector2 axis = _hexAxes[axisIndex];
    //    Vector2 result = axis * vector.x + axis.Perpendicular() * vector.y;
    //    return result;
    //}

    private static Vector2 MirrorAroundHexAxis( int axisIndex, Vector2 vector ) {
        Vector2 axis = _hexAxes[axisIndex];
        Vector2 v = axis * Vector2.Dot( vector, axis );
        Vector2 result = v * 2 - vector;
        return result;
    }

    //// hex (map space) range outline in local space
    //public static Vector2[] GetHexRangeOutline( int range, int hexRadius ) {
    //    Vector2 [] hex12th = GetHexRange12th( range, hexRadius );
    //    int numPointsIn12th = hex12th.Length;

    //    const int numSlices = 6;

    //    // - 1 so slice start vertex is not duplicated
    //    int numPointsInSlice = numPointsIn12th * 2 - 1;
    //    Vector2 [] spline = new Vector2[numSlices * numPointsInSlice];

    //    for ( int i = 0; i < numSlices; i++ ) {
    //        int baseIdx = i * numPointsInSlice + ( numPointsIn12th - 1 );

    //        for ( int j = 0; j < numPointsIn12th; j++ ) {
    //            spline[baseIdx + j] = SnapToHexAxis( i, hex12th[j] );
    //        }

    //        for ( int j = 0; j < numPointsIn12th - 1; j++ ) {
    //            int idxS = baseIdx + j;
    //            int idxD = baseIdx - ( j + 1 );
    //            spline[idxD] = MirrorAroundHexAxis( i, spline[idxS] );
    //        }
    //    }

    //    return spline;
    //}

    public static void DrawSplineDebug( Vector3 [] spline, GameObject ob, float lineWidth ) {
        LineRenderer lineRenderer = ob.GetComponent<LineRenderer>();
        if ( lineRenderer == null ) {
            lineRenderer = ob.AddComponent<LineRenderer>();
        }
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = spline.Length;
        for ( int i = 0; i < spline.Length; i++ ) {
            lineRenderer.SetPosition( i, spline[i] );
        }
    }

    // for debug purposes only
    public static void DrawSphere( Vector3 center, float radius, int numVerts ) {
        float angularStep = Mathf.PI * 2.0f / numVerts;
        for ( int i = 0; i < 3; i++ ) {
            Vector3 [] spline = new Vector3[numVerts + 1];
            for ( int j = 0; j < numVerts + 1; j++ ) {
                Vector3 v = new Vector3();
                float angle = j * angularStep;
                v[( i + 0 ) % 3] = Mathf.Cos( angle );
                v[( i + 1 ) % 3] = Mathf.Sin( angle );
                v[( i + 2 ) % 3] = 0.0f;
                spline[j] = center + v * radius;
            }
            GameObject ob = new GameObject();
            DrawSplineDebug( spline, ob, 0.1f );
        }
    }

    private static void AssignVertsForSection( int i, 
            int numVertsPerSide, 
            float splineTextureCoord,
            Vector2 sectionBox,
            Vector3 localPoint, 
            Vector3 edge, 
            Vector3 splineSideNormal, 
            ref Vector3 [] verts, 
            ref Vector3 [] normals,
            ref Vector2 [] uvs ) {
        Vector3 ns = splineSideNormal * sectionBox.x;
        Vector3 lp = localPoint + ns * 0.5f;

        // right top and bottom
        Vector3 v0 = lp + edge * sectionBox.y;
        Vector3 v1 = lp;
        Vector2 uvRight = new Vector2( splineTextureCoord, 0 );

        // left
        Vector3 v2 = v0 - ns;
        Vector3 v3 = v1 - ns;
        Vector2 uvLeft = new Vector2( splineTextureCoord, 1 );

        int idx;
        int i2 = i * 2;

        // multiple copies of the verts with different normals

        idx = numVertsPerSide * 0 + i2;
        verts  [idx + 0] = v0;
        verts  [idx + 1] = v1;
        normals[idx + 0] = splineSideNormal;
        normals[idx + 1] = splineSideNormal;
        uvs    [idx + 0] = uvRight;
        uvs    [idx + 1] = uvRight;

        idx = numVertsPerSide * 1 + i2;
        verts  [idx + 0] = v2;
        verts  [idx + 1] = v3;
        normals[idx + 0] = -splineSideNormal;
        normals[idx + 1] = -splineSideNormal;
        uvs    [idx + 0] = uvLeft;
        uvs    [idx + 1] = uvLeft;

        idx = numVertsPerSide * 2 + i2;
        verts  [idx + 0] = v0;
        verts  [idx + 1] = v1;
        normals[idx + 0] = edge;
        normals[idx + 1] = -edge;
        uvs    [idx + 0] = uvRight;
        uvs    [idx + 1] = uvRight;

        idx = numVertsPerSide * 3 + i2;
        verts  [idx + 0] = v2;
        verts  [idx + 1] = v3;
        normals[idx + 0] = edge;
        normals[idx + 1] = -edge;
        uvs    [idx + 0] = uvLeft;
        uvs    [idx + 1] = uvLeft;
    }

    //private static void ExtrudeBoxAlongParable( float   height, 
    //        Vector3 parableStart, 
    //        Vector3 parableEnd, 
    //        Vector2 sectionBox, 
    //        int     numSplinePoints,
    //        float   uScale,

    //        out Vector3 [] spline,
    //        out Vector3 [] verts, 
    //        out Vector3 [] normals, 
    //        out Vector2 [] uvs, 
    //        out int     [] indices ) {
    //    spline = Utl.ParableToSpline( height, parableStart, parableEnd, numSplinePoints );
    //    ExtrudeBoxAlongSpline( sectionBox, 
    //            uScale,
    //            spline,
    //            ExtrusionType.Arc,
    //            out verts, 
    //            out normals, 
    //            out uvs, 
    //            out indices );
    //}

    private static int [] GenerateIndicesForSurface( int numSplinePoints, bool ccw = false ) {
        int lastSurfIndex = numSplinePoints - 1;
        int numIndicesPerSide = numSplinePoints * 6;
        int [] indices = new int[numIndicesPerSide * 4];
        int numVertsPerSide = numSplinePoints * 2;

        if ( ccw ) {
            for ( int i = 0; i < lastSurfIndex; i++ ) {
                int sidx, didx;
                int i6 = i * 6;
                int i2 = i * 2;

                sidx = numIndicesPerSide * 0 + i6;
                didx = numVertsPerSide * 0 + i2;
                indices[sidx + 0] = didx + 0;
                indices[sidx + 1] = didx + 1;
                indices[sidx + 2] = didx + 2;
                indices[sidx + 3] = didx + 1;
                indices[sidx + 4] = didx + 3;
                indices[sidx + 5] = didx + 2;

                sidx = numIndicesPerSide * 1 + i6;
                didx = numVertsPerSide * 1 + i2;
                indices[sidx + 0] = didx + 0;
                indices[sidx + 1] = didx + 2;
                indices[sidx + 2] = didx + 1;
                indices[sidx + 3] = didx + 1;
                indices[sidx + 4] = didx + 2;
                indices[sidx + 5] = didx + 3;

                int didx0 = numVertsPerSide * 2 + i2;
                int didx1 = numVertsPerSide * 3 + i2;

                sidx = numIndicesPerSide * 2 + i6;
                indices[sidx + 0] = didx0 + 0;
                indices[sidx + 1] = didx0 + 2;
                indices[sidx + 2] = didx1 + 0;
                indices[sidx + 3] = didx1 + 0;
                indices[sidx + 4] = didx0 + 2;
                indices[sidx + 5] = didx1 + 2;

                sidx = numIndicesPerSide * 3 + i6;
                indices[sidx + 0] = didx0 + 3;
                indices[sidx + 1] = didx0 + 1;
                indices[sidx + 2] = didx1 + 3;
                indices[sidx + 3] = didx1 + 3;
                indices[sidx + 4] = didx0 + 1;
                indices[sidx + 5] = didx1 + 1;
            }
        } else {
            for ( int i = 0; i < lastSurfIndex; i++ ) {
                int sidx, didx;
                int i6 = i * 6;
                int i2 = i * 2;

                sidx = numIndicesPerSide * 0 + i6;
                didx = numVertsPerSide * 0 + i2;
                indices[sidx + 5] = didx + 0;
                indices[sidx + 4] = didx + 1;
                indices[sidx + 3] = didx + 2;
                indices[sidx + 2] = didx + 1;
                indices[sidx + 1] = didx + 3;
                indices[sidx + 0] = didx + 2;

                sidx = numIndicesPerSide * 1 + i6;
                didx = numVertsPerSide * 1 + i2;
                indices[sidx + 5] = didx + 0;
                indices[sidx + 4] = didx + 2;
                indices[sidx + 3] = didx + 1;
                indices[sidx + 2] = didx + 1;
                indices[sidx + 1] = didx + 2;
                indices[sidx + 0] = didx + 3;

                int didx0 = numVertsPerSide * 2 + i2;
                int didx1 = numVertsPerSide * 3 + i2;

                sidx = numIndicesPerSide * 2 + i6;
                indices[sidx + 5] = didx0 + 0;
                indices[sidx + 4] = didx0 + 2;
                indices[sidx + 3] = didx1 + 0;
                indices[sidx + 2] = didx1 + 0;
                indices[sidx + 1] = didx0 + 2;
                indices[sidx + 0] = didx1 + 2;

                sidx = numIndicesPerSide * 3 + i6;
                indices[sidx + 5] = didx0 + 3;
                indices[sidx + 4] = didx0 + 1;
                indices[sidx + 3] = didx1 + 3;
                indices[sidx + 2] = didx1 + 3;
                indices[sidx + 1] = didx0 + 1;
                indices[sidx + 0] = didx1 + 1;
            }
        }
        return indices;
    }

    // returns distance of this segment
    private static float GetExtrudeSection( Vector3 [] spline,
            int current, 
            int next, 
            out Vector3 normal, 
            out Vector3 binormal ) {
        Vector3 nextPoint = spline[next];
        Vector3 thisPoint = spline[current];
        Vector3 d = nextPoint - thisPoint;
        normal = Vector3.Cross( d, Vector3.up ).normalized;

        if (normal == Vector3.zero) {
            normal = Vector3.Cross(d, Vector3.forward).normalized;
        }

        binormal = Vector3.Cross( normal, d ).normalized;
        return d.magnitude;
    }

    private static float GenerateVertsRegular( Vector2    sectionBox,
            Vector3 [] spline,

            out Vector3 [] verts, 
            out Vector3 [] normals, 
            out Vector2 [] uvs ) { 
        int numVertsPerSide = spline.Length * 2;
        int numVerts = numVertsPerSide * 4;
        verts = new Vector3[numVerts];
        normals = new Vector3[numVerts];
        uvs = new Vector2[numVerts];

        float distanceAlongSpline = 0;

        Vector3 normal = Vector3.zero;
        Vector3 binormal = Vector3.zero;

        for ( int i = 0; i < spline.Length - 1; i++ ) {
            float segmentLength = GetExtrudeSection( spline, i + 1, i, out normal, out binormal );
            AssignVertsForSection( i, 
                    numVertsPerSide, 
                    distanceAlongSpline,
                    sectionBox, 
                    spline[i] - spline[0], 
                    binormal, 
                    normal,
                    ref verts, 
                    ref normals,
                    ref uvs );
            distanceAlongSpline += segmentLength;
        }

        // reuse last calculated values for the cap
        AssignVertsForSection( spline.Length - 1, 
                numVertsPerSide, 
                distanceAlongSpline,
                sectionBox, 
                spline[spline.Length - 1] - spline[0], 
                binormal, 
                normal, 
                ref verts, 
                ref normals,
                ref uvs );

        return distanceAlongSpline;
    }

    // generates verts for an extrusion of a box along looped spline
    // returns spline length
    private static float GenerateVertsLooped( Vector2    sectionBox,
            Vector3 [] spline,

            out Vector3 [] verts, 
            out Vector3 [] normals, 
            out Vector2 [] uvs ) { 
        Vector3 firstPoint = spline[0];

        int numVertsPerSide = spline.Length * 2;
        int numVerts = numVertsPerSide * 4;
        verts = new Vector3[numVerts];
        normals = new Vector3[numVerts];
        uvs = new Vector2[numVerts];

        float distanceAlongSpline = 0;

        Vector3 normal;
        Vector3 binormal;

        for ( int i = 0; i < spline.Length - 1; i++ ) {
            float segmentLength = GetExtrudeSection( spline, i + 1, i, out normal, out binormal );
            AssignVertsForSection( i, 
                    numVertsPerSide, 
                    distanceAlongSpline,
                    sectionBox, 
                    spline[i] - firstPoint, 
                    binormal, 
                    normal,
                    ref verts, 
                    ref normals,
                    ref uvs );
            distanceAlongSpline += segmentLength;
        }

        GetExtrudeSection( spline, 1, 0, out normal, out binormal );
        AssignVertsForSection( spline.Length - 1, 
                numVertsPerSide, 
                distanceAlongSpline,
                sectionBox, 
                Vector3.zero, 
                binormal, 
                normal,
                ref verts, 
                ref normals,
                ref uvs );

        return distanceAlongSpline;
    }

    // extrudes a box along an arc spline
    // returns spline length
    private static float GenerateVertsArc( Vector2    sectionBox,
            Vector3 [] spline,

            out Vector3 [] verts, 
            out Vector3 [] normals, 
            out Vector2 [] uvs ) { 
        Vector3 firstPoint = spline[0];
        Vector3 splineDirection = spline[1] - firstPoint;
        Vector3 sideNormal = Vector3.Cross( splineDirection, Vector3.up ).normalized;

        Vector3 upNormal = Vector3.zero;

        int numVertsPerSide = spline.Length * 2;
        int numVerts = numVertsPerSide * 4;
        verts = new Vector3[numVerts];
        normals = new Vector3[numVerts];
        uvs = new Vector2[numVerts];

        // TODO: move the spline in local coords, so local is not calculated each iteration
        int lastSurfIndex = spline.Length - 1;
        Vector3 localPoint = Vector3.zero;
        Vector3 thisPoint = firstPoint;

        float distanceAlongSpline = 0;

        for ( int i = 0; i < lastSurfIndex; i++ ) {
            Vector3 nextPoint = spline[i + 1];

            Vector3 splineSegment = nextPoint - thisPoint;
            upNormal = Vector3.Cross( sideNormal, splineSegment ).normalized;

            AssignVertsForSection( i, 
                    numVertsPerSide, 
                    distanceAlongSpline,
                    sectionBox, 
                    localPoint, 
                    upNormal, 
                    sideNormal, 
                    ref verts, 
                    ref normals,
                    ref uvs );

            thisPoint = nextPoint;
            localPoint = thisPoint - firstPoint;
            distanceAlongSpline += splineSegment.magnitude;
        }

        // reuse last calculated values for the cap
        AssignVertsForSection( lastSurfIndex, 
                numVertsPerSide, 
                distanceAlongSpline,
                sectionBox, 
                localPoint, 
                upNormal, 
                sideNormal, 
                ref verts, 
                ref normals,
                ref uvs );

        return distanceAlongSpline;
    }

    public static void ExtrudeBoxAlongSplineInMesh( Mesh mesh, Vector2 sectionBox, float uScale, Vector3 [] spline, bool looped ) {
        Vector3 [] verts;
        Vector3 [] normals; 
        Vector2 [] uvs;
        int [] indices;
        ExtrudeBoxAlongSpline( sectionBox, uScale, spline, looped ? ExtrusionType.Looped : ExtrusionType.Regular, out verts, out normals, out uvs, out indices );
        mesh.Clear();
        mesh.vertices = verts;
        mesh.normals = normals;
        mesh.uv = uvs; 
        mesh.triangles = indices;
    }

    public enum ExtrusionType {
        Regular,
        Looped,
        Arc,
    }

    // genereates either a closed (looped) or arc surface by extruding a rectangle along a spline 
    // by default the u texture coordinate is in the [0,1] range, so the texture stretches when the spline length changes
    // pass uScale != 0 to override it and set uniform tiled texture along u coordinate
    public static void ExtrudeBoxAlongSpline( Vector2    sectionBox, 
            float      uScale,
            Vector3 [] spline,
            ExtrusionType et,

            out Vector3 [] verts, 
            out Vector3 [] normals, 
            out Vector2 [] uvs, 
            out int     [] indices ) 
    {
        int numSurfPoints = spline.Length;
        if ( numSurfPoints < 2 ) {
            verts = null;
            normals = null;
            uvs = null;
            indices = null;
            return;
        }

        float distanceAlongSpline;

        if ( et == ExtrusionType.Looped ) {
            distanceAlongSpline = GenerateVertsLooped( sectionBox,
                    spline,
                    out verts, 
                    out normals, 
                    out uvs );
        } else if ( et == ExtrusionType.Arc ) {
            distanceAlongSpline = GenerateVertsArc( sectionBox,
                    spline,
                    out verts, 
                    out normals, 
                    out uvs );
        } else {
            distanceAlongSpline = GenerateVertsRegular( sectionBox,
                    spline,
                    out verts, 
                    out normals, 
                    out uvs );
        }

        // this is not done inside AssignVertsForSection, because we need distanceAlongSpline

        // either normalize or simply scale
        float scale = uScale != 0.0f ? 1.0f / uScale : 1.0f / distanceAlongSpline;
        for ( int i = 0; i < uvs.Length; i++ ) {
            uvs[i].x *= scale;
        }

        indices = GenerateIndicesForSurface( spline.Length );
    }

    static void Test_cmd( string [] argv ) {
        GameObject go = new GameObject( "TesselatorTest" );
        go.transform.position = Vector3.zero;
        go.AddComponent<MeshRenderer>();
        MeshFilter mf = go.AddComponent<MeshFilter>();
        Mesh m;
        mf.mesh = m = new Mesh();
        m.name = "TesselatorTest";
        Vector3 [] points = new Vector3[] {
            new Vector3( 0, 0, 0 ),
            new Vector3( 0, 1, 0 ),
            new Vector3( 1, 1, 0 ),
            new Vector3( 0, 1, 1 ),
        };
        ExtrudeBoxAlongSplineInMesh( m, Vector2.one * 0.25f, 1, points, false );
    }

    //// generates geometry by extruding a box along a parable and returns a Mesh object
    //// by default the u texture coordinate is in the [0,1] range, so the texture stretches when the spline length changes
    //// pass uScale != 0 to override it and set uniform tiled texture along u coordinate
    //public static void ExtrudeBoxAlongParableInMesh( Mesh mesh, float height, Vector3 parableStart, Vector3 parableEnd, Vector2 sectionBox, int numSplinePoints, float uScale, out Vector3 [] spline ) 
    //{
    //    Vector3 [] verts;
    //    Vector3 [] normals;
    //    Vector2 [] uvs;
    //    int [] indices;
    //    ExtrudeBoxAlongParable( height, 
    //            parableStart, 
    //            parableEnd, 
    //            sectionBox, 
    //            numSplinePoints,
    //            uScale,
    //            out spline,
    //            out verts,
    //            out normals,
    //            out uvs, 
    //            out indices );
    //    mesh.Clear();
    //    mesh.vertices = verts;
    //    mesh.normals = normals;
    //    mesh.uv = uvs; 
    //    mesh.triangles = indices;
    //}

    //public static void Line2d( GameObject go, Material material, Vector3 [] points, float thickness, Color color, int sortingOrder = 0 ) 
    //{
    //    if ( points.Length == 0 ) {
    //        return;
    //    }
    //    Vector3 [] verts = new Vector3[points.Length * 4];
    //    //Color [] colors = new Color[points.Length * 4];
    //    int [] indices = new int[points.Length * 6];
    //    Vector3 start = points[0];
    //    for ( int i = 0; i < points.Length - 1; i++ ) {
    //        Vector3 p0 = points[i] - start;
    //        Vector3 p1 = points[i + 1] - start;
    //        Vector3 n = Vector3.ClampMagnitude( ( p1 - p0 ).Perpendicular(), thickness * 0.5f );
    //        int i4 = i * 4;
    //        int i6 = i * 6;
    //        verts[i4 + 0] = p0 + n;
    //        verts[i4 + 1] = p0 - n;
    //        verts[i4 + 2] = p1 + n;
    //        verts[i4 + 3] = p1 - n;
    //        //colors[i4 + 0] = color;
    //        //colors[i4 + 1] = color;
    //        //colors[i4 + 2] = color;
    //        //colors[i4 + 3] = color;
    //        indices[i6 + 0] = i4 + 0;
    //        indices[i6 + 1] = i4 + 1;
    //        indices[i6 + 2] = i4 + 2;
    //        indices[i6 + 3] = i4 + 1;
    //        indices[i6 + 4] = i4 + 3;
    //        indices[i6 + 5] = i4 + 2;
    //    }
    //    MeshFilter mf = go.GetComponent<MeshFilter>();
    //    if ( mf == null ) {
    //        mf = go.AddComponent<MeshFilter>();
    //    }
    //    mf.mesh.Clear();
    //    mf.mesh.vertices = verts;
    //    //mf.mesh.colors = colors;
    //    mf.mesh.triangles = indices;
    //    MeshRenderer mr = go.GetComponent<MeshRenderer>();
    //    if ( mr == null ) {
    //        mr = go.AddComponent<MeshRenderer>();
    //    }
    //    mr.receiveShadows = false;
    //    mr.shadowCastingMode = ShadowCastingMode.Off;
    //    mr.lightProbeUsage = LightProbeUsage.Off;
    //    mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
    //    mr.material = material;
    //    mr.sortingOrder = sortingOrder;
    //    mr.material.color = color;
    //    go.transform.position = start;
    //}
}
