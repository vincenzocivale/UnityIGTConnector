using UnityEngine;

/// <summary>
/// CameraControl.cs: Helper script to manipulate Unity cameras
/// </summary>
[ExecuteInEditMode]
public class CameraControl : MonoBehaviour
{
  private Camera UnityCamera;

  void Start()
  {
    UnityCamera = GetComponent<Camera>();
  }

  /**
   * Automatically adjust the camera to visualize all visible VTK actors
   */
  public void ResetCamera()
  {
    if (UnityCamera == null)
    {
      UnityCamera = GetComponent<Camera>();
    }

    if (UnityCamera == null)
    {
      return;
    }

    VTKCamera camera = UnityCamera.GetComponent<VTKCamera>();
    if (camera == null)
    {
      return;
    }

    double[] bounds = new double[6];
    VTKUnityNativePluginLiteScene.GetSceneBounds(camera.GetRenderWindowHandle(), ref bounds);
    ResetCamera(bounds);
  }

  /**
   * Automatically adjust the camera on the specified bounding box
   */
  void ResetCamera(double[] bounds)
  {
    if (UnityCamera == null)
    {
      UnityCamera = GetComponent<Camera>();
    }

    Vector3 p0 = new Vector3((float)bounds[0], (float)bounds[2], (float)bounds[4]);
    Vector3 p1 = new Vector3((float)bounds[1], (float)bounds[3], (float)bounds[5]);
    Vector3 center = 0.5f * (p0 + p1);
    float sceneSize = Vector3.Distance(p0, p1);
    float fov = Mathf.Deg2Rad * UnityCamera.fieldOfView;
    float distance = 0.5f * sceneSize / Mathf.Sin(fov * 0.5f);
    UnityCamera.transform.position = center - distance * UnityCamera.transform.forward;
    UnityCamera.transform.LookAt(center);
    // Adjust orthographic size
    if (UnityCamera.orthographic)
    {
      UnityCamera.orthographicSize = 0.5f * (float)(bounds[3] - bounds[2]);
    }
  }
}