using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// CameraEvent - CommandBuffer pair.
/// Helper class storing a CommandBuffer with its associated CameraEvent.
public class CameraCommandBuffer
{
  public CameraCommandBuffer(CameraEvent e, CommandBuffer cb)
  {
    this.Event = e;
    this.Command = cb;
  }

  public CameraEvent Event { get; set; }
  public CommandBuffer Command { get; set; }
};
