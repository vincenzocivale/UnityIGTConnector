using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

#region VTKCamera
[ExecuteInEditMode]
public class VTKCamera : VTKAbstractCamera
{
  public override IntPtr GetRenderWindowHandle()
  {
    if (UnityCamera == null)
    {
      UnityCamera = GetComponent<Camera>();
    }

    if (UnityCamera == null)
    {
      return IntPtr.Zero;
    }

    return VTKUnityNativePluginLite.GetRenderWindow(UnityCamera.GetInstanceID());
  }

  protected override void InitializeGraphicsResources()
  {
    VTKUnityNativePluginLite.InitializeGraphicsResources(this.GetRenderWindowHandle());
  }

  protected override void ReleaseGraphicsResources()
  {
    base.ReleaseGraphicsResources();

    // Remove render window reference in plugin.
    // Must be done using a command buffer to ensure it is executed after the
    // one called by the base class.

    if (UnityCamera == null)
    {
      UnityCamera = GetComponent<Camera>();
    }

    if (UnityCamera == null)
    {
      return;
    }

    GCHandle h_cameraId = GCHandle.Alloc(UnityCamera.GetInstanceID(), GCHandleType.Pinned);

    CommandBuffer releaseCommandBuffer = new CommandBuffer();
    releaseCommandBuffer.name = "Release VTK window";
    releaseCommandBuffer.IssuePluginEventAndData(VTKUnityNativePluginLite.ReleaseRenderWindow(), 0, h_cameraId.AddrOfPinnedObject());
    Graphics.ExecuteCommandBuffer(releaseCommandBuffer);

    h_cameraId.Free();
  }

  protected override void SetCameraMatrices(IntPtr viewMatrix, IntPtr projMatrix)
  {
    VTKUnityNativePluginLite.SetCameraMatrices(this.GetRenderWindowHandle(), viewMatrix, projMatrix);
  }

  protected override void SetCameraParameters()
  {
    if (UnityCamera == null)
    {
      return;
    }

    Vector3 cameraPos = UnityCamera.transform.position;
    VTKUnityNativePluginLite.SetCameraClippingRange(this.GetRenderWindowHandle(), UnityCamera.nearClipPlane, UnityCamera.farClipPlane);
    VTKUnityNativePluginLite.SetCameraPosition(this.GetRenderWindowHandle(), cameraPos.x, cameraPos.y, cameraPos.z);
  }

  protected override void SetRenderWindowParameters()
  {
    if (UnityCamera == null)
    {
      return;
    }

    // Set VTK render window parameters
    VTKUnityNativePluginLite.SetMultiSamples(this.GetRenderWindowHandle(), QualitySettings.antiAliasing);
    VTKUnityNativePluginLite.SetUseSRGBColorSpace(this.GetRenderWindowHandle(), QualitySettings.activeColorSpace == ColorSpace.Gamma);
    VTKUnityNativePluginLite.SetRenderWindowSize(this.GetRenderWindowHandle(), (int)UnityCamera.pixelRect.width, (int)UnityCamera.pixelRect.height);
  }
}
#endregion
