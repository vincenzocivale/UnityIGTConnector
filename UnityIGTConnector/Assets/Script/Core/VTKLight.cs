using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// VTKLight: This MonoBehaviour script should be added to a Unity light component
/// to setup the corresponding VTK scene light.
/// </summary>
[ExecuteInEditMode]
public class VTKLight : VTKAbstractSceneObject
{
  int LightId = -1;
  void Start()
  {
    // Query Unity light component
    Light unityLight = gameObject.GetComponent<Light>();

    if (unityLight == null)
    {
      return;
    }

    LightId = unityLight.GetInstanceID();
    VTKUnityNativePluginLite.CreateLight(LightId);
    VTKUnityNativePluginLite.SetLightType(LightId, (int)unityLight.type);

    // Spot light
    if (unityLight.type == LightType.Spot)
    {
      VTKUnityNativePluginLite.SetLightSpotAngle(LightId, unityLight.spotAngle * 0.5);
    }

    // Handle light attenuation
    // https://geom.io/bakery/wiki/index.php?title=Point_Light_Attenuation
    double lightRange = unityLight.range > 0 ? unityLight.range : 1e-4;
    VTKUnityNativePluginLite.SetLightRange(LightId, 1 / (lightRange * lightRange));
    VTKUnityNativePluginLite.SetLightIntensity(LightId, unityLight.intensity);
    VTKUnityNativePluginLite.SetLightColor(LightId, unityLight.color.r, unityLight.color.g, unityLight.color.b);

    double[] lightPosition = { unityLight.transform.position.x, unityLight.transform.position.y, unityLight.transform.position.z };
    double[] lightRotation = { unityLight.transform.eulerAngles.x, unityLight.transform.eulerAngles.y, unityLight.transform.eulerAngles.z };

    GCHandle h_lightPosition = GCHandle.Alloc(lightPosition, GCHandleType.Pinned);
    GCHandle h_lightRotation = GCHandle.Alloc(lightRotation, GCHandleType.Pinned);

    VTKUnityNativePluginLite.SetLightTransform(LightId, h_lightPosition.AddrOfPinnedObject(), h_lightRotation.AddrOfPinnedObject());

    h_lightPosition.Free();
    h_lightRotation.Free();
  }

  protected override void AddSceneObjects(VTKCamera camera)
  {
    if (camera == null || LightId == -1)
    {
      return;
    }

    VTKUnityNativePluginLite.AddLight(LightId, camera.GetRenderWindowHandle());
  }

  protected override void RemoveSceneObjects(VTKCamera camera)
  {
    if (camera == null)
    {
      return;
    }

    VTKUnityNativePluginLite.RemoveAllLights(camera.GetRenderWindowHandle());
  }
}
