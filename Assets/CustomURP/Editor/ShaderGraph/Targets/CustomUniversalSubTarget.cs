using UnityEngine;
using UnityEditor.ShaderGraph;
using static Unity.Rendering.Universal.ShaderUtils;
using UnityEditor.ShaderGraph.Internal;
#if HAS_VFX_GRAPH
using UnityEditor.VFX;
#endif

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    abstract class CustomUniversalSubTarget : SubTarget<CustomUniversalTarget>, IHasMetadata
#if HAS_VFX_GRAPH
        , IRequireVFXContext
#endif
    {
        static readonly GUID kSourceCodeGuid = new GUID("92228d45c1ff66740bfa9e6d97f7e280");  // CustomUniversalSubTarget.cs

        public override void Setup(ref TargetSetupContext context)
        {
            context.AddAssetDependency(kSourceCodeGuid, AssetCollection.Flags.SourceDependency);
        }

        protected abstract ShaderID shaderID { get; }

#if HAS_VFX_GRAPH
        // VFX Properties
        VFXContext m_ContextVFX = null;
        VFXContextCompiledData m_ContextDataVFX;
        protected bool TargetsVFX() => m_ContextVFX != null;

        public void ConfigureContextData(VFXContext context, VFXContextCompiledData data)
        {
            m_ContextVFX = context;
            m_ContextDataVFX = data;
        }

#endif

        protected SubShaderDescriptor PostProcessSubShader(SubShaderDescriptor subShaderDescriptor)
        {
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                return VFXSubTarget.PostProcessSubShader(subShaderDescriptor, m_ContextVFX, m_ContextDataVFX);
#endif
            return subShaderDescriptor;
        }

        public override void GetFields(ref TargetFieldContext context)
        {
#if HAS_VFX_GRAPH
            if (TargetsVFX())
                VFXSubTarget.GetFields(ref context, m_ContextVFX);
#endif
        }

        public virtual string identifier => GetType().Name;
        public virtual ScriptableObject GetMetadataObject(GraphDataReadOnly graphData)
        {
            var urpMetadata = ScriptableObject.CreateInstance<UniversalMetadata>();
            urpMetadata.shaderID = shaderID;
            urpMetadata.alphaMode = target.alphaMode;
            urpMetadata.isVFXCompatible = graphData.IsVFXCompatible();

            if (shaderID != ShaderID.SG_SpriteLit && shaderID != ShaderID.SG_SpriteUnlit)
            {
                urpMetadata.allowMaterialOverride = target.allowMaterialOverride;
                urpMetadata.surfaceType = target.surfaceType;
                urpMetadata.castShadows = target.castShadows;
            }
            else
            {
                //Ignore unsupported settings in SpriteUnlit/SpriteLit
                urpMetadata.allowMaterialOverride = false;
                urpMetadata.surfaceType = SurfaceType.Transparent; 
                urpMetadata.castShadows = false;
            }
            
            return urpMetadata;
        }

        private int lastMaterialNeedsUpdateHash = 0;
        protected virtual int ComputeMaterialNeedsUpdateHash() => 0;

        public override object saveContext
        {
            get
            {
                int hash = ComputeMaterialNeedsUpdateHash();
                bool needsUpdate = hash != lastMaterialNeedsUpdateHash;
                if (needsUpdate)
                    lastMaterialNeedsUpdateHash = hash;

                return new UniversalShaderGraphSaveContext { updateMaterials = needsUpdate };
            }
        }
    }


}
