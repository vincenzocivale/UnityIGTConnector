using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

[ExecuteInEditMode]
public class VTKSceneObject : VTKAbstractSceneObject
{
  public string FileName;
  private string _lastFileName;

  public Camera SliceViewCamera = null;

  [Header("Window/Level")]
  public double Window = 3500;
  private double _lastWindow = 3500;

  public double Level = 2047;
  private double _lastLevel = 2047;

  [Header("Slice")]
  [Range(0.0f, 1.0f)]
  public double Slice = 0.0f;
  private double _lastSlice = 0.0f;

  public enum SliceOrientation { I, J, K };
  public SliceOrientation Orientation = SliceOrientation.K;
  private SliceOrientation _lastOrientation = SliceOrientation.K;

  [Header("Volume Rendering")]
  [Range(0.001f, 0.999f)]
  public float AxialCropping = 0.85f;
  [Range(0.001f, 0.999f)]
  public float CoronalCropping = 0.7f;
  [Range(0.001f, 0.999f)]
  public float SagittalCropping = 0.56f;

  // Variables previous state for update
  float lastAxialCropping = 0.85f;
  float lastCoronalCropping = 0.7f;
  float lastSagittalCropping = 0.56f;


  // Initialize sources, mappers and actors
  void Start()
  {
    // Set default volume input file
    FileName = Path.Combine(UnityEngine.Application.streamingAssetsPath, "headsq.vti");
    _lastFileName = FileName;

    VTKUnityNativePluginLiteScene.SetInputFileName(FileName);

    VTKUnityNativePluginLiteScene.InitializeSceneObjects();

    VTKUnityNativePluginLiteScene.SetWindowLevel(Window, Level);
  }

  void Update()
  {
    if (FileName != _lastFileName && (File.Exists(FileName) || Directory.Exists(FileName)))
    {
      VTKUnityNativePlugin.Lock();
      VTKUnityNativePluginLiteScene.SetInputFileName(FileName);
      VTKUnityNativePlugin.Unlock();

      _lastFileName = FileName;

      // Reset slice and cameras
      Slice = 0;
      ResetCameras();
    }

    if (_lastLevel != Level || _lastWindow != Window)
    {
      VTKUnityNativePlugin.Lock();
      VTKUnityNativePluginLiteScene.SetWindowLevel(Window, Level);
      VTKUnityNativePlugin.Unlock();

      _lastWindow = Window;
      _lastLevel = Level;
    }

    if (_lastOrientation != Orientation)
    {
      VTKUnityNativePlugin.Lock();
      VTKUnityNativePluginLiteScene.SetSliceOrientation((int)Orientation);
      VTKUnityNativePlugin.Unlock();

      _lastOrientation = Orientation;

      // Reset slice and cameras
      Slice = 0;
      if (SliceViewCamera != null)
      {
        switch (Orientation)
        {
          case SliceOrientation.I:
            SliceViewCamera.transform.forward = new Vector3(1, 0, 0);
            SliceViewCamera.transform.eulerAngles = new Vector3(0, 90, -90);
            break;
          case SliceOrientation.J:
            SliceViewCamera.transform.forward = new Vector3(0, 1, 0);
            break;
          case SliceOrientation.K:
            SliceViewCamera.transform.forward = new Vector3(0, 0, 1);
            break;
        }
      }
      ResetCameras();
    }

    if (_lastSlice != Slice)
    {
      VTKUnityNativePlugin.Lock();
      VTKUnityNativePluginLiteScene.SetSlice(Slice);
      VTKUnityNativePlugin.Unlock();

      _lastSlice = Slice;
    }

    if (AxialCropping != lastAxialCropping ||
      CoronalCropping != lastCoronalCropping ||
      SagittalCropping != lastCoronalCropping)
    {
      VTKUnityNativePlugin.Lock();
      VTKUnityNativePluginLiteScene.SetVolumeCroppingRegionPlanes(SagittalCropping, CoronalCropping, AxialCropping);
      VTKUnityNativePlugin.Unlock();

      lastAxialCropping = AxialCropping;
      lastCoronalCropping = CoronalCropping;
      lastSagittalCropping = SagittalCropping;
    }
  }

  // Fill the VTK renderer.
  // Automatically called when this script is enabled.
  protected override void AddSceneObjects(VTKCamera camera)
  {
    if (camera != null)
    {
      Camera unityCamera = camera.GetComponent<Camera>();
      if (unityCamera != null)
      {
        VTKUnityNativePlugin.Lock();
        VTKUnityNativePluginLiteScene.AddSceneObjects(camera.GetRenderWindowHandle(), unityCamera.orthographic);
        VTKUnityNativePlugin.Unlock();
      }

      ResetCameras();
    }
  }

  // Clear the VTK renderer.
  // Automatically called when this script is disabled.
  // Remove all view props by default
  protected override void RemoveSceneObjects(VTKCamera camera)
  {
    if (camera != null)
    {
      VTKUnityNativePlugin.Lock();
      VTKUnityNativePluginLiteScene.RemoveSceneObjects(camera.GetRenderWindowHandle());
      VTKUnityNativePlugin.Unlock();
    }
  }

  void ResetCameras()
  {
    Dictionary<Camera, CameraCommandBuffer[]> cameraCommandBuffers = VTKRenderingSystem.Instance.Cameras;
    foreach (KeyValuePair<Camera, CameraCommandBuffer[]> cameraCommandBuffersPair in cameraCommandBuffers)
    {
      CameraControl control = cameraCommandBuffersPair.Key.GetComponent<CameraControl>();
      if (control != null)
      {
        VTKUnityNativePlugin.Lock();
        control.ResetCamera();
        VTKUnityNativePlugin.Unlock();
      }
    }
  }
}
