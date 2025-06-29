using GorillaLocomotion;
using GorillaPortraits.Behaviours.Objects;
using UnityEngine;

namespace GorillaPortraits.Models.StateMachine
{
    public class PortraitState_Loose(Portrait portrait, bool? wasThrown) : PortraitState_Base(portrait)
    {
        private Rigidbody rigidbody;

        private readonly bool? wasThrown = wasThrown;

        public override void Enter()
        {
            base.Enter();

            portrait.transform.SetParent(portrait.shelf.transform);

            if (!portrait.TryGetComponent(out rigidbody))
            {
                rigidbody = portrait.gameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = false;
                rigidbody.useGravity = true;
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rigidbody.mass = 32;
            }

            if (wasThrown.HasValue)
            {
                bool isLeftHand = wasThrown.Value;
                Transform estimatorParent = isLeftHand ? GTPlayer.Instance.leftControllerTransform : GTPlayer.Instance.rightControllerTransform;
                GorillaVelocityEstimator estimator = estimatorParent.GetComponentInChildren<GorillaVelocityEstimator>(true) ?? estimatorParent.gameObject.AddComponent<GorillaVelocityEstimator>();
                rigidbody.velocity = estimator.linearVelocity + GTPlayer.Instance.GetComponent<Rigidbody>().velocity;
                rigidbody.angularVelocity = estimator.angularVelocity;
            }
        }

        public override void Exit()
        {
            base.Exit();

            if (rigidbody is not null && rigidbody)
                Object.Destroy(rigidbody);
        }
    }
}
