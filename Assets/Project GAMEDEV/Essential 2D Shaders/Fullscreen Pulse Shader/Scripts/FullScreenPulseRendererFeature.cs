#if UNITY_2022_2_OR_NEWER
#define PROJECT_GAMEDEV_URP_CUTOFF_VERSION
#endif

using UnityEngine;
using UnityEngine.Rendering;
//using UnityEngine.Rendering.Universal;

namespace ProjectGamedev.Shaders
{
#if UNITY_2022_1
#pragma warning disable CS0618 //On Unity 2022.1.x, the old RenderTargetHandle system is necessary (albeit obsolete) as there are no fullscreen shaders yet in URP.
#endif
//    public class FullScreenPulseRendererFeature : ScriptableRendererFeature
//    {
//        [System.Serializable]
//        public class BlitSettings
//        {
//            public RenderPassEvent Event = RenderPassEvent.AfterRenderingTransparents;

//            public Material blitMaterial = null;
//            public int blitMaterialPassIndex = 0;
//            public Target destination = Target.Color;
//            public string textureId = "_FullscreenPulseBlitPassTexture";
//        }

//        public enum Target
//        {
//            Color,
//            Texture
//        }

//        public BlitSettings settings = new();

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//        RTHandle renderTextureHandle;
//#else
//        RenderTargetHandle renderTextureHandle;
//#endif

//        FullscreenPulseBlitPass blitPass;

//        public override void Create()
//        {
//            var passIndex = settings.blitMaterial != null ? settings.blitMaterial.passCount - 1 : 1;
//            settings.blitMaterialPassIndex = Mathf.Clamp(settings.blitMaterialPassIndex, -1, passIndex);
//            blitPass = new FullscreenPulseBlitPass(settings.Event, settings.blitMaterial, settings.blitMaterialPassIndex, name);

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//            renderTextureHandle = RTHandles.Alloc(settings.textureId, name: settings.textureId);
//#else
//            renderTextureHandle.Init(settings.textureId);
//#endif
//        }

//#if UNITY_2022_1_OR_NEWER //New workflow with enqueuing render passes.
//        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//        {
//            //Allow playing the fullscreen pulse effect only in-game.
//            if (renderingData.cameraData.cameraType == CameraType.Game)
//                renderer.EnqueuePass(blitPass);
//        }

//        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
//        {
//            var src = renderer.cameraColorTargetHandle;

//#if UNITY_2022_1
//            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : renderTextureHandle;
//#else
//            var dest = (settings.destination == Target.Color) ? ScriptableRenderPass.k_CameraTarget : renderTextureHandle;
//#endif


//            if (settings.blitMaterial == null)
//            {
//                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing references in the assigned renderer.", GetType().Name);
//                return;
//            }

//            blitPass.Setup(src, dest);
//        }
//#else //Below Unity 2022.1, i.e. up to 2021.3 inclusive - old workflow without enqueuing render passes.
//        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
//        {
//            if (renderingData.cameraData.cameraType != CameraType.Game)
//                return;

//            var src = renderer.cameraColorTarget;
//            var dest = (settings.destination == Target.Color) ? RenderTargetHandle.CameraTarget : renderTextureHandle;

//            if (settings.blitMaterial == null)
//            {
//                Debug.LogWarningFormat("Missing Blit Material. {0} blit pass will not execute. Check for missing references in the assigned renderer.", GetType().Name);
//                return;
//            }

//            blitPass.Setup(src, dest);
//            renderer.EnqueuePass(blitPass);
//        }
//#endif

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//        protected override void Dispose(bool disposing)
//        {
//            renderTextureHandle?.Release();
//            blitPass.Cleanup();
//        }
//#endif

//        public class FullscreenPulseBlitPass : ScriptableRenderPass
//        {
//            public enum RenderTarget
//            {
//                Color,
//                RenderTexture,
//            }

//            public Material blitMaterial = null;
//            public int blitShaderPassIndex = 0;
//            public FilterMode filterMode;

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//            private RTHandle source;
//            private RTHandle destination;
//            private RTHandle temporaryColorTexture;
//#else
//            private RenderTargetIdentifier source;
//            private RenderTargetHandle destination;
//            private RenderTargetHandle temporaryColorTexture;
//#endif

//            private RenderTextureDescriptor colorDesc;

//            private readonly string profilerTag;
//            new private readonly ProfilingSampler profilingSampler;

//            public FullscreenPulseBlitPass(RenderPassEvent renderPassEvent, Material blitMaterial, int blitShaderPassIndex, string tag)
//            {
//                this.renderPassEvent = renderPassEvent;
//                this.blitMaterial = blitMaterial;
//                this.blitShaderPassIndex = blitShaderPassIndex;
//                this.profilerTag = tag;
//                this.profilingSampler = new(this.profilerTag);

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//                temporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture");
//#else
//                temporaryColorTexture.Init("_TemporaryColorTexture");
//#endif
//            }

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//            public void Setup(RTHandle source, RTHandle destination)
//            {
//                this.source = source;
//                this.destination = destination;
//            }
//#else
//            public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
//            {
//                this.source = source;
//                this.destination = destination;
//            }
//#endif

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
//            {
//                colorDesc = renderingData.cameraData.cameraTargetDescriptor;
//                colorDesc.depthBufferBits = (int)DepthBits.None;

//                //Set up temporary color buffer.
//                RenderingUtils.ReAllocateIfNeeded(ref temporaryColorTexture, colorDesc, name: "_TemporaryColorTexture");

//                ConfigureTarget(temporaryColorTexture); //Configure to color only.
//                ConfigureClear(ClearFlag.Color, Color.clear);
//            }
//#endif

//            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
//            {
//                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

//#if !PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//                colorDesc = renderingData.cameraData.cameraTargetDescriptor;
//                colorDesc.depthBufferBits = (int)DepthBits.None;
//#endif

//                using (new ProfilingScope(cmd, profilingSampler))
//                {

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//                    if (destination == ScriptableRenderPass.k_CameraTarget)
//#else
//                    if (destination == RenderTargetHandle.CameraTarget)
//#endif
//                    {

//#if UNITY_2022_2_0
//                        //Workaround for issue UUM-14400 on Unity 2022.2.0f1.
//                        //See https://issuetracker.unity3d.com/issues/urp-custom-render-pass-does-not-work-when-using-2d-renderer for more information.
//                        cmd.GetTemporaryRT(Shader.PropertyToID(temporaryColorTexture.name), colorDesc, filterMode);
//                        Blitter.BlitCameraTexture(cmd, source, temporaryColorTexture, blitMaterial, blitShaderPassIndex);
//                        Blitter.BlitCameraTexture(cmd, temporaryColorTexture, source, blitMaterial, blitShaderPassIndex);
//#elif PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//                        cmd.GetTemporaryRT(Shader.PropertyToID(temporaryColorTexture.name), colorDesc, filterMode);
//                        Blitter.BlitCameraTexture(cmd, source, temporaryColorTexture, blitMaterial, blitShaderPassIndex);
//                        Blitter.BlitCameraTexture(cmd, temporaryColorTexture, source);
//#else
//                        cmd.GetTemporaryRT(temporaryColorTexture.id, colorDesc, filterMode);
//                        Blit(cmd, source, temporaryColorTexture.Identifier(), blitMaterial, blitShaderPassIndex);
//                        Blit(cmd, temporaryColorTexture.Identifier(), source);
//#endif
//                    }
//                    else
//                    {

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//                        if (source != null)
//                            Blitter.BlitCameraTexture(cmd, source, destination, blitMaterial, blitShaderPassIndex);
//#else
//                        Blit(cmd, source, destination.Identifier(), blitMaterial, blitShaderPassIndex);
//#endif
//                    }
//                }

//                context.ExecuteCommandBuffer(cmd);
//                cmd.Clear();
//                CommandBufferPool.Release(cmd);
//            }

//            public override void FrameCleanup(CommandBuffer cmd)
//            {
//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//                cmd.ReleaseTemporaryRT(Shader.PropertyToID(temporaryColorTexture.name));
//#else
//                cmd.ReleaseTemporaryRT(temporaryColorTexture.id);
//#endif
//            }

//#if PROJECT_GAMEDEV_URP_CUTOFF_VERSION
//            public void Cleanup()
//            {
//                //Release temporary RTHandle.
//                temporaryColorTexture?.Release();
//            }
//#endif
//        }
//    }
#if UNITY_2022_1
#pragma warning restore CS0618
#endif
}