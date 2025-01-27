// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections;
using System.Collections.Generic;
using Blockami.Scripts;
using Meta.Utilities;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Player.Respawning;
using UltimateGloveBall.Arena.Services;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace UltimateGloveBall.Arena.Player
{
    /// <summary>
    /// Controls the player state. Handles the state of the shield, the invulnerability, team state and reference the
    /// respawn controller.
    /// </summary>
    public class PlayerControllerNetwork : NetworkBehaviour                  // Handle gameplay input functions
    {                                                                        

    private const float SHIELD_USAGE_RATE = 20f;    
        private const float SHIELD_CHARGE_RATE = 32f;
        private const float SHIELD_MAX_CHARGE = 100f;
        private const float SHIELD_RESET_TIME = 0.5f;

        [SerializeField] private Collider m_collider;
        [SerializeField] private PlayerAvatarEntity m_avatar;
        [SerializeField, AutoSet] private NetworkedTeam m_networkedTeam;
        [SerializeField, AutoSet] private RespawnController m_respawnController;

        private bool m_shieldActivated = false;
        private Glove.GloveSide m_shieldSide = Glove.GloveSide.Left;

        public GloveArmatureNetworking ArmatureRight;
        public GloveArmatureNetworking ArmatureLeft;

        public GloveNetworking GloveRight;
        public GloveNetworking GloveLeft;

        private NetworkVariable<float> m_shieldCharge = new(SHIELD_MAX_CHARGE);

        private NetworkVariable<float> m_shieldOffTimer = new();
        private NetworkVariable<bool> m_shieldInResetMode = new(false);
        private NetworkVariable<bool> m_shieldDisabled = new(false);

        public NetworkVariable<bool> IsInvulnerable = new();
        private readonly HashSet<Object> m_invulnerabilityActors = new();

        public NetworkedTeam NetworkedTeamComp => m_networkedTeam;
        public RespawnController RespawnController => m_respawnController;

        public Action<bool> OnInvulnerabilityStateUpdatedEvent;

        
        
        
        
        public DrawingGrid OwnedDrawingGrid;
        
        public Action<PlayerControllerNetwork> OnCyclePlayerColor;
        
        public NetworkVariable<int>  NetColorID = new();
        public int ColorID => NetColorID.Value;
        public NetworkVariable<ulong> CurrentPlayerShot = new NetworkVariable<ulong>(writePerm: NetworkVariableWritePermission.Owner);
        
        public List<PlayerShotObject> AllPlayerShots = new List<PlayerShotObject>();
        
       // public NetworkVariable<PlayerShot> CurrentPlayerShot = new NetworkVariable<PlayerShot>();
       private BlockamiData BlockamiData;

       
       
       
       
       
       
       
       public void CyclePlayerColor()
       {
           OnCyclePlayerColor?.Invoke(this);
        
          NetColorID.Value = Random.Range(0, BlockamiData.MaxNormalColorID);
       }
       
       
       
       
        public override void OnNetworkSpawn()             
        {
            BlockamiData[] allBlockamiData = Resources.LoadAll<BlockamiData>("");
            BlockamiData = System.Array.Find(allBlockamiData, data => data.name == "BlockamiData");
            
            enabled = IsServer;           //    set enabled false if not server.   This allows us to disable Update()
                                              // 'enabled' belongs to 'Behavior' class (parent of monobehavior).
                                              
            
                                              
                                              
            if (IsOwner)                                                               
            {
                LocalPlayerEntities.Instance.LocalPlayerController = this;                                   // register with LocalPlayerEntities if owner
                var ovrManager = FindObjectOfType<OVRManager>();
                var  clapDetect = ovrManager.GetComponentInChildren<ClapDetection>();
              if(clapDetect) clapDetect.OnClapDetected += ClapDetectOnOnClapDetected;

            }
            else
            {
                LocalPlayerEntities.Instance.GetPlayerObjects(OwnerClientId).PlayerController = this;            // if other player, fetch PlayerObjects for client ID from tracked dictionary in LocalPlayerEntities and populate it with this
            }

            IsInvulnerable.OnValueChanged += OnInvulnerabilityStateChanged;                                      // add rep notify func for IsInvulnerable 
            OnInvulnerabilityStateChanged(IsInvulnerable.Value, IsInvulnerable.Value);      // run rep notify func once on begin
        }

        private void ClapDetectOnOnClapDetected()
        {
          CyclePlayerColor();
        }


        private void Update()
        {
            if (!IsServer)
            {
                return;
            }

            // if (m_shieldActivated)   // while Shield is on
            // {
            //     m_shieldCharge.Value -= SHIELD_USAGE_RATE * Time.deltaTime;     // server handles decrease of shieldCharge for all clients
            //     if (m_shieldCharge.Value <= 0)                      // if shield is out of charge, stop it
            //     {
            //         m_shieldCharge.Value = 0;
            //         StopShield(m_shieldSide);
            //         m_shieldInResetMode.Value = true;
            //         m_shieldDisabled.Value = true;
            //         ArmatureLeft.DisableShield();
            //         ArmatureRight.DisableShield();
            //     }
            //
            //     ArmatureLeft.ShieldChargeLevel = m_shieldCharge.Value;
            //     ArmatureRight.ShieldChargeLevel = m_shieldCharge.Value;
            // }
            // else if (m_shieldInResetMode.Value)          // handle if shield is in Cooldown (ResetMode), count time til it should not be. during reset it wont be recharging yet
            // {
            //     m_shieldOffTimer.Value += Time.deltaTime;
            //     if (m_shieldOffTimer.Value >= SHIELD_RESET_TIME)
            //     {
            //         m_shieldOffTimer.Value = 0;
            //         m_shieldInResetMode.Value = false;
            //     }
            // }
            // else if (m_shieldCharge.Value < SHIELD_MAX_CHARGE)        // handle shield recharging
            // {
            //     m_shieldCharge.Value += SHIELD_CHARGE_RATE * Time.deltaTime;
            //     if (m_shieldCharge.Value >= SHIELD_MAX_CHARGE)
            //     {
            //         m_shieldCharge.Value = SHIELD_MAX_CHARGE;     // when shield reaches full charge, enable it to be used again
            //         if (m_shieldDisabled.Value)
            //         {
            //             m_shieldDisabled.Value = false;
            //             ArmatureLeft.EnableShield();            // these funcs are just Setters for a networked variable elsewhere
            //             ArmatureRight.EnableShield();
            //         }
            //     }
            //     ArmatureLeft.ShieldChargeLevel = m_shieldCharge.Value;
            //     ArmatureRight.ShieldChargeLevel = m_shieldCharge.Value;
            // }
        }
        
        
        
        private IEnumerator SetAvatarState()    // called everytime avatar materials need to be updated due to gameplay effects (invulnerability)
        {
            if (!m_avatar.IsSkeletonReady)
            {
                yield return new WaitUntil(() => m_avatar.IsSkeletonReady);       //wait til skeleton is ready
            }
            
            var material = m_avatar.Material;
            material.SetKeyword("ENABLE_GHOST_EFFECT", IsInvulnerable.Value);      // set a material parameter on avatar to match current IsInvulnerable.    Apply material to all sub meshes of avatar
            m_avatar.ApplyMaterial();
            
            // ArmatureLeft.SetGhostEffect(IsInvulnerable.Value);    // do same for Gloves
            // ArmatureRight.SetGhostEffect(IsInvulnerable.Value);
            //
            // GloveLeft.SetGhostEffect(IsInvulnerable.Value);
            // GloveRight.SetGhostEffect(IsInvulnerable.Value);
        }
        
        
        
        
        
        public void SetInvulnerability(Object setter)
        {
            if (IsServer)
            {
                _ = m_invulnerabilityActors.Add(setter);    // server,  set this as invulnerable if were not already
                if (!IsInvulnerable.Value)
                {
                    IsInvulnerable.Value = true;
                }
            }
        }

        public void RemoveInvulnerability(Object setter)
        {
            if (IsServer)
            {
                _ = m_invulnerabilityActors.Remove(setter);
                if (IsInvulnerable.Value && m_invulnerabilityActors.Count == 0)
                {
                    IsInvulnerable.Value = false;
                }
            }
        }

        public void ClearInvulnerability()
        {
            if (IsServer)
            {
                m_invulnerabilityActors.Clear();
                IsInvulnerable.Value = false;
            }
        }

        private void OnInvulnerabilityStateChanged(bool previousValue, bool newValue)   //rep notify function
        {
            m_collider.enabled = !newValue;
            _ = StartCoroutine(SetAvatarState());
            OnInvulnerabilityStateUpdatedEvent?.Invoke(newValue);
        }

        
        
      

        public void TriggerShield(Glove.GloveSide side)           // clients check if shield is disabled, if not, trigger shield via RPC to server - called from PlayerInputController
        {
            // if (m_shieldDisabled.Value)   // check if shield is disabled (out of charge)
            // {
            //     if (side == Glove.GloveSide.Right)
            //     {
            //         ArmatureRight.OnShieldNotAvailable();    
            //     }
            //     else
            //     {
            //         ArmatureLeft.OnShieldNotAvailable();
            //     }
            // }
            // else
            // {
            //     TriggerShieldServerRPC(side);    //server trigger shield
            // }
        }

    

        [ServerRpc]
        public void TriggerShieldServerRPC(Glove.GloveSide side)
        {
            // if (m_shieldActivated)
            // {
            //     if (m_shieldSide == side)
            //     {
            //         return;
            //     }
            //                                                          // We are switching sides, deactivate current side first
            //     {
            //         if (m_shieldSide == Glove.GloveSide.Right)
            //         {
            //             ArmatureRight.DeactivateShield();
            //         }
            //         else
            //         {
            //             ArmatureLeft.DeactivateShield();
            //         }
            //     }
            // }
            //
            // m_shieldActivated = true;
            // m_shieldSide = side;
            //
            // if (m_shieldSide == Glove.GloveSide.Right)
            // {
            //     ArmatureRight.ActivateShield();
            // }
            // else
            // {
            //     ArmatureLeft.ActivateShield();
            // }
        }

       
        public void OnShieldHit(Glove.GloveSide side)      // called by OnTriggerEnter (probably from Ball), ONLY server detects hits
        {
            // m_shieldCharge.Value = 0;
            // StopShield(side);
            // m_shieldInResetMode.Value = true;
            // m_shieldDisabled.Value = true;
            // ArmatureLeft.DisableShield();
            // ArmatureRight.DisableShield();
            // ArmatureLeft.ShieldChargeLevel = m_shieldCharge.Value;
            // ArmatureRight.ShieldChargeLevel = m_shieldCharge.Value;
        }
        
        
        private void StopShield(Glove.GloveSide side)
        {
            // if (!IsServer)
            // {
            //     return;
            // }
            //
            // if (!m_shieldActivated || side != m_shieldSide)
            // {
            //     return;
            // }
            //
            // m_shieldActivated = false;
            //
            // if (m_shieldSide == Glove.GloveSide.Right)
            // {
            //     ArmatureRight.DeactivateShield();
            // }
            // else
            // {
            //     ArmatureLeft.DeactivateShield();
            // }
        }

        
        [ServerRpc]
        public void StopShieldServerRPC(Glove.GloveSide side)
        {
            // StopShield(side);
        }

    }
}
