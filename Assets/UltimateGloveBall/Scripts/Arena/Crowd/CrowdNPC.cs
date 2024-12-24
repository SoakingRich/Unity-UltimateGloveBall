// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using UnityEngine;

namespace UltimateGloveBall.Arena.Crowd
{
    /// <summary>
    /// Representation of an NPC crowd member. It can set the face index of the body and change the attachment color.
    /// On initialization we randomize the start time and speed of the animation.
    /// The item used can also be changed.
    /// </summary>
    public class CrowdNPC : MonoBehaviour
    {
        private static readonly int s_bodyColorID = Shader.PropertyToID("_Body_Color");                      // int identifiers for string Material Paramaters
        private static readonly int s_attachmentColorID = Shader.PropertyToID("_Attachment_Color");
        private static readonly int s_faceSwapID = Shader.PropertyToID("_Face_swap");
        [SerializeField] private Animator[] m_animators;
        [SerializeField] private Renderer m_faceRenderer;
        [SerializeField] private Renderer[] m_attachmentsRenderers;
        [SerializeField] private Renderer m_bodyRenderer;

        [SerializeField] private GameObject[] m_items;

        private int m_currentItemIndex;
        private MaterialPropertyBlock m_materialBlock;

        private void Awake()
        {
            for (var i = 0; i < m_items.Length; ++i)
            {
                if (m_items[i].activeSelf)
                {
                    m_currentItemIndex = i;              // each NPC in editor will be given a different item (gameObject) to be Active. Each NPC should record which one has been set active
                    break;
                }
            }
        }

        public void Init(float timeOffset, float speed, Vector2 face)        // init with randomized properties per npc
        {
            foreach (var animator in m_animators)
            {
                if (animator != null)
                {
                    animator.speed = speed;
                    if (animator.isActiveAndEnabled)
                    {
                        var info = animator.GetCurrentAnimatorStateInfo(0);
                        animator.Play(info.shortNameHash, 0, timeOffset);             // play random anim, with random time offset
                    }
                }
            }

            m_materialBlock ??= new MaterialPropertyBlock();     //MaterialPropertyBlock is a handle for a MaterialInstance we can call SetVector or SetColor on
            m_faceRenderer.GetPropertyBlock(m_materialBlock);    // GetPropertyBlock lets us set a MaterialPropertyBlock handle for a renderer's material
            m_materialBlock.SetVector(s_faceSwapID, face);            // make changes on the MaterialPropertyBlock handle
            m_faceRenderer.SetPropertyBlock(m_materialBlock);    // set the new property block on the Renderer
        }

        public void SetColor(Color color)
        {
            m_materialBlock ??= new MaterialPropertyBlock();
            foreach (var rend in m_attachmentsRenderers)
            {
                if (rend != null)
                {
                    rend.GetPropertyBlock(m_materialBlock);
                    m_materialBlock.SetColor(s_attachmentColorID, color);          // set color param on every renderers material in NPC's m_attachmentsRenderers
                    rend.SetPropertyBlock(m_materialBlock);
                }
            }
        }

        public void SetBodyColor(Color color)
        {
            m_materialBlock ??= new MaterialPropertyBlock();
            m_bodyRenderer.GetPropertyBlock(m_materialBlock);
            m_materialBlock.SetColor(s_bodyColorID, color);          // set color specifically on the body
            m_bodyRenderer.SetPropertyBlock(m_materialBlock);
        }

        public void ChangeItem(int itemIndex)                   // specify an item to set active for a NPC
        {
            if (itemIndex >= 0 && itemIndex < m_items.Length)
            {
                m_items[m_currentItemIndex].SetActive(false);
                m_items[itemIndex].SetActive(true);
                m_currentItemIndex = itemIndex;
            }
        }
    }
}