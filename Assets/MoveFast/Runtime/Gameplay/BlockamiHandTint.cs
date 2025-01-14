// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using System.Linq;
using Blockami.Scripts;
using Oculus.Interaction.Input;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

namespace Oculus.Interaction.MoveFast
{
    /// <summary>
    /// Changes the color of the hand based on an IActiveState
    /// </summary>
    public class BlockamiHandTint : MonoBehaviour
    {
        private BlockamiData BlockamiData;
        
        [SerializeField]
        private string _propertyName;    // the color param on material
        
        [SerializeField]
        private List<MaterialPropertyBlockEditor> _materialPropertyBlockEditors;
        
        [FormerlySerializedAs("_isPlaying")] [SerializeField]
        public bool ShouldTrackHandColor;
        
        private Color _defaultColor;

        private OVRManager OVRManager;
        private List<SyntheticHand> AllSyntheticHands = new List<SyntheticHand>();
        
        
        

        private void Awake()
        {
            BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
            BlockamiData = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");

            var ovrManager = FindObjectOfType<OVRManager>();
            if (!ovrManager)
            {
                enabled = false;
                return;
            }

            AllSyntheticHands = ovrManager.GetComponentsInChildren<SyntheticHand>().ToList();

            
            _materialPropertyBlockEditors = new List<MaterialPropertyBlockEditor>();
            foreach (var syntheticHand in AllSyntheticHands)
            {
                var editors = syntheticHand.GetComponentsInChildren<MaterialPropertyBlockEditor>();
                _materialPropertyBlockEditors.AddRange(editors);
            }

            
            _defaultColor = _materialPropertyBlockEditors[0].Renderers[0].sharedMaterial.GetColor(_propertyName);   
            
            
            // get the value of a color param on current material of a renderer stored in _materialPropertyBlockEditors  (list of one)
           //     -- this has synthetic hand's   r_handMeshNode which is the literal skinnedmeshrenderer gO
           //  uses material OculusHand
           // even though Color is not listed on block edtior in editor, we can use SetColor anyway through code
        }

        private void Update() => UpdateActiveColor();

        public void UpdateActiveColor()
        {
            return;
           var id =  NetworkManager.Singleton.LocalClientId;
           var Col = BlockamiData.GetColorFromColorID(LocalPlayerEntities.Instance.GetPlayerObjects(id).PlayerController
               .ColorID);

           if (!ShouldTrackHandColor) Col = _defaultColor;
         
               for (int i = 0; i < _materialPropertyBlockEditors.Count; i++)
                   _materialPropertyBlockEditors[i].MaterialPropertyBlock.SetColor(_propertyName, Col);
           
        }
    }
    
}
