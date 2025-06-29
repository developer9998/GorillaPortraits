using UnityEngine;

#if PLUGIN
using System.Linq;
using GorillaLocomotion;
using System;
using UnityEngine.XR;
using GorillaPortraits.Tools;
#endif

namespace GorillaPortraits.Behaviours.Objects
{
    [DisallowMultipleComponent, DefaultExecutionOrder(200)]
    public class Basket : MonoBehaviour
    {
        [Tooltip("The prefab of the photo grabbed from the basket")]
        public GameObject portraitPrefab;

        [Tooltip("The sensor used to gauge whether a hand is in the basket")]
        public BoxCollider grabSensor;

        public BoxCollider depositSensor;

        public AudioClip[] grabSounds, depositSounds;

#if PLUGIN

        public event Action<bool> OnInteraction;

        private GTPlayer player;
        private EquipmentInteractor interactor;

        private bool wasLeftGrab, wasLeftInside, wasRightGrab, wasRightInside;

        private LayerMask layerMask = 1 << (int)UnityLayer.Prop;

        private readonly Collider[] hitColliders = new Collider[15];
        private int numColliders;

        public void Start()
        {
            player = GTPlayer.Instance;
            interactor = EquipmentInteractor.instance;
        }

        public void LateUpdate()
        {
            if (ApplicationQuittingState.IsQuitting) return;

            bool isLeftGrab = ControllerInputPoller.GetGrab(XRNode.LeftHand) && interactor.leftHandHeldEquipment is null && !interactor.disableLeftGrab;
            if (isLeftGrab && isLeftGrab != wasLeftGrab)
            {
                bool isLeftInside = InGrabArea(player.leftHandFollower.position);
                if (isLeftInside && isLeftInside == wasLeftInside)
                {
                    Logging.Info("Grabbed portrait from basket using left hand");
                    OnInteraction?.Invoke(true);
                }
            }

            bool isRightGrab = ControllerInputPoller.GetGrab(XRNode.RightHand) && interactor.rightHandHeldEquipment is null && !interactor.disableRightGrab;
            if (isRightGrab && isRightGrab != wasRightGrab)
            {
                bool isRightInside = InGrabArea(player.rightHandFollower.position);
                if (isRightInside && isRightInside == wasRightInside)
                {
                    Logging.Info("Grabbed portrait from basket using right hand");
                    OnInteraction?.Invoke(false);
                }
            }

            wasLeftGrab = isLeftGrab;
            wasRightGrab = isRightGrab;

            wasLeftInside = InGrabArea(player.leftHandFollower.position);
            wasRightInside = InGrabArea(player.rightHandFollower.position);
        }

        public bool InGrabArea(Vector3 center, float radius = 0.15f)
        {
            numColliders = Physics.OverlapSphereNonAlloc(center, radius, hitColliders, layerMask, QueryTriggerInteraction.Collide);
            if (numColliders == 0) return false;

            for (int i = 0; i < numColliders; i++)
            {
                if (hitColliders[i] == grabSensor)
                    return true;
            }

            return false;
        }

        public bool InReserveArea(Vector3 center, float radius = 0.05f)
        {
            numColliders = Physics.OverlapSphereNonAlloc(center, radius, hitColliders, layerMask, QueryTriggerInteraction.Collide);
            if (numColliders == 0) return false;

            for(int i = 0; i < numColliders; i++)
            {
                if (hitColliders[i] == depositSensor)
                    return true;
            }

            return false;
        }
#endif
    }
}
