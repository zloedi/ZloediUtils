Shader "XRay Shaders/AbstractXRayHidden" {

Properties {
    _FillColor ("Fill Color", Color) = (.0, .3, .3)
	_Stencil ("Stencil ID", Float) = 0
}

SubShader {
	Tags {"Queue"="Overlay+100" "RenderType"="Opaque" }

	Pass {
		//ZWrite On
		ZTest Always
		//ZTest Lequal
		//ZTest Less
		//ZTest Equal
		//ZTest Greater
		Cull Back

		Stencil {
			Ref [_Stencil]
			Comp equal
			Pass keep
		}

		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#include "UnityCG.cginc"

			struct v2f {
				float4 vertex : POSITION;
			};

			v2f vert (v2f v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			float4 _FillColor;

            void frag(v2f v, out fixed4 res : SV_Target) {
				float b = sin(_Time.w);
				float normBlink = 0.5 * (b + 1);
#if 1
				float normInterlaceY = frac(v.vertex.y / 3.0);
				res = (0.2 + normInterlaceY) * (0.8 + normBlink * 0.5) * _FillColor;
#else
				float normInterlaceX = frac(v.vertex.x / 3.0);
                if ( normInterlaceX < 0.5f ) clip( -1.0 ); 
                if ( normInterlaceY < 0.5f ) clip( -1.0 ); 
                res = _FillColor;
#endif
            }
		ENDCG
	}
}

}
