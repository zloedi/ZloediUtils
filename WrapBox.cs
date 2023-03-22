using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public struct WrapBox {
    // FIXME: this matches the y value of CanvasScaler.referenceResolution
    // FIXME: should be either read from canvas scaler or enforced there
    // FIXME: ultimately we may want always scale, ignoring the canvas resolution
    // Canvases only scale stuff DOWN
    public static float canvasScale => Mathf.Min( 1f, ( float )Screen.height / 1080 );

    public static float ScaleRound( float f ) {
        return Mathf.Round( Scale( f ) );
    }

    public static float Scale( float f ) {
        return f * canvasScale;
    }

    public float x, y, w, h;
    public float W => w / canvasScale;
    public float H => h / canvasScale;

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
}
