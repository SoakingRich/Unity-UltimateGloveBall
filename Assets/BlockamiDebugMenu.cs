using System.Collections.Generic;
using Blockami.Scripts;
using ReadyPlayerMe.Samples.AvatarCreatorWizard;
using TMPro;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Player.Menu;
using UltimateGloveBall.Arena.Spectator;
using UltimateGloveBall.MainMenu;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BlockamiDebugMenu : BasePlayerMenuView    // PlayerInGameMenu
{
     [SerializeField] private Slider m_NormalSpawnRateSlider;
     [SerializeField] private TMP_Text m_NormalSpawnRateValueText;

        [SerializeField] private Slider m_FrenzySpawnRateSlider;
         [SerializeField] private TMP_Text m_FrenzySpawnRateValueText;

        [SerializeField] private Slider m_DistanceSlider;
        [SerializeField] private TMP_Text m_DistanceSliderValueText;

        [SerializeField] private Slider m_CubeSpeedSlider;
        [SerializeField] private TMP_Text m_CubeSpeedSliderValueText;
        
         [SerializeField] private Toggle BlockBounceBackToggle;
          [SerializeField] private Toggle m_CyclePlayerColorOnDraw;
          
       [SerializeField] private Toggle m_SimpleMaterialsToggle;
       
       [SerializeField] private Toggle m_GazeTracking;

        [Header("Spawn Cat")]
        [SerializeField] private Button m_spawnCatButton;

        [Header("Spectator")]
        [SerializeField] private Button m_switchSideButton;


        private void Start()
        {
            m_NormalSpawnRateSlider.onValueChanged.AddListener(OnNormalSpawnRateSliderChanged);
            
            m_CubeSpeedSlider.onValueChanged.AddListener(OnCubeSpeedSliderChanged);
            
            m_FrenzySpawnRateSlider.onValueChanged.AddListener(OnFrenzySpawnRateSliderChanged);
            
            BlockBounceBackToggle.onValueChanged.AddListener(OnBlockBounceBackChanged);
            
            m_CyclePlayerColorOnDraw.onValueChanged.AddListener(CyclePlayerColorOnDrawChanged);
            
            m_DistanceSlider.onValueChanged.AddListener(OnDistanceSliderChanged);
            m_GazeTracking.onValueChanged.AddListener(OnGazeTrackingChanged);
           
            
//             m_SimpleMaterialsToggle.onValueChanged.AddListener(OnSimpleMaterialsChanged);
        }

        private void OnEnable()
        {
            var settings = GameSettings.Instance;
            var audioController = AudioController.Instance;
            
           
            m_NormalSpawnRateSlider.value = BlockamiData.Instance.DefaultSpawnRate;
            m_NormalSpawnRateValueText.text = BlockamiData.Instance.DefaultSpawnRate.ToString("N2") ;
            
            m_FrenzySpawnRateSlider.value = BlockamiData.Instance.FrenzySpawnRate;
            m_FrenzySpawnRateValueText.text = BlockamiData.Instance.FrenzySpawnRate.ToString("N2") ;
            
            m_CubeSpeedSlider.value = BlockamiData.Instance.PlayerCubeMoveSpeed;
            m_CubeSpeedSliderValueText.text = BlockamiData.Instance.PlayerCubeMoveSpeed.ToString("N2") ;
            
            
            var drawingGrids = FindObjectsOfType<DrawingGrid>();      // write to distanceslider, the initial value of DrawingGridDistance
            var dg = drawingGrids[0];
            var positionOnAxis = Vector3.Scale(dg.gameObject.transform.localPosition, dg.MoveDirection);
            var maxfloat = positionOnAxis.magnitude;            
            m_DistanceSlider.value = maxfloat;
            m_DistanceSliderValueText.text = m_DistanceSlider.value.ToString("N2");
            
            BlockBounceBackToggle.isOn = BlockamiData.Instance.LetIncorrectPlayerCubesBounceBack;
            m_CyclePlayerColorOnDraw.isOn = BlockamiData.Instance.CycleColorsOnDraw;
            
             
            m_GazeTracking.isOn = BlockamiData.Instance.BoxingEnabled;
            
            
            
            // m_switchSideButton.gameObject.SetActive(LocalPlayerState.Instance.IsSpectator);
            // m_spawnCatButton.gameObject.SetActive(!LocalPlayerState.Instance.IsSpectator &&
            //     !LocalPlayerState.Instance.SpawnCatInNextGame && GameSettings.Instance.OwnedCatsCount > 0);
            //
            //
          
           
            
         //    m_SimpleMaterialsToggle.isOn = settings.UseLocomotionVignette;

//             #if UNITY_EDITOR
//             gameObject.SetActive(false); 
//             // dont allow changing values of the scriptable object in editor
// #endif

        }
        
        
        
        private void OnNormalSpawnRateSliderChanged(float val)
        {
            BlockamiData.Instance.DefaultSpawnRate = val;
            m_NormalSpawnRateValueText.text = BlockamiData.Instance.DefaultSpawnRate.ToString("N2") ;
            
        }

        private void OnCubeSpeedSliderChanged(float val)
        {
            BlockamiData.Instance.PlayerCubeMoveSpeed = val;
            m_CubeSpeedSliderValueText.text = BlockamiData.Instance.PlayerCubeMoveSpeed.ToString("N2") ;
            
        }
        
        private void OnDistanceSliderChanged(float val)
        {
            var drawingGrids = FindObjectsOfType<DrawingGrid>();
            foreach (var dg in drawingGrids)
            {
                Vector3 finalLoc = dg.MoveDirection * val * -1.0f;
                finalLoc.y = dg.gameObject.transform.position.y;
                dg.gameObject.transform.position = finalLoc;
            }
            
            m_DistanceSliderValueText.text = m_DistanceSlider.value.ToString("N2");

            CancelInvoke();
            Invoke("InvokedRespawnAllPlayers",2.0f);
          
          
        }

        private void OnGazeTrackingChanged(bool val)
        {
            BlockamiData.Instance.BoxingEnabled = val;
            var eyetrack = FindObjectOfType<EyeTracking>(true);
            if (eyetrack)
            {
                eyetrack.transform.gameObject.SetActive(val);
            }
        }
        
        private void OnFrenzySpawnRateSliderChanged(float val)
        {
            BlockamiData.Instance.FrenzySpawnRate = val;
            m_FrenzySpawnRateValueText.text = BlockamiData.Instance.FrenzySpawnRate.ToString("N2") ;
        }
        
        
        private void OnBlockBounceBackChanged(bool val)
        {
            BlockamiData.Instance.LetIncorrectPlayerCubesBounceBack = val;
        }
        
        
        private void CyclePlayerColorOnDrawChanged(bool val)
        {
            BlockamiData.Instance.CycleColorsOnDraw = val;
     
        }
        
        

        public void OnSwitchSidesButtonClicked()     // get the SpectatorNetwork comp on our client owned PlayerAvatarEntity
        {
            var spectatorNet =
                NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject().GetComponent<SpectatorNetwork>();

            spectatorNet.RequestSwitchSide();
        }

        public void OnSpawnCatButtonClicked()
        {
            if (GameSettings.Instance.OwnedCatsCount > 0)
            {
                GameSettings.Instance.OwnedCatsCount--;
                LocalPlayerState.Instance.SpawnCatInNextGame = true;
            }
            m_spawnCatButton.gameObject.SetActive(false);
        }


        [ContextMenu("DebugOnDistanceSliderChanged")]
        public void DebugOnDistanceSliderChanged()
        {
            OnDistanceSliderChanged(m_DistanceSlider.value);
        }


       

        void InvokedRespawnAllPlayers()
        {
            UltimateGloveBall.Arena.Gameplay.GameManager._instance.RespawnAllPlayers();
        }

       


        private void OnSimpleMaterialsChanged(bool val)
        {
           // do a function on all found instances of MaterialSwitcher.cs ??
        }
    }
    

