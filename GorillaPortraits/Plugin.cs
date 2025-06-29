using BepInEx;
using GorillaPortraits.Behaviours;
using GorillaPortraits.Tools;
using UnityEngine;

namespace GorillaPortraits
{
    [BepInPlugin(Constants.GUID, Constants.Name, Constants.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            new Logging(Logger);

            GorillaTagger.OnPlayerSpawned(delegate ()
            {
                try
                {
                    GameObject gameObject = new(Constants.Name);
                    DontDestroyOnLoad(gameObject);
                    gameObject.AddComponent<DataManager>();
                    gameObject.AddComponent<PhotoManager>();
                    gameObject.AddComponent<ShelfManager>();
                }
                catch
                {

                }
            });
        }
    }
}
