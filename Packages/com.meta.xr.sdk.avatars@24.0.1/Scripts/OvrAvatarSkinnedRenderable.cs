#define OVR_AVATAR_PRIMITIVE_HACK_BOUNDING_BOX

using System;
using Oculus.Avatar2;
using UnityEngine;

/// @file OvrAvatarSkinnedRenderable.cs

public abstract class OvrAvatarSkinnedRenderable : OvrAvatarRenderable
{
    /**
     * Component that encapsulates the meshes of a skinned avatar.
     * This component can only be added to game objects that
     * have a Unity Mesh, a Mesh filter and a SkinnedRenderer.
     *
     * In addition to vertex positions, texture coordinates and
     * colors, a vertex in a skinned mesh can be driven by up
     * to 4 bones in the avatar skeleton. Each frame the transforms
     * of these bones are multiplied by the vertex weights for
     * the bone and applied to compute the final vertex position.
     * This can be done by Unity on the CPU or the GPU, or by
     * the Avatar SDK using the GPU. Different variations of this
     * class are provided to allow you to select which implementation
     * best suits your application.
     *
     * @see OvrAvatarPrimitive
     * @see ApplyMeshPrimitive
     * @see OvrAvatarUnitySkinnedRenderable
     */

    /// Designates whether this renderable animations are enabled or not.
    private bool _isAnimationEnabled = true;
    public bool IsAnimationEnabled
    {
        get => _isAnimationEnabled;
        internal set
        {
            if (_isAnimationEnabled != value)
            {
                _isAnimationEnabled = value;
                OnAnimationEnabledChanged(_isAnimationEnabled);
            }
        }
    }

    protected override void Awake()
    {
        base.Awake();
        CheckPropertyIdInit();
    }

    protected internal override void ApplyMeshPrimitive(OvrAvatarPrimitive primitive)
    {
        CheckDefaultRenderer();

        base.ApplyMeshPrimitive(primitive);
        SetAnimationDataValidityForRenderer();
    }

    ///
    /// Apply the given bone transforms from the avatar skeleton
    /// to the Unity skinned mesh renderer.
    /// @param bones    Array of Transforms for the skeleton bones.
    ///                 These must be in the order the Unity SkinnedRenderer expects.
    ///
    public abstract void ApplySkeleton(Transform[] bones);

    public abstract IDisposableBuffer CheckoutMorphTargetBuffer(uint morphCount);
    public abstract void MorphTargetBufferUpdated(IDisposableBuffer buffer);

    public virtual void UpdateSkinningOrigin(in CAPI.ovrAvatar2Transform skinningOrigin)
    {
        // Default implementation is just to apply to transform
        transform.ApplyOvrTransform(skinningOrigin);
    }

    public abstract bool UpdateJointMatrices(
        CAPI.ovrAvatar2EntityId entityId,
        OvrAvatarPrimitive primitive,
        CAPI.ovrAvatar2PrimitiveRenderInstanceID primitiveInstanceId);

    // Invoked when `IsAnimationEnabled` changes.
    protected abstract void OnAnimationEnabledChanged(bool isNowEnabled);
    internal abstract void AnimationFrameUpdate();
    internal abstract void RenderFrameUpdate();


    protected abstract int NumAnimationFramesBeforeValidData { get; }
    private bool _isAnimationDataCompletelyValid = false;
    internal bool IsAnimationDataCompletelyValid { get => _isAnimationDataCompletelyValid;
        private set
        {
            bool valChanged = _isAnimationDataCompletelyValid != value;
            _isAnimationDataCompletelyValid = value;
            SetAnimationDataValidityForRenderer();

            if (value && valChanged)
            {
                // Safely raise the event for all subscribers
                AnimationDataComplete?.Invoke(this);
            }
        }
    }

    private int _numValidAnimationFrames;
    protected int NumValidAnimationFrames
    {
        get => _numValidAnimationFrames;
        set
        {
            _numValidAnimationFrames = value;
            IsAnimationDataCompletelyValid = value >= NumAnimationFramesBeforeValidData;
        }
    }

    internal delegate void AnimationDataCompletionHandler(OvrAvatarSkinnedRenderable sender);
    internal event AnimationDataCompletionHandler AnimationDataComplete;

    protected void IncrementValidAnimationFramesIfNeeded()
    {
        if (NumValidAnimationFrames < NumAnimationFramesBeforeValidData)
        {
            NumValidAnimationFrames++;
        }
    }

    private void SetAnimationDataValidityForRenderer()
    {
        if (rendererComponent != null)
        {
            rendererComponent.GetPropertyBlock(MatBlock);
            SetAnimationDataValidityInMaterialBlock(MatBlock);
            rendererComponent.SetPropertyBlock(MatBlock);
        }
    }

    private void SetAnimationDataValidityInMaterialBlock(MaterialPropertyBlock block)
    {
        block.SetInt(IsSkinnerOutputValidPropID, IsAnimationDataCompletelyValid ? 1 : 0);
    }


    // TODO: Reference count
    public interface IDisposableBuffer : IDisposable
    {
        IntPtr BufferPtr { get; }
    }

    private static int IsSkinnerOutputValidPropID => _propertyIds.IS_SKINNER_OUTPUT_VALID_PROP_ID;
    private struct AttributePropertyIds
    {
        public readonly int IS_SKINNER_OUTPUT_VALID_PROP_ID;

        public bool IsValid => IS_SKINNER_OUTPUT_VALID_PROP_ID != 0;

        public enum InitMethod { PropertyToId }
        public AttributePropertyIds(InitMethod initMethod)
        {
            IS_SKINNER_OUTPUT_VALID_PROP_ID = Shader.PropertyToID("u_IsExternalAttributeSourceValid");
        }
    }

    private static AttributePropertyIds _propertyIds = default;

    private static void CheckPropertyIdInit()
    {
        if (!_propertyIds.IsValid)
        {
            _propertyIds = new AttributePropertyIds(AttributePropertyIds.InitMethod.PropertyToId);
        }
    }
}
