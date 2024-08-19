using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

#region DualDepthPeelingRenderingSystem
public class DualDepthPeelingRenderingSystem
{
  #region Singleton
  static private DualDepthPeelingRenderingSystem m_Instance;
  static public DualDepthPeelingRenderingSystem Instance
  {
    get
    {
      if (m_Instance == null)
        m_Instance = new DualDepthPeelingRenderingSystem();
      return m_Instance;
    }
  }
  #endregion

  // Enable/Disable the rendering system
  public bool Enabled { get; set; }

  // Keep track of cameras using the rendering system
  public Dictionary<Camera, CameraCommandBuffer[]> Cameras = new Dictionary<Camera, CameraCommandBuffer[]>();

  // Game objects to peel
  private HashSet<GameObject> GameObjects = new HashSet<GameObject>();

  // Number of depth peeling render passes
  private uint NumberOfLayers = 1;

  // Render VTK translucent actors and volumes
  public bool VTKRendering = false;
  public bool VTKVolumeRendering = false;

  // Depth peeling materials
  private Material copyOpaqueDepthMaterial;
  private Material initDepthMaterial;
  private Material blendBackMaterial;
  private Material finalBlendMaterial;
  private Material blitDepthMaterial;

  private Dictionary<GameObject, Material> PeelMaterialsCache = new Dictionary<GameObject, Material>();
  private Dictionary<GameObject, Material> LastPeelMaterialsCache = new Dictionary<GameObject, Material>();

  // Depth peeling render textures
  private RenderTexture frontSource;
  private RenderTexture frontDestination;
  private RenderTexture backTemp;
  private RenderTexture backAccumulation;
  private RenderTexture depthSource;
  private RenderTexture depthDestination;
  private RenderTexture volumeOpaqueDepth;

  private RenderTextureDescriptor colorTextureDescriptor;
  private RenderTextureDescriptor depthRGTextureDescriptor;

  // Set the number of depth peeling passes
  public void SetNumberOfLayers(uint n)
  {
    if (n == NumberOfLayers)
    {
      return;
    }

    NumberOfLayers = n;
    Cleanup();
  }

  // Add a game object to the depth peeling rendering system
  public void AddObject(GameObject o)
  {
    if (GameObjects.Contains(o))
    {
      return;
    }

    GameObjects.Add(o);
    Cleanup();
  }

  // Remove a game object from the depth peeling rendering system
  public void RemoveObject(GameObject o)
  {
    if (!GameObjects.Contains(o))
    {
      return;
    }

    GameObjects.Remove(o);
    Cleanup();
  }

  // Remove command buffers from all cameras we added into and clean resources
  public void Cleanup()
  {
    if (frontSource) frontSource.Release();
    if (frontDestination) frontDestination.Release();
    if (backTemp) backTemp.Release();
    if (backAccumulation) backAccumulation.Release();
    if (depthSource) depthSource.Release();
    if (depthDestination) depthDestination.Release();
    if (volumeOpaqueDepth) volumeOpaqueDepth.Release();

    foreach (var cam in Cameras)
    {
      if (cam.Key)
      {
        foreach (var renderBuffer in cam.Value)
        {
          cam.Key.RemoveCommandBuffer(renderBuffer.Event, renderBuffer.Command);
        }
      }
    }
    Cameras.Clear();

    Object.DestroyImmediate(copyOpaqueDepthMaterial);
    Object.DestroyImmediate(initDepthMaterial);
    Object.DestroyImmediate(blendBackMaterial);
    Object.DestroyImmediate(finalBlendMaterial);
    Object.DestroyImmediate(blitDepthMaterial);

    // Destroy materials instantiated by unity
    foreach (var mat in PeelMaterialsCache)
    {
      Object.DestroyImmediate(mat.Value);
    }
    PeelMaterialsCache.Clear();
    foreach (var mat in LastPeelMaterialsCache)
    {
      Object.DestroyImmediate(mat.Value);
    }
    LastPeelMaterialsCache.Clear();

    Object.Destroy(frontSource);
    Object.Destroy(frontDestination);
    Object.Destroy(backTemp);
    Object.Destroy(backAccumulation);
    Object.Destroy(depthSource);
    Object.Destroy(depthDestination);
    Object.Destroy(volumeOpaqueDepth);
  }

