Shader "DualDepthPeeling/Internal/BlendBackBuffer"
{
  SubShader
  {
    Tags { "RenderType" = "Translucent" }

    Pass
    {
      BlendOp Add
      Blend One OneMinusSrcAlpha
      ZTest Off
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
        float2 uv     : TEXCOORD0;
        float4 vertex : SV_POSITION;
      };

      sampler2D newPeel;

      v2f vert(appdata v)
      {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
      }

      fixed4 frag(v2f i) : SV_Target
      {
        fixed4 f = tex2D(newPeel, i.uv);
        if (f.a == 0)
        {
          discard;
        }
        return f;
      }

      ENDCG
    }
  }
}
