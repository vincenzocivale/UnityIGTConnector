using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

#region VTKRenderingSystem
public class VTKRenderingSystem
{
  #region Singleton
  static private VTKRenderingSystem m_Instance;
  static public VTKRenderingSystem Instance
  {
    get
    {
      if (m_Instance == null)
        m_Instance = new VTKRenderingSystem();
      return m_Instance;
    }
  }
  #endregion

  // Enable/Disable the rendering system
  public bool Enabled { get; set; }

  // Keep track of cameras using the rendering system
  public Dictionary<Camera, CameraCommandBuffer[]> Cameras = new Dictionary<Camera, CameraCommandBuffer[]>();

  public delegate void EnableVTKCameraComponentAction(Camera unityCamera);
  public static event EnableVTKCameraComponentAction OnVTKCameraComponentEnabled;
  public delegate void DisableVTKCameraComponentAction(Camera unityCamera);
  public static event DisableVTKCameraComponentAction OnVTKCameraComponentDisabled;

  // Remove command buffers from all cameras we added into
  public void Cleanup()
  {
    foreach (var cam in Cameras)
    {
      if (cam.Key != null)
      {
        foreach (var renderBuffer in cam.Value)
        {
          if (renderBuffer != null)
          {
            cam.Key.RemoveCommandBuffer(renderBuffer.Event, renderBuffer.Command);
            OnVTKCameraComponentDisabled?.Invoke(cam.Key); // Inform scene scripts about the camera being removed
          }
        }
      }
    }
    Cameras.Clear();
    VTKUnityNativePlugin.ClearCameraMatrices();
  }

