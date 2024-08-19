// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

#ifndef UNITY_STANDARD_CORE_FORWARD_INCLUDED
#define UNITY_STANDARD_CORE_FORWARD_INCLUDED

#include "UnityStandardConfig.cginc"
#include "UnityStandardCore.cginc"

#include "DualDepthPeelingDeclaration.cginc"

VertexOutputForwardBase vertBase(VertexInput v) { return vertForwardBase(v); }
VertexOutputForwardAdd vertAdd(VertexInput v) { return vertForwardAdd(v); }

FragmentOutput fragBase(VertexOutputForwardBase i)
{
  FragmentOutput output;
  DEPTH_PEELING_PRECOLOR(i.pos, output)

    output.Color0 = fragForwardBaseInternal(i);

  DEPTH_PEELING_PEEL(output)
    return output;
}

FragmentOutput fragAdd(VertexOutputForwardAdd i)
{
  FragmentOutput output;
  DEPTH_PEELING_PRECOLOR(i.pos, output)

    output.Color0 = fragForwardAddInternal(i);

  DEPTH_PEELING_PEEL(output)
    return output;
}

#endif // UNITY_STANDARD_CORE_FORWARD_INCLUDED
