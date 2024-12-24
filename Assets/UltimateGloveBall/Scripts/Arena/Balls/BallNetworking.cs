// Copyright (c) Meta Platforms, Inc. and affiliates.
// Use of the material below is subject to the terms of the MIT License
// https://github.com/oculus-samples/Unity-UltimateGloveBall/tree/main/Assets/UltimateGloveBall/LICENSE

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.Utilities;
using UltimateGloveBall.Arena.Environment;
using UltimateGloveBall.Arena.Gameplay;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Player.Respawning;
using UltimateGloveBall.Arena.VFX;
using UltimateGloveBall.Design;
using Unity.Netcode;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace UltimateGloveBall.Arena.Balls
{
    /// <summary>
    /// Handles the network state of the ball as well as the game logic. This is the core of the ball behaviour,
    /// it handles throwing, ownership changes, audio on collision, vfx on collision, enabling and disabling the
    /// physics. Updating score and knocking out players.
    /// It has event that works with the BallStateSync class to keep the ball position synchronized between players.
    /// </summary>
    public class BallNetworking : NetworkBehaviour       // BallNetworking script handles all characteristics of balls that are not Gameplay UGB related, but more network specific
    {
        public event Action BallWasThrownLocally;
        public event Action<BallNetworking, bool> BallDied;
        public event Action BallShotFromServer;
        public event Action<float> OnBallShot;
        public event Action<ulong, ulong> OnOwnerChanged;
        [SerializeField, AutoSet] private Rigidbody m_rigidbody;
        [SerializeField, AutoSet] private BallStateSync m_stateSync;
        [SerializeField] private BallData m_ballData;                   // each BallPrefab has different BallData such as shootspeed, spin rate etc.
        [SerializeField] private BallAudio m_ballAudio;
        [SerializeField] private AudioClip m_onThrownAudioClip;

        [SerializeField] private List<MeshRenderer> m_ballRenderers;
        [SerializeField] private Material m_deadMaterial;
        [SerializeField] private AudioSource m_audioSource;

        [SerializeField] private BallSpinVisual m_ballSpinVisual;      // script that makes balls spin or stay still

        [SerializeField, AutoSet] private BallBehaviour m_ballBehaviour;

        private NetworkVariable<ulong> m_owner = new(ulong.MaxValue);   // im guessing this is m_ownerPlayerID ???  

        private NetworkVariable<bool> m_isOnSpawnerState = new(true);
        private ulong m_throwOwnerClientId = ulong.MaxValue;

        private bool
            m_ballIsDead; // When this is true our ball is officially killed and should no longer be able to be picked up or interact with objects

        private bool
            m_killBallOnNextCollision; // This should be true once we have detected that a ball has been shot from the server.

        private Material
            m_defaultMaterial; // Default material is picked from the first indexed ball mesh renderer's material.

        private Glove m_lastGrabber;

        public bool HasOwner => m_owner.Value != ulong.MaxValue;

        public NetworkedTeam.Team ThrownTeam { get; private set; } = NetworkedTeam.Team.NoTeam;    // which Team threw the ball most recently,  TeamA or TeamB

        public bool IsAlive => !m_ballIsDead;          // alias var for !m_ballIsDead

        public Glove CurrentGrabber { get; private set; } // Current grabber that holds the ball.

        public BallBehaviour BallBehaviour => m_ballBehaviour;     // alias var for m_ballBehaviour


        public override void OnNetworkSpawn()
        {
            m_owner.OnValueChanged += OnOwnershipUpdated;                       // bind rep notify functions to replicated variables  - m_owner, m_isOnSpawnerState
            m_isOnSpawnerState.OnValueChanged += OnSpawnerStateChanged;

            OnOwnershipUpdated(m_owner.Value, m_owner.Value);                        // run rep notify funcs one initial time
            OnSpawnerStateChanged(m_isOnSpawnerState.Value, m_isOnSpawnerState.Value);        // run rep notify funcs one initial time
        }

        private void OnSpawnerStateChanged(bool previousvalue, bool newvalue)       // rep notify for m_isOnSpawnerState  - check bool for if Ball is on spawner
        {
            if (newvalue)
            {
                gameObject.SetLayerToChilds(ObjectLayers.SPAWN_BALL);            // set collision channels for all child gO's
                m_ballSpinVisual.SetState(BallSpinVisual.SpinState.Spawned);
            }
            else
            {
                m_ballSpinVisual.SetState(BallSpinVisual.SpinState.Holding);    // ball is not on spawner, stop BallSpinVisual from spinning
                if (gameObject.GetComponent<ElectricBall>() != null)
                {
                    gameObject.SetLayerToChilds(ObjectLayers.FIRE_BALL);
                }
                else
                {
                    gameObject.SetLayerToChilds(ObjectLayers.BALL);
                }
            }
        }

        private void Awake()
        {
            m_ballSpinVisual.Init(m_ballData);
            m_defaultMaterial = m_ballRenderers[0].sharedMaterial;
        }

        private void OnEnable()
        {
            m_stateSync.DetectedBallShotFromServer += FromDetectedBallShotFromServer;        // bind to events on sibling script on gObject 'BallStateSync' when this script 'enabled'
        }

        private void FromDetectedBallShotFromServer()    // local clients will destroy balls on next collision, and Not wait for server to NetworkDestroy it?
        {
            m_killBallOnNextCollision = true;
        }

        private void OnDisable()
        {
            m_stateSync.DetectedBallShotFromServer -= FromDetectedBallShotFromServer;
            ResetBall();
        }

        private void FixedUpdate()
        {
            // Lock the rigidbody in place when velocity becomes low
            if (IsOwner && m_rigidbody.velocity.magnitude < 0.1f)        // settle the rigid body if its at really low velocity
            {
                m_rigidbody.velocity = Vector3.zero;
                m_rigidbody.angularVelocity = Vector3.zero;
            }

            if (m_ballIsDead || !(transform.position.y < -1)) return;               // this line's return statement is just guarding the KillBall method a line later

            KillBall(true);       // if ball is not already dead, and transform position is higher than -1, kill it ???
        }

        private void OnCollisionEnter(Collision collision)      // when balls hit Anything
        {
            if (m_ballIsDead)
            {
                m_audioSource.PlayOneShot(m_ballAudio.BallBounceClip);        // if ball is already 'dead' and not a threat,  only do some bounce sounds
                return;
            }

            m_ballSpinVisual.SetState(BallSpinVisual.SpinState.Hit);    // tell BallSpinVisual, we hit something   - so we dont spin anymore
            var isHitSfxPlayed = false;

            // Process Collision SFX and VFX                    // check what we hit?  obstacle, sheild or player - play VFX locally, and sound
            var go = collision.gameObject;
            if (!HasOwner && go.GetComponent<Obstacle>() != null)   
            {
                m_audioSource.PlayOneShot(m_ballAudio.BallBounceClip);      // check if we hit an Obstacle
                isHitSfxPlayed = true;
                var contact = collision.GetContact(0);
                var hitPos = collision.collider.ClosestPointOnBounds(contact.point);
                VFXManager.Instance.PlayHitVFX(hitPos, contact.normal);
            }
            else if (!HasOwner && go.GetComponent<Shield>() != null)     // check if we hit a Shield
            {
                m_audioSource.PlayOneShot(m_ballAudio.BallHitShieldClip);
                isHitSfxPlayed = true;
                var contact = collision.GetContact(0);
                VFXManager.Instance.PlayHitVFX(contact.point, contact.normal);
            }

            var hitPlayer = go.TryGetComponent<NetworkedTeam>(out var teamComp);     // check if we hit a player
            if (hitPlayer)
            {
                m_audioSource.PlayOneShot(m_ballAudio.BallHitClip);
                isHitSfxPlayed = true;
                var contact = collision.GetContact(0);
                VFXManager.Instance.PlayHitVFX(contact.point, contact.normal);
            }

            if (!isHitSfxPlayed)
            {
                m_audioSource.PlayOneShot(m_ballAudio.BallBounceClip);
            }

            if (!IsServer)
            {
                if (m_killBallOnNextCollision)
                {
                    KillBall(false);                // if m_killBallOnNextCollision has been set on client, kill the ball asap
                }

                return;        /// nonServer Clients STOP here
            }

            if (ThrownTeam == NetworkedTeam.Team.NoTeam)
            {
                return;       // if ball has NoTeam for some reason,  dont do anything
            }

            // deal with hit player
            if (hitPlayer)
            {
                if (teamComp.MyTeam != ThrownTeam)
                {
                    GameState.Instance.Score.UpdateScore(ThrownTeam, 1);
                    go.GetComponent<RespawnController>().KnockOutPlayer();    // insta-kill player
                }
            }

            // When ball has collided we kill it on the server  //   it is not destroyed, it is just irrelevant
            KillBall(true);


            ThrownTeam = NetworkedTeam.Team.NoTeam;
        }

        private void OnOwnershipUpdated(ulong previousValue, ulong newValue)    // rep notify func for m_Owner
        {
            if (IsOwner && m_lastGrabber != null && newValue == OwnerClientId)     // if new owner is the local player
            {
                // If a grab was allowed by server we can update the grabbers and assign this ball to our grabber.
                CurrentGrabber = m_lastGrabber;
                CurrentGrabber.AssignBall(this);
                m_lastGrabber = null;
            }

            if (previousValue != newValue && newValue != ulong.MaxValue)   // if owner has changed
            {
                m_audioSource.PlayOneShot(m_ballAudio.BallGrabbedClip);         // play a sound whenever ownership has changed
            }

            OnOwnerChanged?.Invoke(previousValue, newValue);         // invoke an action, in case any other script cares we changed ownership
        }

        /// <summary>
        ///     Call this to reset all states, local and networked.
        /// </summary>
        public void ResetBall()
        {
            m_stateSync.Reset(); // Make sure the state syncing is reset as well

            if (CurrentGrabber != null &&
                CurrentGrabber.CurrentBall ==
                this) // If we for any reason have a grabber assigned when resetting, we make sure it unassigns this ball
                CurrentGrabber.SetCurrentBall(null);

            CurrentGrabber = null;

            // Reset all local data, physics and visuals
            m_ballIsDead = false;
            m_killBallOnNextCollision = false;
            ThrownTeam = NetworkedTeam.Team.NoTeam;
            m_throwOwnerClientId = ulong.MaxValue;
            m_lastGrabber = null;
            EnablePhysics(false);
            UpdateVisuals(false);

            if (m_ballBehaviour != null)
            {
                m_ballBehaviour.ResetBall();
            }
        }

        public void SetSpawnState(bool onSpawner)   // set bOnSpawner true or false
        {
            m_isOnSpawnerState.Value = onSpawner;
            ResetOwner();
        }

        public void TryGrabBall(Glove grabber)       // this is run locally by clients
        {
            if (m_ballIsDead) return;

            if (grabber.HasBall) // If the glove already has a ball we do not try to grab this one.
                return;

            m_lastGrabber = grabber;
            TakeOwnershipServerRpc(NetworkManager.LocalClientId);
        }

        public void Drop()            // run locally by clients
        {
            DropBallServerRpc(transform.position);
        }

        public void Throw(Vector3 direction, float chargeUpPct)
        {
            var ballPositionOnThrow = transform.position;
            BallWasThrownLocally?.Invoke();

            CurrentGrabber.SetCurrentBall(null); // Make the grabber release the ball
            CurrentGrabber = null;
            ThrowServerRpc(ballPositionOnThrow, direction, chargeUpPct);           // tell the server to throw ball

            m_ballSpinVisual.SetState(BallSpinVisual.SpinState.Thrown, chargeUpPct);
            m_throwOwnerClientId = m_owner.Value;
            m_audioSource.PlayOneShot(m_onThrownAudioClip);

            // Anyone but server will shoot immediately and catch up with server packets. Server shoots in above Throw-RPC         // doesnt this mean Catch-Down with server packets not up??
            if (!IsServer)
                ShootBall(ballPositionOnThrow, direction, chargeUpPct);    //  throw ball locally, and expect ball position to get corrected by server throw
        }

        // This is for balls that are spawned mid air   (-< original comment)           // for 3 way powerup balls, balls may be spawned midair
        public void LaunchBall(Vector3 direction, NetworkedTeam.Team team, float chargeUpPct)
        {
            if (!IsServer) return;

            m_isOnSpawnerState.Value = false;
            ThrownTeam = team;
            BallWasThrownLocally?.Invoke();
            ShootBall(transform.position, direction, chargeUpPct);
            BallShotFromServer?.Invoke();
        }

        private void ResetOwner()
        {
            m_owner.Value = ulong.MaxValue;
        }

        [ServerRpc(RequireOwnership = false)]            // this attribute means server can be asked to run this, even when this NetworkObject is not owned by server
        private void DropBallServerRpc(Vector3 origin)        // drop is different from Throw i believe. All dropped balls freeze position and fall to ground
        {
            var thrower = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(m_owner.Value);      // var thrower - is a NetworkObject, likely a PlayerObject - m_owner.Value is a ulong ID (a NetworkObject ID)
            ThrownTeam = thrower.TryGetComponent<NetworkedTeam>(out var team) ? team.MyTeam : NetworkedTeam.Team.NoTeam;   // get the Throwers Team from getting a NetworkedTeamComponent

            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);         // literally change ownership of this script back to the server,   Dropped balls belong to the server
            ResetOwner();
            // Release the ball from following the previous grabber
            CurrentGrabber.SetCurrentBall(null);
            CurrentGrabber = null;

            var ballTransform = transform;
            ballTransform.SetParent(null);   // get the transform in World Space ??  ( in unity you can set Parents of transforms )

            // drop here
            ballTransform.position = origin;
            transform.forward = Vector3.down;
            EnablePhysics(true);
            m_rigidbody.velocity = Vector3.zero;
            m_rigidbody.angularVelocity = Vector3.zero;

            BallShotFromServer?.Invoke();
        }

        [ServerRpc(RequireOwnership = false)]           // server can be asked to run this, even when this NetworkObject is not owned by server
        private void ThrowServerRpc(Vector3 origin, Vector3 direction, float chargeUpPct)
        {
            var thrower = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(m_owner.Value);

            if (thrower == null)
            {
                Debug.LogWarning(
                    $"Ball was thrown but no network object was found for owner. Owner ID: {m_owner.Value}");
                return;
            }

            if (thrower.TryGetComponent<NetworkedTeam>(out var team))
                ThrownTeam = team.MyTeam;
            else
                Debug.LogWarning($"Ball was thrown but no team was found on the owner. Owner ID: {m_owner.Value}");

            // give back to server
            NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
            ResetOwner();
            // Release the ball from following the previous grabber
            if (CurrentGrabber != null)
            {
                CurrentGrabber.SetCurrentBall(null);
                CurrentGrabber = null;
            }

            ShootBall(origin, direction, chargeUpPct);             // let server know a networked Origin and direction,  Ball will be given a default rotation looking-at throw direction before Server shoots it

            BallShotFromServer?.Invoke();
        }

        private void ShootBall(Vector3 origin, Vector3 direction, float chargeUpPct)       //fairly sure only Server ever runs this, despite the check here for "IsServer"   //actually Client does use this also
        {
            // throw the ball in the desired direction
            var ballTransform = transform;
            ballTransform.position = origin;
            ballTransform.forward = direction;
            EnablePhysics(true);
            m_rigidbody.angularVelocity = Vector3.zero;
            var ballForce = Mathf.Lerp(m_ballData.MinThrowSpeed, m_ballData.MaxThrowSpeed, chargeUpPct);
            m_rigidbody.velocity = direction.normalized * ballForce;
            OnBallShot?.Invoke(chargeUpPct);
            if (IsServer)
            {
                OnBallShotClientRPC(chargeUpPct);
            }
        }

        [ClientRpc]
        private void OnBallShotClientRPC(float chargeUpPct)      // an rpc that gets called on Clients when they themselves Shoot,   Clients will have also run their own local ShootBall as well
        {
            m_ballSpinVisual.SetState(BallSpinVisual.SpinState.Thrown, chargeUpPct);

            if (m_throwOwnerClientId != NetworkManager.Singleton.LocalClientId)
            {
                m_audioSource.PlayOneShot(m_onThrownAudioClip);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void TakeOwnershipServerRpc(ulong clientId)         // this func takes ownership of a ball on behalf of some client, and also sets the current ball as currently grabbed by a player's Glove
        {
            if (m_ballIsDead) return;
            if (HasOwner) return;

            if (NetworkManager.ConnectedClients.TryGetValue(clientId, out var client))    // try to get a NetworkID for local player?
            {
                var gloves = client.OwnedObjects.Where(o => o.name.Contains("GloveHand"))         // NetworkManager keeps list of NetworkClients, who can have a list of owned objects  OwnedObjects -   this has GetByPredicate syntax
                    .Select(o => o.GetComponent<GloveNetworking>())                              // from OwnedObjects return a filtered list of gO's that have a GloveNetworking on them and name.Contains("GloveHand")
                    .ToList();
                if (gloves.Count > 0)
                {
                    var sqrDist = float.MaxValue;
                    GloveNetworking grabber = null;
                    foreach (var glove in gloves)
                    {
                        var distanceToGlove = Mathf.Min(sqrDist,
                            Vector3.SqrMagnitude(glove.transform.position - transform.position));

                        if (!(distanceToGlove < sqrDist)) continue;

                        grabber = glove;
                        sqrDist = distanceToGlove;
                    }                            // get the nearest glove to this ball location as 'grabber'       // this is i guess faster than passing this script networkobject's id in with this rpc ??

                    if (grabber != null)
                    {
                        // The server sets the grabber and follow transform at this point
                        CurrentGrabber = grabber.GetComponent<Glove>();
                        CurrentGrabber.SetCurrentBall(this);
                        EnablePhysics(false);
                        m_rigidbody.Sleep();
                    }

                    SetSpawnState(false);     // if the ball is grabbed, its not on a Spawner

                    NetworkObject.ChangeOwnership(clientId);       // Do the actual change of ownership of this script
                    m_owner.Value = clientId;
                }
            }
        }

        public void EnablePhysics(bool enable)
        {
            m_rigidbody.isKinematic = !enable;    // Physics objects should not be Kinematic
            m_rigidbody.useGravity = enable;
        }

        private void UpdateVisuals(bool isDead)
        {
            foreach (var ball in m_ballRenderers)          // ball may have multiple ballRenderers such as the TripleBall
                ball.sharedMaterial = isDead ? m_deadMaterial : m_defaultMaterial;
        }

        public void KillBall(bool announceDeath, bool dieInstantly = false)      // this gets used by both server and clients. We handle visuals here and after that we... 
        // invoke ballDied which gets picked up by BallSpawner, which gets NetworkManager to put balls back into the pool
        {
            m_ballIsDead = true;

            UpdateVisuals(true);
            if (announceDeath)
            {
                // We tell local server scripts our ball has died
                BallDied?.Invoke(this, dieInstantly);            // bdieInstantly will circumvent the bKillBallOnNextCollision default behavior of OnCollisionEnter
            }
        }
    }
}