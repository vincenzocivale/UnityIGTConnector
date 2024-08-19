Shader "DualDepthPeeling/Internal/FinalImageBlending"
{

  SubShader
  {
    Tags { "RenderType" = "Translucent" }

    Pass
    {
      BlendOp Add
      Blend One OneMinusSrcAlpha
      ZTest On
      ZWrite Off
      Cull Off

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
        float2 uv      : TEXCOORD0;
        float4 vertex  : SV_POSITION;
      };

      sampler2D frontTexture;
      sampler2D backTexture;

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        fixed4 front = tex2D(frontTexture, i.uv);
        fixed4 back = tex2D(backTexture, i.uv);
        front.a = 1.0 - front.a; // stored as (1 - alpha)
        float4 finalColor = float4(0,0,0,0);
        // Underblend. Back color is premultiplied:\n"
        finalColor.rgb = (front.rgb + back.rgb * front.a);
        // The first '1. - ...' is to convert the 'underblend' alpha to
        // an 'overblend' alpha, since we'll be letting GL do the
        // transparent-over-opaque blending pass.
        finalColor.a = (1.0 - front.a * (1.0 - back.a));
        return finalColor;
      }

      ENDCG
    }
  }
}
