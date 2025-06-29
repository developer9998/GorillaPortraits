using GorillaExtensions;
using GorillaPortraits.Behaviours.Objects;
using GorillaTag.Audio;
using System;
using UnityEngine;

namespace GorillaPortraits.Models.StateMachine
{
    public class PortraitState_InHand(Portrait portrait, bool isLeftHand, bool spawnInHand) : PortraitState_Base(portrait)
    {
        protected Vector3 originPosition, targetHandPosition;
        protected Quaternion originRotation, targetHandRotation;

        public readonly bool isLeftHand = isLeftHand, spawnInHand = spawnInHand;

        private float interpolationTime;

        public override void Enter()
        {
            base.Enter();

            if (isLeftHand && EquipmentInteractor.instance.leftHandHeldEquipment is null)
                EquipmentInteractor.instance.leftHandHeldEquipment = portrait;
            else if (!isLeftHand && EquipmentInteractor.instance.rightHandHeldEquipment is null)
                EquipmentInteractor.instance.rightHandHeldEquipment = portrait;

            VRRig rig = VRRig.LocalRig ?? GorillaTagger.Instance.offlineVRRig;
            portrait.transform.SetParent(isLeftHand ? rig.leftHandTransform.parent : rig.rightHandTransform.parent);

            originPosition = portrait.transform.localPosition;
            originRotation = portrait.transform.localRotation;

            targetHandPosition = isLeftHand ? portrait.leftGrabPosition : portrait.rightGrabPosition;
            targetHandRotation = Quaternion.Euler(isLeftHand ? portrait.leftGrabEulerAngles : portrait.rightGrabEulerAngles);

            interpolationTime = Convert.ToInt32(spawnInHand);
            if (interpolationTime == 1)
            {
                MovePortrait();
                if (portrait.shelf.basket.grabSounds is AudioClip[] sounds && sounds.Length > 0) GTAudioOneShot.Play(sounds.GetRandomItem(), portrait.transform.position, 0.5f);
            }
        }

        public override void Exit()
        {
            base.Exit();

            if (isLeftHand && (UnityEngine.Object)EquipmentInteractor.instance.leftHandHeldEquipment == portrait)
                EquipmentInteractor.instance.leftHandHeldEquipment = null;
            else if (!isLeftHand && (UnityEngine.Object)EquipmentInteractor.instance.rightHandHeldEquipment == portrait)
                EquipmentInteractor.instance.rightHandHeldEquipment = null;
        }

        public override void Update()
        {
            base.Update();

            float targetTime = Mathf.Min(interpolationTime + (Time.deltaTime * 8f), 1f);
            if (targetTime != interpolationTime)
            {
                interpolationTime = targetTime;
                MovePortrait();
            }
        }

        public void MovePortrait()
        {
            bool atRestingLocation = interpolationTime == 1;
            Vector3 position = atRestingLocation ? targetHandPosition : Vector3.Lerp(originPosition, targetHandPosition, interpolationTime);
            Quaternion quaternion = atRestingLocation ? targetHandRotation : Quaternion.Lerp(originRotation, targetHandRotation, interpolationTime);

            portrait.transform.localPosition = position;
            portrait.transform.localRotation = quaternion;
        }
    }
}
