using System.Collections.Generic;
using Blockami.Scripts;
using TMPro;
using UltimateGloveBall.App;
using UltimateGloveBall.Arena.Player;
using UltimateGloveBall.Arena.Player.Menu;
using UltimateGloveBall.Arena.Spectator;
using UltimateGloveBall.MainMenu;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BlockamiDebugMenu : BasePlayerMenuView    // PlayerInGameMenu
{
     [SerializeField] private Slider m_NormalSpawnRateSlider;
     [SerializeField] private TMP_Text m_NormalSpawnRateValueText;

        [SerializeField] private Slider m_FrenzySpawnRateSlider;
         [SerializeField] private TMP_Text m_FrenzySpawnRateValueText;

        [HideInInspector] [SerializeField] private Slider m_crowdVolumeSlider;
        [HideInInspector]  [SerializeField] private TMP_Text m_crowdVolumeValueText;

         [SerializeField] private Toggle BlockBounceBackToggle;
          [SerializeField] private Toggle m_CyclePlayerColorOnDraw;
         [HideInInspector]  [SerializeField] private Toggle m_locomotionVignetteToggle;

        [Header("Spawn Cat")]
        [SerializeField] private Button m_spawnCatButton;

        [Header("Spectator")]
        [SerializeField] private Button m_switchSideButton;

        public BlockamiData m_blockamiData;

        private void Start()
        {
            m_NormalSpawnRateSlider.onValueChanged.AddListener(OnNormalSpawnRateSliderChanged);
            m_FrenzySpawnRateSlider.onValueChanged.AddListener(OnFrenzySpawnRateSliderChanged);
            
            BlockBounceBackToggle.onValueChanged.AddListener(OnBlockBounceBackChanged);
            m_CyclePlayerColorOnDraw.onValueChanged.AddListener(CyclePlayerColorOnDrawChanged);
            
            // m_crowdVolumeSlider.onValueChanged.AddListener(OnCrowdSliderChanged);
            // m_locomotionVignetteToggle.onValueChanged.AddListener(OnLocomotionVignetteChanged);
        }

        private void OnEnable()
        {
            var settings = GameSettings.Instance;
            var audioController = AudioController.Instance;
            
            m_blockamiData = BlockamiData.Instance;
            m_NormalSpawnRateSlider.value = m_blockamiData.DefaultSpawnRate;
            m_NormalSpawnRateValueText.text = m_blockamiData.DefaultSpawnRate.ToString("N2") ;
            m_FrenzySpawnRateSlider.value = m_blockamiData.FrenzySpawnRate;
            m_FrenzySpawnRateValueText.text = m_blockamiData.FrenzySpawnRate.ToString("N2") ;
            
            
            BlockBounceBackToggle.isOn = m_blockamiData.LetIncorrectPlayerCubesBounceBack;
            m_CyclePlayerColorOnDraw.isOn = m_blockamiData.CycleColorsOnDraw;
            
            // m_switchSideButton.gameObject.SetActive(LocalPlayerState.Instance.IsSpectator);
            // m_spawnCatButton.gameObject.SetActive(!LocalPlayerState.Instance.IsSpectator &&
            //     !LocalPlayerState.Instance.SpawnCatInNextGame && GameSettings.Instance.OwnedCatsCount > 0);
            //
            //
            // m_crowdVolumeSlider.value = audioController.CrowdVolume;
            // m_crowdVolumeValueText.text = audioController.CrowdVolumePct.ToString("N2") + "%";
            //
            //
            
            // m_locomotionVignetteToggle.isOn = settings.UseLocomotionVignette;

            #if UNITY_EDITOR
            gameObject.SetActive(false); 
            // dont allow changing values of the scriptable object in editor
#endif

        }
        
        
        
        private void OnNormalSpawnRateSliderChanged(float val)
        {
            m_blockamiData.DefaultSpawnRate = val;
            m_NormalSpawnRateValueText.text = m_blockamiData.DefaultSpawnRate.ToString("N2") ;
            
        }

        private void OnFrenzySpawnRateSliderChanged(float val)
        {
            m_blockamiData.FrenzySpawnRate = val;
            m_FrenzySpawnRateValueText.text = m_blockamiData.FrenzySpawnRate.ToString("N2") ;
        }
        
        
        private void OnBlockBounceBackChanged(bool val)
        {
            m_blockamiData.LetIncorrectPlayerCubesBounceBack = val;
        }
        
        
        private void CyclePlayerColorOnDrawChanged(bool val)
        {
            m_blockamiData.CycleColorsOnDraw = val;
     
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

      

        private void OnCrowdSliderChanged(float val)
        {
            // var audioController = AudioController.Instance;
            // audioController.SetCrowdVolume(val);
            // m_crowdVolumeValueText.text = audioController.CrowdVolumePct.ToString("N2") + "%";
        }

       


        private void OnLocomotionVignetteChanged(bool val)
        {
            GameSettings.Instance.UseLocomotionVignette = val;
        }
    }
    

