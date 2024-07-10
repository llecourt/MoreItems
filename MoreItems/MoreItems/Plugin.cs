using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using LethalLib.Modules;
using MoreItems.Behaviours;
using UnityEngine;
using UnityEngine.Assertions;

namespace MoreItems
{
    [BepInPlugin(guid, name, version)]
    public class Plugin : BaseUnityPlugin
    {
        const string guid = "LeoLR.MoreItems";
        const string name = "MoreItems";
        const string version = "4.2.0";

        Harmony harmony = new Harmony("LeoLR.MoreItems");
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
            NetworkPrefabs.RegisterNetworkPrefab(dynamite.spawnPrefab);
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.clearPreviousText = true;
            node.displayText = "Boom boom";
            Items.RegisterShopItem(dynamite, null, null, node, 50);
            print("dynamite loaded");

            var karma = bundle.LoadAsset<Item>("Assets/Karma/KarmaItem.asset");
            KarmaBehaviour kb = karma.spawnPrefab.AddComponent<KarmaBehaviour>();
            kb.itemProperties = karma;
            kb.grabbable = true;
            kb.grabbableToEnemies = true;
            Utilities.FixMixerGroups(karma.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(karma.spawnPrefab);
            Items.RegisterShopItem(karma, 500);

            var sypo = bundle.LoadAsset<Item>("Assets/Sypo/SypoItem.asset");
            var sb = sypo.spawnPrefab.AddComponent<SypoBehaviour>();
            sb.itemProperties = sypo;
            sb.grabbable = true;
            sb.grabbableToEnemies = true;
            Utilities.FixMixerGroups(sypo.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(sypo.spawnPrefab);
            Items.RegisterScrap(sypo, 20, Levels.LevelTypes.All);

            var minikit = bundle.LoadAsset<Item>("Assets/Minikit/MinikitItem.asset");
            Utilities.FixMixerGroups(minikit.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(minikit.spawnPrefab);
            Items.RegisterScrap(minikit, 20, Levels.LevelTypes.All);

            var coil = bundle.LoadAsset<Item>("Assets/GravityCoil/GravityCoilItem.asset");
            var cb = coil.spawnPrefab.AddComponent<GravityCoilBehaviour>();
            cb.itemProperties = coil;
            cb.grabbable = true;
            cb.grabbableToEnemies = true;
            Utilities.FixMixerGroups(coil.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(coil.spawnPrefab);
            Items.RegisterScrap(coil, 20, Levels.LevelTypes.All);

            var frame = bundle.LoadAsset<Item>("Assets/FramedPic/FramedPicItem.asset");
            Utilities.FixMixerGroups(frame.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(frame.spawnPrefab);
            Items.RegisterScrap(frame, 20, Levels.LevelTypes.All);

            var phone = bundle.LoadAsset<Item>("Assets/Phone/PhoneItem.asset");
            var ph = phone.spawnPrefab.AddComponent<PhoneBehaviour>();
            ph.itemProperties = phone;
            ph.grabbable = true;
            ph.grabbableToEnemies = true;
            Utilities.FixMixerGroups(phone.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(phone.spawnPrefab);
            // Items.RegisterScrap(phone, 20, Levels.LevelTypes.All);
            Items.RegisterShopItem(phone, 0);

            harmony.PatchAll();
        }
    }
}
