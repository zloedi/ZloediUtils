#if UNITY_STANDALONE || UNITY_2021_1_OR_NEWER || SDL

using System.Collections.Generic;
using System.Runtime.CompilerServices;

#if SDL
using GalliumMath;
using SDLPorts;
#else
using UnityEngine;
#endif

public struct WrapBox {
    const float DefaultMinRes = 1080;

    // scale stuff down, if resolution is below this value
    static float _canvasMinResolution = DefaultMinRes;

    // use fixed scale if canvas scale is disabled
    static float _fixedScale = 1;

    // FIXME: this matches the y value of CanvasScaler.referenceResolution
    // FIXME: should be either read from canvas scaler or enforced there
    // FIXME: ultimately we may want always scale, ignoring the canvas resolution
    // Canvases only scale stuff DOWN
    public static float canvasScale =>
                            _canvasMinResolution == 0
                                ? _fixedScale
                                : Mathf.Min( 1f,
                                    ( float )Mathf.Min( Screen.width, Screen.height )
                                        / _canvasMinResolution );

    public static float ScaleRound( float f ) {
        return Mathf.Round( Scale( f ) );
    }

    public static float Scale( float f ) {
        return f * canvasScale;
    }

    public static float Unscale( float f ) {
        return f / canvasScale;
    }

    public static void DisableCanvasScale( float fixedScale = 1 ) {
        _fixedScale = fixedScale;
        _canvasMinResolution = 0f;
    }

    public static void EnableCanvasScale( float minRes = 0 ) {
        _canvasMinResolution = minRes != 0 ? minRes : DefaultMinRes;
    }

    public float x, y, w, h;
    public float W => w / canvasScale;
    public float H => h / canvasScale;
    public Vector2 midPoint => new Vector2( x + w / 2, y + h / 2 );

    // id will remain constant 23, unless id is passed to the anchoring api-s
    // if a valid id is specified, it is propagated to all 'children' wboxes
    // useful as a handle in the IMM widgets
    public int id;

    public int GetId( int id ) {
        return id == int.MaxValue ? this.id : 31 * this.id + id;
    }

    public WrapBox( float x, float y, float w, float h, int id = 0 ) {
        this.x = Mathf.Round( x );
        this.y = Mathf.Round( y );
        this.w = Mathf.Round( w );
        this.h = Mathf.Round( h );
        this.id = id == 0 ? 23 : id;
    } 

    public WrapBox Center( float inW, float inH, float x = 0, float y = 0, int id = int.MaxValue ) {
        inW *= canvasScale;
        inH *= canvasScale;
        float inX = this.x + ( this.w - inW ) * 0.5f + Scale( x );
        float inY = this.y + ( this.h - inH ) * 0.5f + Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    public WrapBox TopCenter( float inW, float inH, float y = 0, int id = int.MaxValue ) {
        inW *= canvasScale;
        inH *= canvasScale;
        float inX = this.x + ( this.w - inW ) * 0.5f;
        float inY = this.y + Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }
    
    public WrapBox TopRight( float inW, float inH, float x = 0, float y = 0,
                                                                        int id = int.MaxValue ) {
        inW *= canvasScale;
        inH *= canvasScale;
        float inX = this.x + this.w - inW - Scale( x );
        float inY = this.y + Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    public WrapBox TopLeft( float inW, float inH, float x = 0, float y = 0,
                                                                        int id = int.MaxValue ) {
        inW = Scale( inW );
        inH = Scale( inH );
        float inX = this.x + Scale( x );
        float inY = this.y + Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    public WrapBox CenterLeft( float inW, float inH, float x = 0, float y = 0,
                                                                        int id = int.MaxValue ) {
        inW = Scale( inW );
        inH = Scale( inH );
        float inX = this.x + Scale( x );
        float inY = this.y + ( this.h - inH ) * 0.5f + Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    public WrapBox CenterRight( float inW, float inH, float x = 0, float y = 0,
                                                                        int id = int.MaxValue ) {
        inW = Scale( inW );
        inH = Scale( inH );
        float inX = this.x + this.w - inW - Scale( x );
        float inY = this.y + ( this.h - inH ) * 0.5f + Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    public WrapBox BottomLeft( float inW, float inH, float x = 0, float y = 0,
                                                                        int id = int.MaxValue ) {
        inW *= canvasScale;
        inH *= canvasScale;
        float inX = this.x + Scale( x );
        float inY = this.y + this.h - inH - Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    public WrapBox BottomRight( float inW, float inH, float x = 0, float y = 0,
                                                                        int id = int.MaxValue ) {
        inW *= canvasScale;
        inH *= canvasScale;
        float inX = this.x + this.w - inW - Scale( x );
        float inY = this.y + this.h - inH - Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    public WrapBox BottomCenter( float inW, float inH, float y = 0, int id = int.MaxValue ) {
        inW *= canvasScale;
        inH *= canvasScale;
        float inX = this.x + ( this.w - inW ) * 0.5f;
        float inY = this.y + this.h - inH - Scale( y );
        return new WrapBox( inX, inY, inW, inH, GetId( id ) );
    }

    // moves the box below the current one
    public WrapBox NextDown() {
        return TopLeft( W, H, y: H );
    }

    public WrapBox NextUp() {
        return TopLeft( W, H, y: -H );
    }

    public WrapBox NextRight( int id = int.MaxValue ) {
        return TopLeft( W, H, x: W, id: id );
    }

    public WrapBox NextLeft() {
        return TopLeft( W, H, x: -W );
    }

    // make a copy then move down
    public WrapBox CopyMoveDown( int i ) {
        var copy = new WrapBox( x, y, w, h, GetId( i ) );
        y += h;
        return copy;
    }


    // example: WrapBox wb = wbox.CopyMoveUp( row ).Center( wbox.W * 0.9f, wbox.H * 0.9f );
    public WrapBox CopyMoveUp( int i ) {
        var copy = new WrapBox( x, y, w, h, GetId( i ) );
        y -= h;
        return copy;
    }

    public WrapBox CopyMoveLeft( int i ) {
        var copy = new WrapBox( x, y, w, h, GetId( i ) );
        x -= w;
        return copy;
    }

    public WrapBox CopyMoveRight( int i ) {
        var copy = new WrapBox( x, y, w, h, GetId( i ) );
        x += w;
        return copy;
    }

    // eat top from this box, generate a box from the difference, return modified box
    public WrapBox EatTop( int eatH, out WrapBox wboxDifference ) {
        wboxDifference = TopLeft( W, eatH );
        return EatTop( wboxDifference );
    }

    public WrapBox EatTop( WrapBox wbox ) {
        return BottomLeft( W, H - wbox.H );
    }
}

#endif