  // Allocate render textures that need to be passed to the plugin.
  // If it was possible for CommandBuffer.GetTemporaryRT(int nameId,...) to
  // return a RenderTexture instead of its identifier, then we could send its
  // handle directly to the plugin. Instead we need a RenderTexture instance
  // to call GetNativeTexturePtr().
  private void AllocateRenderTextures()
  {
    frontSource = new RenderTexture(colorTextureDescriptor);
    frontSource.name = "FrontSource";

    frontDestination = new RenderTexture(colorTextureDescriptor);
    frontDestination.name = "FrontDestination";

    backTemp = new RenderTexture(colorTextureDescriptor);
    backTemp.name = "BackTemp";

    backAccumulation = new RenderTexture(colorTextureDescriptor);
    backAccumulation.name = "BackAccumulation";

    depthSource = new RenderTexture(depthRGTextureDescriptor);
    depthSource.name = "DepthSource";

    depthDestination = new RenderTexture(depthRGTextureDescriptor);
    depthDestination.name = "DepthDestination";

    int w = depthRGTextureDescriptor.width;
    int h = depthRGTextureDescriptor.height;
    volumeOpaqueDepth = new RenderTexture(w, h, 24, RenderTextureFormat.Depth);
    volumeOpaqueDepth.name = "VolumeOpaqueDepth";

    // Unity BUG: colorBuffer must be accessed before using
    // GetNativeTexturePtr() for the first time. See related issue:
    // https://issuetracker.unity3d.com/issues/getnativetextureptr-returns-0-on-rendertexture-until-colorbuffer-property-get-is-called
    depthSource.colorBuffer.ToString();
    depthDestination.colorBuffer.ToString();
    backTemp.colorBuffer.ToString();
    frontSource.colorBuffer.ToString();
    frontDestination.colorBuffer.ToString();
    backAccumulation.colorBuffer.ToString();
    volumeOpaqueDepth.depthBuffer.ToString();
  }

  public void RegisterRenderTextures()
  {
    // WARNING: Strange Unity behavior here, potentially related to the Unity
    // bug mentionned above.
    // GetNativeTexturePtr() must be accessed after the pass has done rendering
    // to prevent textures to be bound to the wrong target. 
    // Adding the following results in additional glFlush() calls at the end of
    // the render pass and preserves texture binding.
    // 
    if (frontSource != null)
    {
      frontSource.GetNativeTexturePtr().ToString();
      depthSource.GetNativeTexturePtr().ToString();
      depthDestination.GetNativeTexturePtr().ToString();
      frontDestination.GetNativeTexturePtr().ToString();
      backTemp.GetNativeTexturePtr().ToString();
      backAccumulation.GetNativeTexturePtr().ToString();
      volumeOpaqueDepth.GetNativeTexturePtr().ToString();
    }
  }

  // Swap objects helper
  static void Swap<T>(ref T lhs, ref T rhs)
  {
    T temp;
    temp = lhs;
    lhs = rhs;
    rhs = temp;
  }

