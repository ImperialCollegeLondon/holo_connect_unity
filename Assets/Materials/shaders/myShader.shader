// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'glstate.matrix.mvp' with 'UNITY_MATRIX_MVP'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/myShader"
{
	SubShader
	{
	Tags{ "RenderType" = "Opaque" }
		Pass{
		CGPROGRAM

#pragma vertex vert
#pragma fragment frag


		struct appdata {
		float4 vertex : POSITION;

	};

	struct v2f {
		float4 vertex : SV_POSITION;
		float3 worldPos : TEXCOORD0;
	};



	v2f vert(appdata v) {
		v2f o;

		o.worldPos = mul(unity_ObjectToWorld, v.vertex);
		o.vertex = UnityObjectToClipPos(v.vertex);

		return o;
	}

	fixed4 frag(v2f i) : COLOR{
   if (i.worldPos.y > -1.4) {
	return float4(0,0,0,1);
	}
   else {
	   return float4(0.2,0.2,0,1);
}
	}

		ENDCG
	}


	}
		FallBack "Diffuse"
}