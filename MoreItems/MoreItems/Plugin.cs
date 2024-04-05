using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using MoreItems.Behaviours;
using UnityEngine;

namespace MoreItems
{
    [BepInPlugin(guid, name, version)]
    public class Plugin : BaseUnityPlugin
    {
        const string guid = "LeoLR.MoreItems";
        const string name = "MoreItems";
        const string version = "0.0.5";

        readonly Harmony harmony = new Harmony("LeoLR.MoreItems");
        public static Plugin instance;

        void Awake()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
            instance = this;

            string assetDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "moreitems");
            AssetBundle bundle = AssetBundle.LoadFromFile(assetDir);

            var dynamite = bundle.LoadAsset<Item>("Assets/Dynamite/DynamiteItem.asset");
            DynamiteBehaviour db = dynamite.spawnPrefab.AddComponent<DynamiteBehaviour>();
            db.itemProperties = dynamite;
            db.grabbable = true;
            db.grabbableToEnemies = true;

            Utilities.FixMixerGroups(dynamite.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(dynamite.spawnPrefab);

            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "Boom boom";
            Items.RegisterShopItem(dynamite, null, null, node, 50);

            var karma = bundle.LoadAsset<Item>("Assets/Karma/KarmaItem.asset");
            KarmaBehaviour kb = karma.spawnPrefab.AddComponent<KarmaBehaviour>();
            kb.itemProperties = karma;
            kb.grabbable = true;
            kb.grabbableToEnemies = true;

            Utilities.FixMixerGroups(karma.spawnPrefab);
            LethalLib.Modules.NetworkPrefabs.RegisterNetworkPrefab(karma.spawnPrefab);

            Items.RegisterShopItem(karma, 500);

            harmony.PatchAll();
        }
    }
}
