using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEditor;

/// <summary>
/// VTKUnityNativePlugin: C# interface to the VTK native plugin for unity 3D.
/// </summary>

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public class VTKUnityNativePlugin
{
  // Define dll to import
#if (UNITY_IPHONE || UNITY_WEBGL) && !UNITY_EDITOR
  const string pluginDll = "__Internal";
#else
  const string pluginDll = "VTKNativePlugin";
#endif

  // Add Plugin folder to path for dlls to be found at runtime
  static VTKUnityNativePlugin()
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

    RedirectOutputWindow(dllPath + Path.DirectorySeparatorChar + "vtkOutputWindow.log");
  }

  // Redirect VTK output window
  [DllImport(pluginDll)]
  public static extern void RedirectOutputWindow(string fileName);

  // VTK rendering callback functions to be used with CommandBuffer.IssuePluginEvent
  [DllImport(pluginDll)]
  public static extern IntPtr Render();
  [DllImport(pluginDll)]
  public static extern IntPtr SynchronizeCamera();
  [DllImport(pluginDll)]
  public static extern IntPtr RenderOpaquePass();
  [DllImport(pluginDll)]
  public static extern IntPtr RenderTranslucentPass();
  [DllImport(pluginDll)]
  public static extern IntPtr RenderOverlayPass();
  [DllImport(pluginDll)]
  public static extern IntPtr DepthPeelingInitialize();
  [DllImport(pluginDll)]
  public static extern IntPtr DepthPeelingInitializeDepth();
  [DllImport(pluginDll)]
  public static extern IntPtr PeelVolumesOutsideTranslucentRange();
  [DllImport(pluginDll)]
  public static extern IntPtr PeelTranslucentGeometry();
  [DllImport(pluginDll)]
  public static extern IntPtr PeelVolumetricGeometry();
  [DllImport(pluginDll)]
  public static extern IntPtr DepthPeelingFinalize();

  [DllImport(pluginDll)]
  private static extern void LockImpl();
  [DllImport(pluginDll)]
  private static extern void UnlockImpl();

  [DllImport(pluginDll)]
  public static extern IntPtr Finalize();

  private static bool _IsRenderLocked = false;
  public static bool IsRenderLocked => _IsRenderLocked;

  public static void Lock()
  {
    LockImpl();
    _IsRenderLocked = true;
  }

  public static void Unlock()
  {
    UnlockImpl();
    _IsRenderLocked = false;
  }
  // Assign plugin textures for depth peeling
  [DllImport(pluginDll)]
  public static extern IntPtr AssignDepthSource();
  [DllImport(pluginDll)]
  public static extern IntPtr AssignDepthDestination();
  [DllImport(pluginDll)]
  public static extern IntPtr AssignFrontSource();
  [DllImport(pluginDll)]
  public static extern IntPtr AssignFrontDestination();
  [DllImport(pluginDll)]
  public static extern IntPtr AssignBack();
  [DllImport(pluginDll)]
  public static extern IntPtr AssignBackTemp();
  [DllImport(pluginDll)]
  public static extern IntPtr AssignOpaqueDepth();

  // Set camera projection matrix.
  // The input matrix is transposed before being passed to VTK to match the coordinate system.
  [DllImport(pluginDll)]
  public static extern void SetCameraProjectionMatrix(Double16 projectionMatrix);
  public static void SetCameraProjectionMatrix(Matrix4x4 matrix)
  {
    SetCameraProjectionMatrix(Matrix4x4ToDouble16ColMajor(matrix));
  }

  // Set camera view matrix.
  // The input matrix is transposed before being passed to VTK to match the coordinate system.
  [DllImport(pluginDll)]
  public static extern void SetCameraViewMatrix(Double16 viewMatrix);
  public static void SetCameraViewMatrix(Matrix4x4 matrix)
  {
    SetCameraViewMatrix(Matrix4x4ToDouble16ColMajor(matrix));
  }

  // Clear queued camera matrices
  [DllImport(pluginDll)]
  public static extern void ClearCameraMatrices();

  // Double16 (float[16]) helper structure for matrix conversion
  [StructLayout(LayoutKind.Sequential)]
  public struct Double16
  {
    [MarshalAsAttribute(UnmanagedType.LPArray, SizeConst = 16)]
    public double[] elements;
  }

  // Convert unity Matrix4x4 to Double16
  public static Double16 Matrix4x4ToDouble16(Matrix4x4 unityMatrix)
  {
    Double16 pluginMatrix = new Double16()
    {
      elements = new double[16]
    };

    for (int row = 0; row < 4; row++)
    {
      for (int col = 0; col < 4; col++)
      {
        pluginMatrix.elements[(row * 4) + col] = unityMatrix[row, col];
      }
    }

    return pluginMatrix;
  }

  // Transpose and convert unity Matrix4x4 to Double16
  public static Double16 Matrix4x4ToDouble16ColMajor(Matrix4x4 unityMatrix)
  {
    Double16 pluginMatrix = new Double16()
    {
      elements = new double[16]
    };

    for (int row = 0; row < 4; row++)
    {
      for (int col = 0; col < 4; col++)
      {
        pluginMatrix.elements[(col * 4) + row] = unityMatrix[row, col];
      }
    }

    return pluginMatrix;
  }
}
