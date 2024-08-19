using System.Collections;
using UnityEngine;

[ExecuteInEditMode]
public class VTKSceneCamera : MonoBehaviour
{
  private ArrayList Cameras = new ArrayList();

  void OnEnable()
  {
    Camera.onPreRender += OnPreRenderCallback;
  }

  void OnDisable()
  {
    Camera.onPreRender -= OnPreRenderCallback;

    foreach (Camera cam in Cameras)
    {
      if (cam != null)
      {
        cam.gameObject.GetComponent<VTKCamera>().enabled = false;
      }
    }
    Cameras.Clear();
  }

  void OnPreRenderCallback(Camera cam)
  {
    // Store cameras to clean on disable
    if (!Cameras.Contains(cam))
    {
      Cameras.Add(cam);
    }

    // Add and enable VTKCamera on the Unity camera
    VTKCamera c = cam.gameObject.GetComponent<VTKCamera>();
    if (c == null)
    {
      c = cam.gameObject.AddComponent<VTKCamera>();
    }
    c.enabled = true;

    // Make sure the VTK rendering system is initialized for this camera
    if (VTKRenderingSystem.Instance.Enabled)
    {
      if (!VTKRenderingSystem.Instance.Cameras.ContainsKey(cam))
      {
        VTKRenderingSystem.Instance.InitializeCommandBuffers(c);
      }
    }
    if (DualDepthPeelingRenderingSystem.Instance.Enabled)
    {
      if (!DualDepthPeelingRenderingSystem.Instance.Cameras.ContainsKey(cam))
      {
        DualDepthPeelingRenderingSystem.Instance.InitializeCommandBuffers(c);
      }
    }
  }
}
