#if UNITY_STANDALONE || UNITY_2021_0_OR_NEWER


using UnityEngine;
using System.Globalization;

// widgets to be drawn inside the Qonsole log
public static class QonWidgets {


static int _colorPickerHandle;
static readonly uint [] bmpHUE = {
	0xff0000ff, // red
	0xff00ffff, // magenta
	0x0000ffff, // blue
	0x00ffffff, // cyan
	0x00ff00ff, // green
	0xffff00ff, // yellow
	0xff0000ff, // red
};
static readonly uint[] bmpSV = {
	0xffffffff, 0xff0000ff,
	0x000000ff, 0x000000ff,
};
public static void ColorPicker_kmd( string [] argv ) {
	Texture2D colorPickerSV = null;
	Texture2D colorPickerHUE = null;
	Color toColor( uint u ) {
		byte r = ( byte )( ( u >> 24 ) & 0xff );
		byte g = ( byte )( ( u >> 16 ) & 0xff );
		byte b = ( byte )( ( u >> 8 ) & 0xff );
		return new Color32( r, g, b, 0xff );
	}
	colorPickerHUE = new Texture2D( 1, 7 );
	for ( int i = 0; i < bmpHUE.Length; i++ ) {
		colorPickerHUE.SetPixel( 0, i, toColor( bmpHUE[i % 6] ) );
	}
	colorPickerHUE.Apply();
	colorPickerSV = new Texture2D( 2, 2 );
	for ( int j = 0; j < 2; j++ ) {
		for ( int i = 0; i < 2; i++ ) {
			colorPickerSV.SetPixel( i, j, toColor( bmpSV[i + j * 2] ) );
		}
	}
	colorPickerSV.Apply();
	float size = 0;
	if ( argv.Length > 1 ) {
        size = Cellophane.AtoF( argv[1] );
	}
	if ( size == 0 ) {
		size = 256;
	}
	_colorPickerHandle++;
	int handle = _colorPickerHandle;

	string colorString = "";
	Color currentColor = Color.red;
	Color hueColor = currentColor;
	float hue = 0, saturation = 1, val = 0;

	Qonsole.PrintAndAct( "\n", ( screenPos, alpha ) => {
		Vector2 origin = screenPos + new Vector2( Qonsole.LineHeight(), Qonsole.LineHeight() );
		Vector2 mouseGlob = Event.current.mousePosition * QGL.pixelsPerPoint;
		Vector2 mouse = mouseGlob - origin;

#if false
        RGBToHSV( inputColor, out hue, out saturation, out val );
#endif

		QGL.SetTexture( colorPickerSV );
		GL.Color( Color.white );
		GL.Begin( GL.QUADS );
		QGL.DrawQuad( origin, Vector2.one * size, srcOrigin: Vector2.one * 0.25f,
																	srcSize: Vector2.one * 0.5f );
		GL.End();

		QUI.WidgetResult result;

		void updateSV() {
            var sv = new Vector2( saturation, val );
            var isv = new Vector2( 1 - sv.x, 1 - sv.y );
            var c0 = isv.x * isv.y * toColor( bmpSV[0] );
            var c1 = sv.x * isv.y * hueColor;
            var c2 = isv.x * sv.y * toColor( bmpSV[2] );
            var c3 = sv.x * sv.y * toColor( bmpSV[3] );
			var c = c0 + c1 + c2 + c3;
			if ( c != currentColor ) {
				currentColor = c;
                uint ir = ( uint )( currentColor.r * 255.999 );
                uint ig = ( uint )( currentColor.g * 255.999 );
                uint ib = ( uint )( currentColor.b * 255.999 );
				uint rgb = ir << 16 | ig << 8 | ib << 0;
				string r = currentColor.r.ToString( "F2", CultureInfo.InvariantCulture );
				string g = currentColor.g.ToString( "F2", CultureInfo.InvariantCulture );
				string b = currentColor.b.ToString( "F2", CultureInfo.InvariantCulture );
				colorString = $"Color({r}, {g}, {b})\n";
				colorString += $"Color32({ir}, {ig}, {ib})\n";
				colorString += $"#{rgb.ToString( "X6" )}";
				GUIUtility.systemCopyBuffer = colorString;
			}
		}

		result = QUI.ClickRect( origin.x, origin.y, size, size, handle );
		if ( result == QUI.WidgetResult.Pressed || result == QUI.WidgetResult.Released
															|| result == QUI.WidgetResult.Active ) {
            saturation = Mathf.Clamp( mouse.x / size, 0, 1 );
            val = Mathf.Clamp( mouse.y / size, 0, 1 );
			updateSV();
		}

		float gap = size / 20f;
		Vector2 hueOrigin = origin + new Vector2( size + gap, 0 );
		Vector2 hueSize = new Vector2( size / 6f, size );

		result = QUI.ClickRect( hueOrigin.x, hueOrigin.y, hueSize.x, hueSize.y, handle );
		if ( result == QUI.WidgetResult.Released || result == QUI.WidgetResult.Active ) {
			hue = Mathf.Clamp( mouse.y / size * 6, 0, 6 );
			int i0 = Mathf.Min( ( int )hue, 5 );
			int i1 = ( i0 + 1 ) % 6;
			Color c0 = toColor( bmpHUE[i0] );
			Color c1 = toColor( bmpHUE[i1] );
			hueColor = Color.Lerp( c0, c1, hue - i0 );
			updateSV();
			colorPickerSV.SetPixel( 1, 0, hueColor );
			colorPickerSV.Apply();
		}

		QGL.SetTexture( colorPickerHUE );
		GL.Color( Color.white );
		GL.Begin( GL.QUADS );
		QGL.DrawQuad( hueOrigin, hueSize, srcOrigin: new Vector2( 0, 1f / 14f ),
														srcSize: new Vector2( 1, 1 - 1f / 7f ) );
		GL.End();

		QGL.SetTexture( null );
		GL.Begin( GL.QUADS );
		GL.Color( currentColor );
		Vector2 sampleSize = new Vector2( size, size / 2 );
		Vector2 sampleOrigin = hueOrigin + new Vector2( hueSize.x + gap, 0 );
		QGL.DrawQuad( sampleOrigin, sampleSize );
		GL.End();

		QGL.SetFontTexture();
		GL.Begin( GL.QUADS  );
		float py = hueOrigin.y + hue / 6 * size - CodePage437.CharSz / 2;
		QGL.DrawScreenCharWithOutline( '>', hueOrigin.x - CodePage437.CharSz, py, Color.white, 1 );
		QGL.DrawScreenCharWithOutline( '<', hueOrigin.x + hueSize.x, py, Color.white, 1 );
		QGL.DrawScreenCharWithOutline( '+', origin.x + saturation * size - CodePage437.CharSz / 2,
											origin.y + val * size - CodePage437.CharSz / 2,
																				Color.white, 1 );
		QGL.DrawTextWithOutline( colorString + "\n\nCopied to the Clipboard on change.",
								sampleOrigin.x, sampleOrigin.y + sampleSize.y + gap, Color.white );
		GL.End();
	} );
	int numLines = 1 + ( int )( size / Qonsole.LineHeight() );
	for ( int i = 0; i < numLines; i++ ) {
		Qonsole.Print( "\n" );
	}
}


}


#endif // UNITY_STANDALONE
