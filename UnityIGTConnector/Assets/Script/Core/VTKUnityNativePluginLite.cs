using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

/// <summary>
/// VTKUnityNativePluginLite: C# interface to the VTK native plugin for unity 3D.
/// </summary>

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class VTKUnityNativePluginLite
{
  #region Environment
  // Define dll to import
#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
  const string pluginDll = "__Internal";
#else
  const string pluginDll = "VTKNativePluginLite";
#endif

  // Add Plugin folder to path for dlls to be found at runtime
  static VTKUnityNativePluginLite()
  {
    string currentPath = Environment.GetEnvironmentVariable("PATH",
      EnvironmentVariableTarget.Process);
#if UNITY_EDITOR_32
    string dllPath = Application.dataPath
        + Path.DirectorySeparatorChar + "VTKUnity-Activiz"
        + Path.DirectorySeparatorChar + "Plugin";
#elif UNITY_EDITOR_64
    string dllPath = Application.dataPath
        + Path.DirectorySeparatorChar + "VTKUnity-Activiz"
        + Path.DirectorySeparatorChar + "Plugin";
#else // Player
    string dllPath = Application.dataPath
        + Path.DirectorySeparatorChar + "Plugin";
#endif
    if (currentPath != null && currentPath.Contains(dllPath) == false)
    {
      Environment.SetEnvironmentVariable("PATH", currentPath + Path.PathSeparator
          + dllPath, EnvironmentVariableTarget.Process);
    }
  }

  #endregion

  #region RenderWindow
  // Get the render window for a specific camera.
  [DllImport(pluginDll)]
  public static extern IntPtr GetRenderWindow(int cameraId);
  [DllImport(pluginDll)]
  public static extern IntPtr ReleaseRenderWindow();

  [DllImport(pluginDll)]
  public static extern void InitializeGraphicsResources(IntPtr renderWindow);
  [DllImport(pluginDll)]
  public static extern void SetMultiSamples(IntPtr renderWindow, int samples);
  [DllImport(pluginDll)]
  public static extern void SetUseSRGBColorSpace(IntPtr renderWindow, bool sRGB);
  [DllImport(pluginDll)]
  public static extern void SetRenderWindowSize(IntPtr renderWindow, int w, int h);
  #endregion

  #region Camera
  [DllImport(pluginDll)]
  public static extern void SetCameraMatrices(IntPtr renderWindow, IntPtr viewMatrix, IntPtr projectionMatrix);
  [DllImport(pluginDll)]
  public static extern void SetCameraClippingRange(IntPtr renderWindow, double near, double far);
  [DllImport(pluginDll)]
  public static extern void SetCameraPosition(IntPtr renderWindow, double x, double y, double z);
  #endregion

  #region Lights
  [DllImport(pluginDll)]
  public static extern void CreateLight(int lightId);
  [DllImport(pluginDll)]
  public static extern void AddLight(int lightId, IntPtr renderWindow);
  [DllImport(pluginDll)]
  public static extern void RemoveAllLights(IntPtr renderWindow);
  [DllImport(pluginDll)]
  public static extern void SetLightType(int lightId, int lightType);
  [DllImport(pluginDll)]
  public static extern void SetLightSpotAngle(int lightId, double angle);
  [DllImport(pluginDll)]
  public static extern void SetLightRange(int lightId, double range);
  [DllImport(pluginDll)]
  public static extern void SetLightIntensity(int lightId, double intensity);
  [DllImport(pluginDll)]
  public static extern void SetLightColor(int lightId, double r, double g, double b);
  [DllImport(pluginDll)]
  public static extern void SetLightTransform(int lightId, IntPtr position, IntPtr eulerAngles);
  #endregion

}