  // Add VTK rendering command buffers to the camera
  public void InitializeCommandBuffers(VTKAbstractCamera camera)
  {
    if (camera.UnityCamera == null || Cameras.ContainsKey(camera.UnityCamera))
    {
      return;
    }

    // Force interlaced rendering pipeline and depth pass if depth peeling is enabled.
    if (DualDepthPeelingRenderingSystem.Instance.Enabled)
    {
      camera.RenderingPipeline = VTKAbstractCamera.RenderingPipelineMode.INTERLACED;
      camera.UnityCamera.depthTextureMode = DepthTextureMode.Depth;
    }

    // Interlaced rendering pipeline
    if (camera.RenderingPipeline == VTKAbstractCamera.RenderingPipelineMode.INTERLACED)
    {
      CameraEvent cameraPassEvent = CameraEvent.BeforeForwardOpaque;
      CameraCommandBuffer depthPass = null;

      if (camera.UnityCamera.depthTextureMode == DepthTextureMode.Depth ||
        camera.UnityCamera.depthTextureMode == DepthTextureMode.DepthNormals)
      {
        cameraPassEvent = CameraEvent.BeforeDepthTexture;

        CommandBuffer depthPassCommandBuffer = new CommandBuffer();
        depthPassCommandBuffer.name = "VTK Depth Pass";
        depthPassCommandBuffer.IssuePluginEventAndData(VTKUnityNativePlugin.RenderOpaquePass(), 0, camera.GetRenderWindowHandle());
        depthPass = new CameraCommandBuffer(CameraEvent.AfterDepthTexture, depthPassCommandBuffer);
        camera.UnityCamera.AddCommandBuffer(depthPass.Event, depthPass.Command);
      }

      CommandBuffer syncCameraCommandBuffer = new CommandBuffer();
      syncCameraCommandBuffer.name = "VTK Synchronize Camera";
      syncCameraCommandBuffer.IssuePluginEventAndData(VTKUnityNativePlugin.SynchronizeCamera(), 0, camera.GetRenderWindowHandle());
      CameraCommandBuffer cameraPass = new CameraCommandBuffer(cameraPassEvent, syncCameraCommandBuffer);
      camera.UnityCamera.AddCommandBuffer(cameraPass.Event, cameraPass.Command);

      CommandBuffer opaquePassCommandBuffer = new CommandBuffer();
      opaquePassCommandBuffer.name = "VTK Opaque Pass";
      opaquePassCommandBuffer.IssuePluginEventAndData(VTKUnityNativePlugin.RenderOpaquePass(), 1, camera.GetRenderWindowHandle());
      CameraCommandBuffer opaquePass = new CameraCommandBuffer(CameraEvent.AfterForwardOpaque, opaquePassCommandBuffer);
      camera.UnityCamera.AddCommandBuffer(opaquePass.Event, opaquePass.Command);

      CameraCommandBuffer translucentPass = null;

      if (!DualDepthPeelingRenderingSystem.Instance.Enabled)
      {
        // VTK Translucent, Volumetric and Overlay passes are done sequentially
        // to reduce the number of blit calls and state changes.
        CommandBuffer translucentPassCommandBuffer = new CommandBuffer();
        translucentPassCommandBuffer.name = "VTK Translucent-Volumetric-Overlay Passes";
        translucentPassCommandBuffer.IssuePluginEventAndData(VTKUnityNativePlugin.RenderTranslucentPass(), 0, camera.GetRenderWindowHandle());
        translucentPass = new CameraCommandBuffer(CameraEvent.AfterForwardAlpha, translucentPassCommandBuffer);
        camera.UnityCamera.AddCommandBuffer(translucentPass.Event, translucentPass.Command);
      }
      else
      {
        // Render VTK translucent props with Unity depth peeling
        DualDepthPeelingRenderingSystem.Instance.VTKRendering = true;
        DualDepthPeelingRenderingSystem.Instance.VTKVolumeRendering = true;

        // Add overlay pass as it wasn't added with the translucent passes
        CommandBuffer overlayPassCommandBuffer = new CommandBuffer();
        overlayPassCommandBuffer.name = "VTK Overlay Pass";
        overlayPassCommandBuffer.IssuePluginEventAndData(VTKUnityNativePlugin.RenderOverlayPass(), 0, camera.GetRenderWindowHandle());
        translucentPass = new CameraCommandBuffer(CameraEvent.AfterImageEffects, overlayPassCommandBuffer);
        camera.UnityCamera.AddCommandBuffer(translucentPass.Event, translucentPass.Command);
      }

      CameraCommandBuffer[] cameraCommandBuffers = new CameraCommandBuffer[] { cameraPass, depthPass, opaquePass, translucentPass };
      Cameras[camera.UnityCamera] = cameraCommandBuffers;
    }

    // Sequential rendering pipeline
    if (camera.RenderingPipeline == VTKCamera.RenderingPipelineMode.SEQUENTIAL)
    {
      CommandBuffer renderCommandBuffer = new CommandBuffer();
      renderCommandBuffer.name = "VTK Render Pass";
      renderCommandBuffer.IssuePluginEventAndData(VTKUnityNativePlugin.Render(), 0, camera.GetRenderWindowHandle());
      CameraCommandBuffer VTKPass = new CameraCommandBuffer(CameraEvent.AfterForwardAlpha, renderCommandBuffer);
      camera.UnityCamera.AddCommandBuffer(VTKPass.Event, VTKPass.Command);

      CameraCommandBuffer[] cameraCommandBuffers = new CameraCommandBuffer[] { VTKPass };
      Cameras[camera.UnityCamera] = cameraCommandBuffers;
    }

    OnVTKCameraComponentEnabled?.Invoke(camera.UnityCamera); // Inform scene scripts about the camera being added
  }
}
#endregion

#region VTKAbstractCamera
[ExecuteInEditMode]
public abstract class VTKAbstractCamera : MonoBehaviour
{
  // VTK rendering pipeline type.
  // In INTERLACED mode, VTK render passes are splitted and executed by
  // different command buffers before/after the corresponding unity render pass
  // In SEQUENTIAL mode, a single command buffer is used to render all VTK
  // passes sequentially.
  public enum RenderingPipelineMode { INTERLACED, SEQUENTIAL };
  public RenderingPipelineMode RenderingPipeline = RenderingPipelineMode.INTERLACED;

  [HideInInspector]
  // Unity camera associated with this VTK renderer
  public Camera UnityCamera = null;

