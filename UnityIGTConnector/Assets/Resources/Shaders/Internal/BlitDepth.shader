Shader "DualDepthPeeling/Internal/BlitDepth"
{
	SubShader
	{
    Cull Off
    ZWrite On
    ZTest Off

    Pass
    {
      CGPROGRAM
      #pragma vertex vert
      #pragma fragment CopyDepthBufferFragmentShader
      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
      };

      struct v2f
      {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
      };

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
      }

      sampler2D _CameraDepthTexture;

      half4 CopyDepthBufferFragmentShader(v2f i, out float outDepth : SV_Depth) : SV_Target
      {
        float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
        outDepth = depth;
        return 0;
      }

      ENDCG
    }
	}
}
