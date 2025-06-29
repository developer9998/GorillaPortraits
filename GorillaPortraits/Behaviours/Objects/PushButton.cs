using UnityEngine;

#if PLUGIN
using System;
using HandIndicator = GorillaTriggerColliderHandIndicator;
#endif

namespace GorillaPortraits.Behaviours.Objects
{
    public class PushButton : MonoBehaviour
    {
        public float Debounce = 0.25f;

        public MeshRenderer Renderer;

        public int MatIndex = 0;

        public Material DefaultMat, ActivatedMat;

#if PLUGIN

        public bool OnCooldown => (lastPressTime + Debounce) >= Time.realtimeSinceStartup;

        public event Action<PushButton, bool> OnPressed;

        public bool Activated;

        private static float lastPressTime;

        public void OnTriggerEnter(Collider other)
        {
            if (OnCooldown || !other.TryGetComponent(out HandIndicator component))
                return;

            lastPressTime = Time.realtimeSinceStartup;

            GorillaTagger.Instance.offlineVRRig.PlayHandTapLocal(271, component.isLeftHand, 0.02f);
            GorillaTagger.Instance.StartVibration(component.isLeftHand, GorillaTagger.Instance.tapHapticStrength / 2f, GorillaTagger.Instance.tapHapticDuration);

            OnPressed?.Invoke(this, component.isLeftHand);
        }

        public void UpdateAppearence()
        {
            if (Renderer && ActivatedMat && DefaultMat)
            {
                Material[] materials = Renderer.materials;
                materials[MatIndex] = Activated ? ActivatedMat : DefaultMat;
                Renderer.materials = materials;
            }
        }
#endif
    }
}
