Shader "DualDepthPeeling/Internal/InitializeDepth"
{
  SubShader
  {
    Pass
    {    
      BlendOp Max
      ZTest Off
      ZWrite Off
      Cull Off

      GLSLPROGRAM
      #include "UnityCG.glslinc"

      uniform sampler2D opaqueDepth;

      #ifdef VERTEX
      void main()
      {
        gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;
      }
      #endif

      #ifdef FRAGMENT
      void main()
      {
        // current depth
        gl_FragDepth = gl_FragCoord.z;

        // opaque depth
        ivec2 pixel = ivec2(gl_FragCoord.xy);
        float oDepth = texelFetch(opaqueDepth, pixel, 0).y;

        if (oDepth != -1. && gl_FragDepth > oDepth)
        {
          // Ignore fragments that are occluded by opaque geometry:
          gl_FragData[1].xy = vec2(-1., oDepth);
          return;
        }
        else
        {
          gl_FragData[1].xy = vec2(-gl_FragDepth, gl_FragDepth);
          return;
        }
      }
      #endif

      ENDGLSL
    }
  }
}