  public abstract IntPtr GetRenderWindowHandle();

  protected abstract void InitializeGraphicsResources();
  protected virtual void ReleaseGraphicsResources()
  {
    // Clear VTK rendering resources.
    // Finalize the render window and remove all renderer and associated actors.
    CommandBuffer finalizeCommandBuffer = new CommandBuffer();
    finalizeCommandBuffer.name = "VTK Finalize";
    finalizeCommandBuffer.IssuePluginEventAndData(VTKUnityNativePlugin.Finalize(), 0, this.GetRenderWindowHandle());
    Graphics.ExecuteCommandBuffer(finalizeCommandBuffer);
  }

  // Expects 16 double values describing column major matrices
  protected abstract void SetCameraMatrices(IntPtr viewMatrix, IntPtr projectionMatrix);
  protected abstract void SetCameraParameters();
  protected abstract void SetRenderWindowParameters();


  void OnPreRender()
  {
    if (UnityCamera == null)
    {
      UnityCamera = GetComponent<Camera>();
    }

    if (UnityCamera == null || !VTKRenderingSystem.Instance.Enabled)
    {
      return;
    }

    // Update command buffers
    VTKRenderingSystem.Instance.InitializeCommandBuffers(this);

    this.SetRenderWindowParameters();

    // Update VTK camera matrices
    Matrix4x4 viewMatrix = UnityCamera.worldToCameraMatrix;
    Matrix4x4 projectionMatrix = UnityCamera.projectionMatrix;

    if (UnityCamera.stereoEnabled)
    {
      // Stereo enabled - Retrieve the matrices for the current eye
      Camera.MonoOrStereoscopicEye activeEye = UnityCamera.stereoActiveEye;

      if (!(Camera.MonoOrStereoscopicEye.Mono == activeEye))
      {
        Camera.StereoscopicEye stereoEye = Camera.StereoscopicEye.Left;

        if (Camera.MonoOrStereoscopicEye.Right == activeEye)
        {
          stereoEye = Camera.StereoscopicEye.Right;
        }

        viewMatrix = UnityCamera.GetStereoViewMatrix(stereoEye);
        projectionMatrix = UnityCamera.GetStereoProjectionMatrix(stereoEye);
      }

      // Enqueue matrices in plugin to synchronize stereo render passes.
      // Unity calls PreRender for left and right first and then calls Render.
      VTKUnityNativePlugin.SetCameraViewMatrix(viewMatrix);
      VTKUnityNativePlugin.SetCameraProjectionMatrix(projectionMatrix);
    }
    else
    {
      // Stereo disabled - Set the matrices for the current camera directly
      VTKUnityNativePlugin.Double16 projectionMatrixColMajor = VTKUnityNativePlugin.Matrix4x4ToDouble16ColMajor(projectionMatrix);
      VTKUnityNativePlugin.Double16 viewMatrixColMajor = VTKUnityNativePlugin.Matrix4x4ToDouble16ColMajor(viewMatrix);

      GCHandle h_projectionMatrix = GCHandle.Alloc(projectionMatrixColMajor.elements, GCHandleType.Pinned);
      GCHandle h_viewMatrix = GCHandle.Alloc(viewMatrixColMajor.elements, GCHandleType.Pinned);

      this.SetCameraMatrices(h_viewMatrix.AddrOfPinnedObject(), h_projectionMatrix.AddrOfPinnedObject());

      h_projectionMatrix.Free();
      h_viewMatrix.Free();
    }

    this.SetCameraParameters();
  }

  public void OnEnable()
  {
    this.InitializeGraphicsResources();
   
    VTKRenderingSystem.Instance.Enabled = true;
    VTKRenderingSystem.Instance.Cleanup();
  }

  public void OnDisable()
  {
    VTKRenderingSystem.Instance.Cleanup();

    if (DualDepthPeelingRenderingSystem.Instance.Enabled)
    {
      DualDepthPeelingRenderingSystem.Instance.Cleanup();
    }

    this.ReleaseGraphicsResources();
  }
}
#endregion