  // Add depth peeling command buffers to the camera
  public void InitializeCommandBuffers(VTKCamera camera)
  {
    if (camera.UnityCamera == null || Cameras.ContainsKey(camera.UnityCamera))
    {
      return;
    }

    // Depth peeling requires reading from the camera depth texture
    camera.UnityCamera.depthTextureMode = DepthTextureMode.Depth;

    // Color textures descriptor
    colorTextureDescriptor = new RenderTextureDescriptor();
    colorTextureDescriptor.bindMS = false;
    colorTextureDescriptor.width = camera.UnityCamera.pixelWidth;
    colorTextureDescriptor.height = camera.UnityCamera.pixelHeight;
    colorTextureDescriptor.depthBufferBits = 0;
    colorTextureDescriptor.msaaSamples = 1;
    colorTextureDescriptor.colorFormat = RenderTextureFormat.Default;
    colorTextureDescriptor.dimension = TextureDimension.Tex2D;
    colorTextureDescriptor.volumeDepth = 1;
    //colorTextureDescriptor.sRGB = true;

    // Depth peeling RG depth textures descriptor
    depthRGTextureDescriptor = new RenderTextureDescriptor();
    depthRGTextureDescriptor.bindMS = false;
    depthRGTextureDescriptor.width = camera.UnityCamera.pixelWidth;
    depthRGTextureDescriptor.height = camera.UnityCamera.pixelHeight;
    depthRGTextureDescriptor.depthBufferBits = 0;
    depthRGTextureDescriptor.msaaSamples = 1;
    depthRGTextureDescriptor.colorFormat = RenderTextureFormat.RGFloat;
    depthRGTextureDescriptor.dimension = TextureDimension.Tex2D;
    depthRGTextureDescriptor.volumeDepth = 1;

    // Allocate render textures.
    // Their handle needs to be sent to the plugin so we cannot get them as
    // temporary RT from nameIds using the command buffer, otherwise the handle
    // would not be accessible.
    AllocateRenderTextures();

    // ------------------------------------------------------------------------
    // Depth peeling materials
    copyOpaqueDepthMaterial = new Material(Shader.Find("DualDepthPeeling/Internal/CopyOpaqueDepth"));
    copyOpaqueDepthMaterial.hideFlags = HideFlags.HideAndDontSave;

    initDepthMaterial = new Material(Shader.Find("DualDepthPeeling/Internal/InitializeDepth"));
    initDepthMaterial.hideFlags = HideFlags.HideAndDontSave;

    blendBackMaterial = new Material(Shader.Find("DualDepthPeeling/Internal/BlendBackBuffer"));
    blendBackMaterial.hideFlags = HideFlags.HideAndDontSave;

    finalBlendMaterial = new Material(Shader.Find("DualDepthPeeling/Internal/FinalImageBlending"));
    finalBlendMaterial.hideFlags = HideFlags.HideAndDontSave;

    blitDepthMaterial = new Material(Shader.Find("DualDepthPeeling/Internal/BlitDepth"));
    blitDepthMaterial.hideFlags = HideFlags.HideAndDontSave;

    // WARNING: Material caching - 2020-12-15
    // We cannot update the material blend state at runtime using command buffers because:
    // - Only global shader properties can be changed from command buffers.
    // - MaterialPropertiesBlock cannot set _BlendOperation as it only sets variables inside CGPROGRAM/ENDCG
    // So we must have 2 instances of the material per game object, one for each blend operation.
    // The instantiated materials must be stored to be destroyed later in Cleanup().
    foreach (GameObject gameObject in GameObjects)
    {
      Material sharedPeelMaterial = gameObject.GetComponent<Renderer>().sharedMaterial;

      Material peelMaterial = new Material(sharedPeelMaterial.shader);
      peelMaterial.CopyPropertiesFromMaterial(sharedPeelMaterial);
      peelMaterial.SetInt("_BlendOperation", (int)UnityEngine.Rendering.BlendOp.Max);
      peelMaterial.DisableKeyword("LAST_PEEL");
      PeelMaterialsCache[gameObject] = peelMaterial;

      Material lastPeelMaterial = new Material(sharedPeelMaterial.shader);
      lastPeelMaterial.CopyPropertiesFromMaterial(sharedPeelMaterial);
      lastPeelMaterial.SetInt("_BlendOperation", (int)UnityEngine.Rendering.BlendOp.Add);
      lastPeelMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
      lastPeelMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
      lastPeelMaterial.EnableKeyword("LAST_PEEL");
      LastPeelMaterialsCache[gameObject] = lastPeelMaterial;
    }

    // Camera command buffer
    CommandBuffer cbPeel = new CommandBuffer();
    cbPeel.name = "Peel";

    // ------------------------------------------------------------------------
    // Copy Opaque Depth

    int opaqueDepth = Shader.PropertyToID("opaqueDepth");
    cbPeel.GetTemporaryRT(opaqueDepth, depthRGTextureDescriptor);

    cbPeel.SetRenderTarget(opaqueDepth);
    cbPeel.ClearRenderTarget(true, true, new Color(-1, -1, 0, 0));

    copyOpaqueDepthMaterial.SetFloat("clearValue", 1);
    cbPeel.Blit(BuiltinRenderTextureType.Depth, opaqueDepth, copyOpaqueDepthMaterial);

    // ------------------------------------------------------------------------
    // Prepare
    cbPeel.Blit(opaqueDepth, depthSource);
    cbPeel.Blit(opaqueDepth, depthDestination);

    cbPeel.SetRenderTarget(backTemp);
    cbPeel.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

    cbPeel.SetRenderTarget(frontSource);
    cbPeel.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

    cbPeel.SetRenderTarget(backAccumulation);
    cbPeel.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

    // Blit opaque depth texture for volume depth peeling
    cbPeel.Blit(BuiltinRenderTextureType.Depth, volumeOpaqueDepth, blitDepthMaterial);

    RenderTargetIdentifier[] rt0 = new RenderTargetIdentifier[] { backTemp, depthSource };
    cbPeel.SetRenderTarget(rt0, volumeOpaqueDepth);

    // Initialize VTK Dual Depth Peeling resources and depth
    if (VTKRendering)
    {
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.DepthPeelingInitialize(), 0, camera.GetRenderWindowHandle());

      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignDepthSource(), 0, depthSource.GetNativeTexturePtr());
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignDepthDestination(), 0, depthDestination.GetNativeTexturePtr());
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignBackTemp(), 0, backTemp.GetNativeTexturePtr());
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignFrontSource(), 0, frontSource.GetNativeTexturePtr());
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignFrontDestination(), 0, frontDestination.GetNativeTexturePtr());
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignBack(), 0, backAccumulation.GetNativeTexturePtr());
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignOpaqueDepth(), 0, volumeOpaqueDepth.GetNativeTexturePtr());

      cbPeel.SetRenderTarget(rt0, volumeOpaqueDepth);
      // WARNING: Calling SetRenderTarget followed by IssuePluginEventAndData does not seem to
      // guarantee that the texture is bound to the draw target when executing the plugin callback.
      // Work around this by adding a ClearRenderTarget call that does not clear color nor depth.
      cbPeel.ClearRenderTarget(false, false, new Color(0.0f, 0.0f, 0.0f, 0.0f));

      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.DepthPeelingInitializeDepth(), 0, camera.GetRenderWindowHandle());
    }

    // Unity Rendering
    foreach (GameObject gameObject in GameObjects)
    {
      Renderer renderer = gameObject.GetComponent<Renderer>();
      cbPeel.DrawRenderer(renderer, initDepthMaterial, 0, 0);
    }

    // VTK Volume rendering: PeelVolumesOutsideTranslucentRange
    if (VTKRendering && VTKVolumeRendering)
    {
      RenderTargetIdentifier[] rti = new RenderTargetIdentifier[] { backAccumulation, frontSource };
      cbPeel.SetRenderTarget(rti, volumeOpaqueDepth);
      // WARNING: Calling SetRenderTarget followed by IssuePluginEventAndData does not seem to
      // guarantee that the texture is bound to the draw target when executing the plugin callback.
      // Work around this by adding a ClearRenderTarget call that does not clear color nor depth.
      cbPeel.ClearRenderTarget(false, false, new Color(0.0f, 0.0f, 0.0f, 0.0f));

      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.PeelVolumesOutsideTranslucentRange(), 0, camera.GetRenderWindowHandle());
    }

    cbPeel.ReleaseTemporaryRT(opaqueDepth);

    // ------------------------------------------------------------------------
    // Peel

    // WARNING: While we must use RenderTexture instances to send their handle
    // to the plugin, here we must use temporary render textures for sampling
    // in Unity shaders. If we try to read from the instances, textures get
    // bound to the wrong target. This only happens when the game is running
    // and the window looses focus (i.e. hide/show the window).
    int lastFrontPeel = Shader.PropertyToID("lastFrontPeel");
    cbPeel.GetTemporaryRT(lastFrontPeel, colorTextureDescriptor);

    int lastDepthPeel = Shader.PropertyToID("lastDepthPeel");
    cbPeel.GetTemporaryRT(lastDepthPeel, depthRGTextureDescriptor);

    int newPeel = Shader.PropertyToID("newPeel");
    cbPeel.GetTemporaryRT(newPeel, colorTextureDescriptor);

    int peelIndex = 0;
    while (peelIndex < NumberOfLayers)
    {
      // -- InitializeTargetsForTranslucentPass:
      cbPeel.SetRenderTarget(backTemp);
      cbPeel.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

      cbPeel.SetRenderTarget(depthDestination);
      cbPeel.ClearRenderTarget(true, true, new Color(-1.0f, -1.0f, 0.0f, 0.0f));

      // -- Prepare Front Destination:
      cbPeel.SetRenderTarget(frontDestination);
      if (!VTKVolumeRendering)
      {
        // -- ClearFrontDestination:
        cbPeel.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
      }
      else
      {
        // -- CopyFrontSourceToFrontDestination:
        cbPeel.Blit(frontSource, frontDestination);
      }

      cbPeel.Blit(frontSource, lastFrontPeel); // Blit for sampling in shaders
      cbPeel.Blit(depthSource, lastDepthPeel);

      RenderTargetIdentifier[] rb = new RenderTargetIdentifier[] { backTemp, frontDestination, depthDestination };
      cbPeel.SetRenderTarget(rb, volumeOpaqueDepth);
      // WARNING: Calling SetRenderTarget followed by IssuePluginEventAndData does not seem to
      // guarantee that the texture is bound to the draw target when executing the plugin callback.
      // Work around this by adding a ClearRenderTarget call that does not clear color nor depth.
      cbPeel.ClearRenderTarget(false, false, new Color(0.0f, 0.0f, 0.0f, 0.0f));

      // VTK Rendering
      if (VTKRendering)
      {
        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.PeelTranslucentGeometry(), 0, camera.GetRenderWindowHandle());
      }

      // Unity Rendering
      foreach (GameObject gameObject in GameObjects)
      {
        // The right approach is to have only one material per game object and
        // update the blend state and keywords as needed.
        // See warning about material caching.
        Renderer renderer = gameObject.GetComponent<Renderer>();
        cbPeel.DrawRenderer(renderer, PeelMaterialsCache[gameObject]);
      }

      // -- BlendBackBuffer:
      cbPeel.Blit(backTemp, newPeel); // Blit for sampling in shader
      cbPeel.Blit(backTemp, backAccumulation, blendBackMaterial);

      // -- SwapFrontBufferSourceDest
      Swap(ref frontSource, ref frontDestination);
      if (VTKRendering)
      {
        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignFrontSource(), 0, frontSource.GetNativeTexturePtr());
        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignFrontDestination(), 0, frontDestination.GetNativeTexturePtr());
      }

      // VTK Volume rendering: PeelVolumetricGeometry
      if (VTKRendering && VTKVolumeRendering)
      {
        // -- InitializeTargetsForVolumetricPass:
        cbPeel.SetRenderTarget(backTemp);
        cbPeel.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

        // -- Prepare Front Destination:
        // Only rendering volumes here so always use
        // CopyFrontSourceToFrontDestination:
        cbPeel.SetRenderTarget(frontDestination);
        cbPeel.Blit(frontSource, frontDestination);

        RenderTargetIdentifier[] rtv = new RenderTargetIdentifier[] { backTemp.colorBuffer, frontDestination.colorBuffer };
        cbPeel.SetRenderTarget(rtv, volumeOpaqueDepth);
        // WARNING: Calling SetRenderTarget followed by IssuePluginEventAndData does not seem to
        // guarantee that the texture is bound to the draw target when executing the plugin callback.
        // Work around this by adding a ClearRenderTarget call that does not clear color nor depth.
        cbPeel.ClearRenderTarget(false, false, new Color(0.0f, 0.0f, 0.0f, 0.0f));

        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.PeelVolumetricGeometry(), 0, camera.GetRenderWindowHandle());

        // -- BlendBackBuffer:
        cbPeel.Blit(backTemp, newPeel); // Blit for sampling in shader
        cbPeel.Blit(backTemp, backAccumulation, blendBackMaterial);

        Swap(ref frontSource, ref frontDestination);
        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignFrontSource(), 0, frontSource.GetNativeTexturePtr());
        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignFrontDestination(), 0, frontDestination.GetNativeTexturePtr());
      }

      // -- SwapDepthBufferSourceDest
      Swap(ref depthSource, ref depthDestination);
      if (VTKRendering)
      {
        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignDepthSource(), 0, depthSource.GetNativeTexturePtr());
        cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.AssignDepthDestination(), 0, depthDestination.GetNativeTexturePtr());
      }

      peelIndex++;
    }

    cbPeel.ReleaseTemporaryRT(lastFrontPeel);
    cbPeel.ReleaseTemporaryRT(newPeel);

    // ------------------------------------------------------------------------
    // AlphaBlendRender
    cbPeel.Blit(depthSource, lastDepthPeel); // Blit for sampling in shader
    cbPeel.SetRenderTarget(backAccumulation);

    if (VTKRendering)
    {
      cbPeel.IssuePluginEventAndData(VTKUnityNativePlugin.DepthPeelingFinalize(), 0, camera.GetRenderWindowHandle());
    }

    foreach (GameObject gameObject in GameObjects)
    {
      // The right approach is to have only one material per game object and
      // update the blend state and keywords as needed.
      // See warning about material caching.
      Renderer renderer = gameObject.GetComponent<Renderer>();
      cbPeel.DrawRenderer(renderer, LastPeelMaterialsCache[gameObject], 0, -1);
    }

    cbPeel.ReleaseTemporaryRT(lastDepthPeel);

    // ------------------------------------------------------------------------
    // BlendFinalImage

    // WARNING: While we must use RenderTexture instances to send their handle
    // to the plugin, here we must use temporary render textures for sampling
    // in Unity shaders. If we try to read from the instances, textures get
    // bound to the wrong target. This only happens when the game is running
    // and the window looses focus (i.e. hide/show the window).
    int frontTexture = Shader.PropertyToID("frontTexture");
    cbPeel.GetTemporaryRT(frontTexture, colorTextureDescriptor);
    cbPeel.Blit(frontSource, frontTexture);

    int backTexture = Shader.PropertyToID("backTexture");
    cbPeel.GetTemporaryRT(backTexture, colorTextureDescriptor);
    cbPeel.Blit(backAccumulation, backTexture);

    cbPeel.Blit(BuiltinRenderTextureType.CameraTarget, BuiltinRenderTextureType.CameraTarget, finalBlendMaterial);

    cbPeel.ReleaseTemporaryRT(backTexture);
    cbPeel.ReleaseTemporaryRT(frontTexture);

    // ------------------------------------------------------------------------
    // Add command buffer
    CameraCommandBuffer depthPeelingPass = new CameraCommandBuffer(CameraEvent.AfterForwardAlpha, cbPeel);
    camera.UnityCamera.AddCommandBuffer(depthPeelingPass.Event, depthPeelingPass.Command);

    CameraCommandBuffer[] cameraCommandBuffers = new CameraCommandBuffer[] { depthPeelingPass };
    Cameras[camera.UnityCamera] = cameraCommandBuffers;
  }
}
#endregion

#region DualDepthPeelingCamera
//[ExecuteInEditMode]
public class DualDepthPeelingCamera : MonoBehaviour
{
  public uint NumberOfLayers = 4;

  void OnPostRender()
  {
    if (!DualDepthPeelingRenderingSystem.Instance.Enabled)
    {
      return;
    }
    DualDepthPeelingRenderingSystem.Instance.RegisterRenderTextures();
  }

  void OnPreRender()
  {
    VTKCamera camera = GetComponent<VTKCamera>();
    if (!DualDepthPeelingRenderingSystem.Instance.Enabled || camera == null)
    {
      return;
    }

    // Update command buffers and rendering parameters
    DualDepthPeelingRenderingSystem.Instance.SetNumberOfLayers(NumberOfLayers);
    DualDepthPeelingRenderingSystem.Instance.InitializeCommandBuffers(camera);
  }

  public void OnEnable()
  {
    DualDepthPeelingRenderingSystem.Instance.Enabled = true;
    DualDepthPeelingRenderingSystem.Instance.Cleanup();
  }

  public void OnDisable()
  {
    DualDepthPeelingRenderingSystem.Instance.Cleanup();
  }
}
#endregion
