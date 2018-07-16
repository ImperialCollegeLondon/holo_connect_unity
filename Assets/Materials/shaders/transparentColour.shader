Shader "Unlit/unlitTransparentScalable"
{
	Properties
	{
		_color("color", Color) = (0,0,0,1) 
	_Transparency("Transparency", Range(0.0,1.0)) = 0.25

	}
		SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 1
		//ZWrite Off
		Blend  SrcAlpha OneMinusSrcAlpha
		Pass
	{
		CGPROGRAM
#pragma vertex vert
#pragma fragment frag
		// make fog work
#pragma multi_compile_fog

#include "UnityCG.cginc"

		struct appdata
	{
		float4 vertex : POSITION;
	};

	struct v2f
	{
		UNITY_FOG_COORDS(1)
			float4 vertex : SV_POSITION;
	};

	float4 _MainTex_ST;
	float _Transparency;
	float4 _color;

	v2f vert(appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		//UNITY_TRANSFER_FOG(o,o.vertex);
		return o;
	}

	fixed4 frag(v2f i) : SV_Target
	{
		// sample the texture
		fixed4 col = _color;
	// apply fog
	//UNITY_APPLY_FOG(i.fogCoord, col);
	col.a *= _Transparency;
	return col;
	}
		ENDCG
	}
	}
}
