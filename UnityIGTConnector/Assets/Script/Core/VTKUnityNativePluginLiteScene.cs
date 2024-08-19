using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;
using System.Diagnostics;

/// <summary>
/// VTKUnityNativePluginLiteScene
/// </summary>

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class VTKUnityNativePluginLiteScene
{
  #region Environment
  // Define dll to import
#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
  const string pluginDll = "__Internal";
#else
  const string pluginDll = "VTKNativePluginLiteScene";
#endif

  // Add Plugin folder to path for dlls to be found at runtime
  static VTKUnityNativePluginLiteScene()
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

  // Fill the VTK renderer
  [DllImport(pluginDll)]
  public static extern void AddSceneObjects(IntPtr renderWindow, bool sliceView);

  // Clear the VTK renderer
  [DllImport(pluginDll)]
  public static extern void RemoveSceneObjects(IntPtr renderWindow);
  // Clear the VTK renderer

  // Initialize sources, mappers and actors
  [DllImport(pluginDll)]
  public static extern void InitializeSceneObjects();

  // Set the input image filename
  [DllImport(pluginDll)]
  public static extern void SetInputFileName(string fileName);

  // Return the visible props bounds
  [DllImport(pluginDll)]
  public static extern IntPtr GetSceneBounds(IntPtr renderWindow);
  public static void GetSceneBounds(IntPtr renderWindow, ref double[] bounds)
  {
    IntPtr bnds_ptr = GetSceneBounds(renderWindow);
    if (bnds_ptr != IntPtr.Zero)
    {
      Marshal.Copy(bnds_ptr, bounds, 0, bounds.Length);
    }
  }

  // Set the color window and level values
  [DllImport(pluginDll)]
  public static extern void SetWindowLevel(double window, double level);
  
  // Set the current slice index
  [DllImport(pluginDll)]
  public static extern void SetSlice(double slice);
    
  // Set the volume cropping planes
  [DllImport(pluginDll)]
  public static extern void SetVolumeCroppingRegionPlanes(double I, double J, double K);

  // Set the slice orientation
  [DllImport(pluginDll)]
  public static extern void SetSliceOrientation(int sliceOrientation);

  // Set which render window corresponds tothe 2D view
  [DllImport(pluginDll)]
  public static extern void SetSliceViewRenderWindow(IntPtr renderWindow);

}
