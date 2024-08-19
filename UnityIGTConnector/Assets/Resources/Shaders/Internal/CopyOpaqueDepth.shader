Shader "DualDepthPeeling/Internal/CopyOpaqueDepth"
{
  SubShader
  {
    Pass
    {
      CGPROGRAM
      #pragma vertex   vert
      #pragma fragment frag
      #include "UnityCG.cginc"

      struct appdata
      {
        float4 vertex : POSITION;
        float2 uv     : TEXCOORD0;
        float4 normal : NORMAL;
      };

      struct v2f
      {
        float2 uv     : TEXCOORD0;
        float4 vertex : SV_POSITION;
      };

      sampler2D _CameraDepthTexture;
      uniform float clearValue;

      v2f vert (appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv     = v.uv;
        return o;
      }

      fixed4 frag (v2f i) : SV_Target
      {
        float depth = tex2D(_CameraDepthTexture, i.uv).r;
        if (depth == clearValue)
        {
          // If no depth value has been written, discard the frag:
          discard;
        }
        return fixed4(-1, depth, 0, 0);
      }

      ENDCG
    }
  }
}
