using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public abstract class VTKAbstractSceneObject : MonoBehaviour
{
  protected abstract void AddSceneObjects(VTKCamera camera);

  protected abstract void RemoveSceneObjects(VTKCamera camera);

  void OnEnable()
  {
    // Add scene props to exisiting cameras
    Dictionary<Camera, CameraCommandBuffer[]> cameraCommandBuffers = VTKRenderingSystem.Instance.Cameras;
    if (DualDepthPeelingRenderingSystem.Instance.Enabled)
    {
      cameraCommandBuffers = DualDepthPeelingRenderingSystem.Instance.Cameras;
    }

    foreach (KeyValuePair<Camera, CameraCommandBuffer[]> cameraCommandBuffersPair in cameraCommandBuffers)
    {
      AddSceneObjects(cameraCommandBuffersPair.Key);
    }

    // Listen for new cameras being available
    VTKRenderingSystem.OnVTKCameraComponentEnabled += AddSceneObjects;
    VTKRenderingSystem.OnVTKCameraComponentDisabled += RemoveSceneObjects;
  }

  void OnDisable()
  {
    Dictionary<Camera, CameraCommandBuffer[]> cameraCommandBuffers = VTKRenderingSystem.Instance.Cameras;
    if (DualDepthPeelingRenderingSystem.Instance.Enabled)
    {
      cameraCommandBuffers = DualDepthPeelingRenderingSystem.Instance.Cameras;
    }

    foreach (KeyValuePair<Camera, CameraCommandBuffer[]> cameraCommandBuffersPair in cameraCommandBuffers)
    {
      RemoveSceneObjects(cameraCommandBuffersPair.Key);
    }

    VTKRenderingSystem.OnVTKCameraComponentEnabled -= AddSceneObjects;
    VTKRenderingSystem.OnVTKCameraComponentDisabled -= RemoveSceneObjects;
  }

  void AddSceneObjects(Camera unityCamera)
  {
    if (unityCamera == null)
    {
      return;
    }

    this.AddSceneObjects(unityCamera.GetComponent<VTKCamera>());
  }

  void RemoveSceneObjects(Camera unityCamera)
  {
    if (unityCamera == null)
    {
      return;
    }

    this.RemoveSceneObjects(unityCamera.GetComponent<VTKCamera>());
  }
}
