Shader "XRay Shaders/AbstractXRayObstruct" {

Properties {
	_Stencil ("Stencil ID", Float) = 0
}

SubShader {
	Tags {"Queue"="Overlay" "RenderType"="Opaque" }

	Pass {
		ZWrite Off
		ZTest LEqual
		Cull Back
		Stencil {
			Ref [_Stencil]
			Comp always
			Pass replace
		}
		ColorMask 0
	}
}

}
