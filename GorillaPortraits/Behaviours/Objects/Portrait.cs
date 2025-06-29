using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

#if PLUGIN
using GorillaPortraits.Models;
using GorillaTag.Audio;
using GorillaExtensions;
using GorillaLocomotion;
using UnityEngine.XR;
using GorillaPortraits.Tools;
using GorillaPortraits.Models.StateMachine;
#endif

namespace GorillaPortraits.Behaviours.Objects
{
#if PLUGIN
    [DefaultExecutionOrder(100)]
    public class Portrait : HoldableObject
#else
    public class Portrait : MonoBehaviour
#endif
    {
        public GameObject stand;

        public AudioClip placeSound;

        public Vector3 leftGrabPosition, rightGrabPosition;

        public Vector3 leftGrabEulerAngles, rightGrabEulerAngles;

        [Tooltip("The background for the photo, Photo.Colour is applied here"), FormerlySerializedAs("background")]
        public Image backgroundImage;

        [Tooltip("The main photo"), FormerlySerializedAs("photo")]
        public Image photoImage;

        [Header("Application Panel")]

        [Tooltip("The game object for the photo apply panel")]
        public GameObject applyPanel;

        [Tooltip("The amount of photos visible for each page")]
        public int pageCapacity;

        [Tooltip("The array of buttons used to choose a photo, length should be page capacity")]
        public PhotoButton[] selection;

        [Tooltip("A navigation button")]
        public PushButton nextPage, prevPage;

        public TMP_Text pageText;

#if PLUGIN

        public StateMachine<PortraitState_Base> portraitState;

        public StateMachine<PortraitMenu_Base> portraitMenu;
        public PortraitMenu_Display portraitMainDisplay;

        public Shelf shelf;

        private GTPlayer player;

        public bool isSwapped, wasSwappedLeft;

        public int currentPage = 0;

        public Photo currentPhoto;

        public void Awake()
        {
            portraitMenu = new StateMachine<PortraitMenu_Base>();
            portraitMainDisplay = new PortraitMenu_Display(this);

            portraitState = new StateMachine<PortraitState_Base>();
            portraitState.OnStateChanged += StateChanged;

            stand.SetActive(false);
        }

        public void Start()
        {
            player = GTPlayer.Instance;
        }

        public void Update()
        {
            portraitState?.Update();
            portraitMenu?.Update();
        }

        public void LateUpdate()
        {
            if (ApplicationQuittingState.IsQuitting) return;

            Vector3 leftHandPosition = player.leftHandFollower.position;
            Vector3 rightHandPosition = player.rightHandFollower.position;

            if (portraitState.CurrentState is PortraitState_InHand portraitHandState)
            {
                bool isLeftHand = portraitHandState.isLeftHand;

                if (isSwapped && (wasSwappedLeft ? ControllerInputPoller.GetGrabRelease(XRNode.LeftHand) : ControllerInputPoller.GetGrabRelease(XRNode.RightHand)))
                {
                    Logging.Info("Revoking swap state");
                    isSwapped = false;
                }

                if (isLeftHand
                    && ControllerInputPoller.GetGrab(XRNode.RightHand)
                    && !isSwapped
                    && !wasSwappedLeft
                    && !EquipmentInteractor.instance.disableRightGrab
                    && EquipmentInteractor.instance.rightHandHeldEquipment is null
                    && InRange(rightHandPosition))
                {
                    Logging.Info("Swapping from left to right hand");
                    isSwapped = true;
                    wasSwappedLeft = true;
                    portraitState.SwitchState(new PortraitState_InHand(this, false, false));
                    return;
                }

                if (isLeftHand && ControllerInputPoller.GetGrabRelease(XRNode.LeftHand))
                {
                    Logging.Info("Releasing from left hand");
                    isSwapped = false;
                    if (CheckSurroundings()) portraitState.SwitchState(new PortraitState_Loose(this, true));
                    return;
                }

                if (!isLeftHand
                    && ControllerInputPoller.GetGrab(XRNode.LeftHand)
                    && !isSwapped
                    && wasSwappedLeft
                    && !EquipmentInteractor.instance.disableLeftGrab
                    && EquipmentInteractor.instance.leftHandHeldEquipment is null
                    && InRange(leftHandPosition))
                {
                    Logging.Info("Swapping from right to left hand");
                    isSwapped = true;
                    wasSwappedLeft = false;
                    portraitState.SwitchState(new PortraitState_InHand(this, true, false));
                    return;
                }

                if (!isLeftHand && ControllerInputPoller.GetGrabRelease(XRNode.RightHand))
                {
                    Logging.Info("Releasing from right hand");
                    isSwapped = false;
                    if (CheckSurroundings()) portraitState.SwitchState(new PortraitState_Loose(this, false));
                    return;
                }
            }
            else
            {
                // check surroundings (whether portrait is on shelf or being disposed)

                if (portraitState.HasState && portraitState.CurrentState is PortraitState_Loose)
                {
                    CheckSurroundings();
                }

                // check hands for grabbing the portrait

                if (ControllerInputPoller.GetGrab(XRNode.LeftHand)
                    && !EquipmentInteractor.instance.disableLeftGrab
                    && EquipmentInteractor.instance.leftHandHeldEquipment is null
                    && InRange(leftHandPosition))
                {
                    Logging.Info("Grabbing with left hand");
                    wasSwappedLeft = false;
                    portraitState.SwitchState(new PortraitState_InHand(this, true, false));
                    return;
                }

                if (ControllerInputPoller.GetGrab(XRNode.RightHand)
                    && !EquipmentInteractor.instance.disableRightGrab
                    && EquipmentInteractor.instance.rightHandHeldEquipment is null
                    && InRange(rightHandPosition))
                {
                    Logging.Info("Grabbing with right hand");
                    wasSwappedLeft = true;
                    portraitState.SwitchState(new PortraitState_InHand(this, false, false));
                    return;
                }

            }
        }

        public void StateChanged(PortraitState_Base newState)
        {
            if (newState is null) return;

            if (newState is PortraitState_InHand && (portraitMenu.CurrentState is null || portraitMenu.CurrentState == portraitMainDisplay))
            {
                portraitMenu.SwitchState(new PortraitMenu_Selection(this, 4, PhotoManager.Instance.GetPhotos()));
            }
            else if (newState is not PortraitState_InHand && (portraitMenu.CurrentState is null || portraitMenu.CurrentState != portraitMainDisplay))
            {
                portraitMenu.SwitchState(portraitMainDisplay);
            }
        }

        /// <returns>Whether the surroundings are clear</returns>
        public bool CheckSurroundings()
        {
            // check basket

            if (shelf.basket.InReserveArea(transform.position))
            {
                if (shelf.basket.depositSounds is AudioClip[] sounds && sounds.Length > 0) GTAudioOneShot.Play(sounds.GetRandomItem(), transform.position, 0.5f);
                Destroy(gameObject);
                return false;
            }

            // check shelf

            if (shelf.GetPlacementArea(transform.position) is BoxCollider sensor && sensor)
            {
                if (portraitState.CurrentState is not PortraitState_OnDisplay)
                {
                    if (placeSound is not null && placeSound) GTAudioOneShot.Play(placeSound, transform.position, 0.5f);
                    portraitState.SwitchState(new PortraitState_OnDisplay(this, sensor));
                }
                return false;
            }

            return true;
        }

        public bool InRange(Vector3 point)
        {
            Vector3 portraitPosition = transform.position + transform.rotation * Vector3.zero;
            float grabDistance = 0.15f * GTPlayer.Instance.scale;
            return (portraitPosition - point).IsShorterThan(grabDistance);
        }

        #region Leftover HoldableObject methods
        public override void DropItemCleanup()
        {
            Logging.Warning("DropItemCleanup");
        }

        public override void OnGrab(InteractionPoint pointGrabbed, GameObject grabbingHand)
        {
            Logging.Warning("OnGrab");
        }

        public override void OnHover(InteractionPoint pointHovered, GameObject hoveringHand)
        {
            Logging.Warning("OnHover");
        }
        #endregion
#endif
    }
}
