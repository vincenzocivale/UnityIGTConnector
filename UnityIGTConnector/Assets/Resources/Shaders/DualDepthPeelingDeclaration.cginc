#ifndef DUAL_DEPTH_PEELING_DECL
#define DUAL_DEPTH_PEELING_DECL

/////////////////////////////////////////////////////////////////////////////
// Dual Depth Peeling Decl
/////////////////////////////////////////////////////////////////////////////

#pragma shader_feature_local LAST_PEEL

struct FragmentOutput
{
  fixed4 Color0 : SV_Target0;
#ifndef LAST_PEEL
  fixed4 Color1 : SV_Target1;
  fixed4 Color2 : SV_Target2;
#endif
};

UNITY_DECLARE_TEX2D(lastDepthPeel);
#ifndef LAST_PEEL
UNITY_DECLARE_TEX2D(lastFrontPeel);
float epsilon = 0.0000001;

struct FragmentDepth {
  float Depth;
  float Min;
  float Max;
};

bool DualDepthPeeling_PreColor(float4 vertexPos, inout FragmentDepth fragDepth, inout FragmentOutput o)
{
  float gl_FragDepth = vertexPos.z;
  int2 pixelCoord = int2(vertexPos.xy);
  float4 front = lastFrontPeel.Load(int3(pixelCoord, 0));
  float2 minMaxDepth = lastDepthPeel.Load(int3(pixelCoord, 0)).xy;
  fragDepth.Min = -minMaxDepth.x;
  fragDepth.Max = minMaxDepth.y;
  fragDepth.Depth = gl_FragDepth;

  // Default outputs (no data/change):  
  o.Color0 = fixed4(0, 0, 0, 0);
  o.Color1 = front;
  o.Color2 = fixed4(-1, -1, 0, 0);

  // Is this fragment outside the current peels?  
  if (gl_FragDepth < fragDepth.Min - epsilon ||
    gl_FragDepth > fragDepth.Max + epsilon)
  {
    return false;
  }

  // Is this fragment inside the current peels?  
  if (gl_FragDepth > fragDepth.Min + epsilon &&
    gl_FragDepth < fragDepth.Max - epsilon)
  {
    // Write out depth so this frag will be peeled later:  
    o.Color2.xy = float2(-gl_FragDepth, gl_FragDepth);
    return false;
  }

  return true;
}

void DualDepthPeeling_Peel(FragmentDepth fragDepth, inout FragmentOutput o)
{
  float4 frag = o.Color0;
  float4 front = o.Color1;

  // This fragment is on a current peel:
  if (fragDepth.Depth >= fragDepth.Min - epsilon &&
    fragDepth.Depth <= fragDepth.Min + epsilon)
  { // Front peel:
    // Clear the back color:
    o.Color0 = float4(0, 0, 0, 0);
    // We store the front alpha value as (1-alpha) to allow MAX
    // blending. This also means it is really initialized to 1,
    // as it should be for under-blending.
    front.a = 1. - front.a;
    // Use under-blending to combine fragment with front color:
    o.Color1.rgb = front.a * frag.a * frag.rgb + front.rgb;
    // Write out (1-alpha):
    o.Color1.a = 1. - (front.a * (1. - frag.a));
  }
  //ifndef NO_PRECOLOR_EARLY_RETURN just 'else' is ok. We'd return earlier in this case.
  else // (gl_FragDepth == maxDepth)
  { // Back peel:
    // Dump premultiplied fragment, it will be blended later:
    frag.rgb *= frag.a;
    o.Color0 = frag;
  }
}
#else
bool DualDepthPeeling_LastPreColor(float4 vertexPos)
{
  float gl_FragDepth = vertexPos.z;
  int2 pixelCoord = int2(vertexPos.xy);
  float2 minMaxDepth = lastDepthPeel.Load(int3(pixelCoord, 0)).xy;
  float minDepth = -minMaxDepth.x;
  float maxDepth = minMaxDepth.y;

  // Discard all fragments outside of the last set of peels
  if (gl_FragDepth < minDepth || gl_FragDepth > maxDepth)
  {
    return false;
  }

  return true;
}

void DualDepthPeeling_LastPeel(inout fixed4 color)
{
  color.rgb *= color.a;
}
#endif

// Helper macro to perform a dual depth peeling "precolor" step.
// 'vertexPosition': Input vertex position from the vertex sader.
// 'fragmentOutput': Output fragment of type FragmentOutput.
#ifndef LAST_PEEL                     
  #define DEPTH_PEELING_PRECOLOR(vertexPosition, fragmentOutput) \
    FragmentDepth fragDepth;                                     \
    if (!DualDepthPeeling_PreColor(vertexPosition, fragDepth, fragmentOutput)) \
    {                                                          \
      return output;\
    }
#else
  #define DEPTH_PEELING_PRECOLOR(vertexPosition, fragmentOutput) \
    if (!DualDepthPeeling_LastPreColor(vertexPosition))\
    {\
      discard;\
    }
#endif

// Helper macro to perform a dual depth peeling "peel" step.
// 'fragmentOutput': Output fragment of type FragmentOutput.
#ifndef LAST_PEEL
  #define DEPTH_PEELING_PEEL(fragmentOutput) \
    DualDepthPeeling_Peel(fragDepth, fragmentOutput);
#else
  #define DEPTH_PEELING_PEEL(fragmentOutput) \
    DualDepthPeeling_LastPeel(fragmentOutput.Color0);
#endif

#endif