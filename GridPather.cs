using System;
using System.Collections.Generic;

public static class GridPather {


public const int FREE = ( int )0x0fffffff;
public const int BLOC = ( int )0x40000000;

static int Clamp( int a, int min, int max ) {
    return Math.Min( max, Math.Max( a, min ) );
}

// front size should be power of two, use this for getting a nice front buffer
static int [] CreateFront( int navMapSize ) {
    // front size picked totally arbitrary
    int c = Math.Max( navMapSize / 4, 1 );

    // compute the next highest power of 2 of 32-bit c
    c--;
    c |= c >> 1;
    c |= c >> 2;
    c |= c >> 4;
    c |= c >> 8;
    c |= c >> 16;
    c++;

    return new int[c];
}

static int [] CreateFloodMap( int navMapSize ) {
    return new int[navMapSize];
}

// == PUBLIC API ==

public class Context {
    // internal state
    public int head;
    public int tail;
    public int [] front;

    // the origin of the flood
    public int origin;

    // the filled map, passed to i.e. trace path
    public int [] floodMap;

    public int diagNumCrossedNodes;
}

// ! silently overflows !
public static void Push( Context ctx, int coord ) {
    ctx.front[ctx.tail & ( ctx.front.Length - 1 )] = coord;
    ctx.tail++;
    ctx.diagNumCrossedNodes++;
}

public static int PopRand( Context ctx, int rand ) {
    rand = (ctx.head + rand % (ctx.tail - ctx.head)) & ( ctx.front.Length - 1 );
    int result = ctx.front[rand];
    ctx.front[rand] = ctx.front[ctx.head & ( ctx.front.Length - 1 )];
    ctx.head++;
    return result;
}

public static int PopBack( Context ctx ) {
    int coord = ctx.front[(ctx.tail - 1) & ( ctx.front.Length - 1 )];
    ctx.tail--;
    return coord;
}

public static int Pop( Context ctx ) {
    int coord = ctx.front[ctx.head & ( ctx.front.Length - 1 )];
    ctx.head++;
    return coord;
}

public static void Reset( Context ctx, int origin ) {
    ctx.origin = origin;
    ctx.head = 0;
    ctx.tail = 1;
    ctx.front[ctx.head] = origin;

    ctx.diagNumCrossedNodes = 0;
}

public static bool TryExpand( Context ctx, int nbr, int newScore ) {
    if ( ( ctx.floodMap[nbr] & ~BLOC ) > newScore ) {
        ctx.floodMap[nbr] = newScore;
        Push( ctx, nbr );
        return true;
    }
    return false;
}

public static bool FrontIsEmpty( Context ctx ) {
    return ctx.head == ctx.tail;
}

public static Context CreateContext( int navMapSize ) {
    return new Context {
        front = CreateFront( navMapSize ),
        floodMap = CreateFloodMap( navMapSize ),
    };
}

// keep them ordered
public static int [] QuadPrims(int pitch) => new [] { -1, -pitch, 1, pitch };

// before being able to trace paths, you need to flood the map
// reuse the context for tracing multiple paths
public static bool FloodHexMap( int origin, int maxRange, int navMapPitch,
                                                byte [] navMap, int navMapLength, Context ctx ) {
    origin = Clamp( origin, 0, navMapLength - 1 );
    if ( navMap[origin] != 0 ) {
        return false;
    }
    int [] prims = {
         -1,
         -navMapPitch,
         1 - navMapPitch,
         1,
         navMapPitch,
         -1 + navMapPitch,
    };
    // clear the flood map, or more precisely, copy the nav there
    for ( int i = 0; i < navMapLength; i++ ) {
        ctx.floodMap[i] = navMap[i] != 0 ? BLOC : FREE;
    }
#if false
    for ( int i = navMapLength; i < ctx.floodMap.Length; i++ ) {
        ctx.floodMap[i] = BLOC;
    }
#else
    // add a row of blocking tiles under the last row
    int n = Math.Min( navMapLength + navMapPitch, ctx.floodMap.Length );
    for ( int i = navMapLength; i < n; i++ ) {
        ctx.floodMap[i] = BLOC;
    }
#endif
    int max = ctx.floodMap.Length - 1;
    ctx.floodMap[origin] = 0;
    Reset( ctx, origin );
    int [] nbrs = new int [6];
    do {
        int c = Pop( ctx );
        int headScore = ctx.floodMap[c];
        if ( headScore < maxRange ) {
            int newScore = headScore + 1;
            nbrs[0] = Clamp( c + prims[0], 0, max ); 
            nbrs[1] = Clamp( c + prims[1], 0, max ); 
            nbrs[2] = Clamp( c + prims[2], 0, max ); 
            nbrs[3] = Clamp( c + prims[3], 0, max );
            nbrs[4] = Clamp( c + prims[4], 0, max );
            nbrs[5] = Clamp( c + prims[5], 0, max );
            TryExpand( ctx, nbrs[0], newScore );
            TryExpand( ctx, nbrs[1], newScore );
            TryExpand( ctx, nbrs[2], newScore );
            TryExpand( ctx, nbrs[3], newScore );
            TryExpand( ctx, nbrs[4], newScore );
            TryExpand( ctx, nbrs[5], newScore );
        }
    } while ( ! FrontIsEmpty( ctx ) );

    return true;
}

// call this to get a path between two nodes
// origin is already stored in context by Flood
public static bool TraceHexPath( int target, int floodMapPitch, Context ctx, List<int> ioResult ) { 
    ioResult.Clear();
    target = Clamp( target, 0, ctx.floodMap.Length - 1 );
    if ( ctx.floodMap[target] == BLOC 
            || ctx.floodMap[target] == FREE 
            || ctx.floodMap[ctx.origin] == BLOC ) {
        return false;
    }
    int [] prims = {
         -1,
         -floodMapPitch,
         1 - floodMapPitch,
         1,
         floodMapPitch,
         -1 + floodMapPitch,
    };
    // explictly push target, then start at 1
    ioResult.Add( target );
    int max = ctx.floodMap.Length - 1;
    while ( true ) {
        int [] neighbours = {
            Clamp( target + prims[0], 0, max ),
            Clamp( target + prims[1], 0, max ),
            Clamp( target + prims[2], 0, max ),
            Clamp( target + prims[3], 0, max ),
            Clamp( target + prims[4], 0, max ),
            Clamp( target + prims[5], 0, max ),
        };
        int [] floods = {
            ctx.floodMap[neighbours[0]],
            ctx.floodMap[neighbours[1]],
            ctx.floodMap[neighbours[2]],
            ctx.floodMap[neighbours[3]],
            ctx.floodMap[neighbours[4]],
            ctx.floodMap[neighbours[5]],
        };
        int [] scores = {
            ( int )( floods[0] & BLOC ) | ( ( floods[0] & 0x0fffffff ) << 3 ) | 0,
            ( int )( floods[1] & BLOC ) | ( ( floods[1] & 0x0fffffff ) << 3 ) | 1,
            ( int )( floods[2] & BLOC ) | ( ( floods[2] & 0x0fffffff ) << 3 ) | 2,
            ( int )( floods[3] & BLOC ) | ( ( floods[3] & 0x0fffffff ) << 3 ) | 3,
            ( int )( floods[4] & BLOC ) | ( ( floods[4] & 0x0fffffff ) << 3 ) | 4,
            ( int )( floods[5] & BLOC ) | ( ( floods[5] & 0x0fffffff ) << 3 ) | 5,
        };
        int min = Math.Min( scores[5],
                        Math.Min( scores[4],
                            Math.Min( scores[3],
                                Math.Min( scores[2],
                                    Math.Min( scores[1],
                                                scores[0] ) ) ) ) );
        if ( ( min >> 3 ) >= ctx.floodMap[target] ) {
            break;
        }
        target = neighbours[min & 7];
        ioResult.Add( target );
    }
    return target == ctx.origin;
}

public static bool FloodQuadMap( int origin, int maxRange, int navMapPitch,
                                                byte [] navMap, int navMapLength, Context ctx ) {
    origin = Clamp( origin, 0, navMapLength - 1 );
    if ( navMap[origin] != 0 ) {
        return false;
    }

    // keep them ordered
    int [] prims = { -1, -navMapPitch, 1, navMapPitch };

    // clear the flood map, or more precisely, copy the nav there
    for ( int i = 0; i < navMapLength; i++ ) {
        ctx.floodMap[i] = navMap[i] != 0 ? BLOC : FREE;
    }

#if false
    for ( int i = navMapLength; i < ctx.floodMap.Length; i++ ) {
        ctx.floodMap[i] = BLOC;
    }
#else
    // add a row of blocking tiles under the last row
    int n = Math.Min( navMapLength + navMapPitch, ctx.floodMap.Length );
    for ( int i = navMapLength; i < n; i++ ) {
        ctx.floodMap[i] = BLOC;
    }
#endif

    int max = ctx.floodMap.Length - 1;
    ctx.floodMap[origin] = 0;
    Reset( ctx, origin );
    int [] nbrs = new int [4];
    do {
        int c = Pop( ctx );
        int headScore = ctx.floodMap[c];
        if ( headScore < maxRange ) {
            int newScore = headScore + 1;
            nbrs[0] = Clamp( c + prims[0], 0, max ); 
            nbrs[1] = Clamp( c + prims[1], 0, max ); 
            nbrs[2] = Clamp( c + prims[2], 0, max ); 
            nbrs[3] = Clamp( c + prims[3], 0, max );
            TryExpand( ctx, nbrs[0], newScore );
            TryExpand( ctx, nbrs[1], newScore );
            TryExpand( ctx, nbrs[2], newScore );
            TryExpand( ctx, nbrs[3], newScore );
        }
    } while ( ! FrontIsEmpty( ctx ) );

    return true;
}

// call this to get a path between two nodes
// origin is already stored in context by Flood
public static bool TraceQuadPath( int target, int floodMapPitch, Context ctx, List<int> ioResult ) { 
    ioResult.Clear();
    target = Clamp( target, 0, ctx.floodMap.Length - 1 );
    if ( ctx.floodMap[target] == BLOC 
            || ctx.floodMap[target] == FREE 
            || ctx.floodMap[ctx.origin] == BLOC ) {
        return false;
    }

    // keep them ordered
    int [] prims = { -1, -floodMapPitch, 1, floodMapPitch };

    // explictly push target, then start at 1
    ioResult.Add( target );
    int max = ctx.floodMap.Length - 1;
    while ( true ) {
        int [] neighbours = {
            Clamp( target + prims[0], 0, max ),
            Clamp( target + prims[1], 0, max ),
            Clamp( target + prims[2], 0, max ),
            Clamp( target + prims[3], 0, max ),
        };
        int [] floods = {
            ctx.floodMap[neighbours[0]],
            ctx.floodMap[neighbours[1]],
            ctx.floodMap[neighbours[2]],
            ctx.floodMap[neighbours[3]],
        };
        int [] scores = {
            ( int )( floods[0] & BLOC ) | ( ( floods[0] & 0x0fffffff ) << 2 ) | 0,
            ( int )( floods[1] & BLOC ) | ( ( floods[1] & 0x0fffffff ) << 2 ) | 1,
            ( int )( floods[2] & BLOC ) | ( ( floods[2] & 0x0fffffff ) << 2 ) | 2,
            ( int )( floods[3] & BLOC ) | ( ( floods[3] & 0x0fffffff ) << 2 ) | 3,
        };
        int min = Math.Min( scores[3],
                                Math.Min( scores[2],
                                    Math.Min( scores[1],
                                                scores[0] ) ) );
        if ( ( min >> 2 ) >= ctx.floodMap[target] ) {
            break;
        }
        target = neighbours[min & 3];
        ioResult.Add( target );
    }
    return target == ctx.origin;
}


}
