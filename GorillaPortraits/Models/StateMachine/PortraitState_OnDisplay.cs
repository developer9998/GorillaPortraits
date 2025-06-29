using GorillaExtensions;
using GorillaPortraits.Behaviours;
using GorillaPortraits.Behaviours.Objects;
using UnityEngine;

namespace GorillaPortraits.Models.StateMachine
{
    public class PortraitState_OnDisplay(Portrait portrait, BoxCollider sensor) : PortraitState_Base(portrait)
    {
        protected BoxCollider sensor = sensor;

        public override void Enter()
        {
            base.Enter();

            portrait.stand.SetActive(true);

            portrait.transform.SetParent(sensor.transform);
            portrait.transform.localPosition = portrait.transform.localPosition.WithY(0);

            Vector3 down = Vector3.down;
            Vector3 direction = portrait.transform.localRotation * down;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.001f) direction = down;

            direction.Normalize();

            if (Vector3.Dot(direction, down) < 0f) direction = -direction;

            portrait.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.Euler(-90f, 0f, 0f);
            portrait.transform.SetParent(portrait.shelf.transform);

            ShelfManager.OnPortraitAdd?.Invoke(portrait);
        }

        public override void Exit()
        {
            base.Exit();

            portrait.stand.SetActive(false);

            ShelfManager.OnPortraitRemove?.Invoke(portrait);
        }
    }
}
