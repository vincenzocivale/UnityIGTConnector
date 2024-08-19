using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Depth peeling game object.
// Helper script to register the parent game object for depth peeling rendering
public class DepthPeelingObject : MonoBehaviour
{
  void OnEnable()
  {
    // Disable mesh renderer.
    // This game object is rendered by depth peeling command buffers.
    MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
    meshRenderer.enabled = false;

    // Add game object to depth peeling render pass.
    DualDepthPeelingRenderingSystem.Instance.AddObject(gameObject);
  }

  void OnDisable()
  {
    // Add game object to depth peeling render pass.
    DualDepthPeelingRenderingSystem.Instance.RemoveObject(gameObject);
  }
}
