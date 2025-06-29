using UnityEngine;

#if PLUGIN
using System;
using System.Linq;
using GorillaPortraits.Tools;
#endif

namespace GorillaPortraits.Behaviours.Objects
{
    public class Shelf : MonoBehaviour
    {
        public string displayName;

        [Tooltip("The name of the GTZone that our shelf is present in. Our shelf will enable and disable based on whether the zone is loaded.")]
        public string requiredZoneName;

        [Space]

        public Vector3 worldPosition;
        public Vector3 worldEulerAngles;

        [Space]

        public Basket basket;

        public BoxCollider[] placementSensors;


#if PLUGIN

        private GTZone requiredZone = GTZone.none;

        private LayerMask layerMask = 1 << (int)UnityLayer.Prop;

        private readonly Collider[] hitColliders = new Collider[15];
        private int numColliders;

        public void Awake()
        {
            transform.position = worldPosition;
            transform.eulerAngles = worldEulerAngles;

            basket.OnInteraction += delegate (bool isLeftHand)
            {
                Logging.Info($"OnInteraction (for {requiredZoneName}, is left hand: {isLeftHand})");
                ShelfManager.Instance.NewPortrait(basket.portraitPrefab, this, isLeftHand);
            };

            DataManager.OnDataLoaded += OnDataLoaded;
        }

        public void OnDataLoaded()
        {
            if (requiredZone == GTZone.none)
            {
                DataManager.OnDataLoaded -= OnDataLoaded;

                if (requiredZoneName != null && requiredZoneName.Length > 0)
                {
                    foreach (GTZone zone in Enum.GetValues(typeof(GTZone)).Cast<GTZone>())
                    {
                        if (zone.GetName().ToLower().Contains(requiredZoneName.ToLower()))
                        {
                            Logging.Info($"Identified zone for {gameObject.name}: {zone}");
                            requiredZone = zone;
                            break;
                        }
                    }

                    if (requiredZone != GTZone.none)
                    {
                        CheckZones(ZoneManagement.instance.zones);
                        ZoneManagement.OnZoneChange += CheckZones;
                    }
                }
            }
        }

        public void CheckZones(ZoneData[] zones)
        {
            bool active = requiredZone == GTZone.none || ZoneManagement.IsInZone(requiredZone);

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(active);
            }
        }

        public BoxCollider GetPlacementArea(Vector3 center, float radius = 0.015f)
        {
            if (placementSensors == null || placementSensors.Length == 0) return null;

            numColliders = Physics.OverlapSphereNonAlloc(center, radius, hitColliders, layerMask, QueryTriggerInteraction.Collide);
            if (numColliders == 0) return null;

            for (int i = 0; i < numColliders; i++)
            {
                if (Array.Find(placementSensors, sensor => hitColliders[i] == sensor) is BoxCollider sensor && sensor)
                {
                    return sensor;
                }
            }

            return null;
        }
#endif
    }
}
