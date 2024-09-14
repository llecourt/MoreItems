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
        const string version = "8.6.0";

        Harmony harmony = new Harmony("LeoLR.MoreItems");
        public static Plugin instance;
        public AssetBundle bundle;

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
            bundle = AssetBundle.LoadFromFile(assetDir);

            var dynamite = bundle.LoadAsset<Item>("Assets/Dynamite/DynamiteItem.asset");
            dynamite.spawnPrefab.AddComponent<DynamiteBehaviour>().itemProperties = dynamite;
            Utilities.FixMixerGroups(dynamite.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(dynamite.spawnPrefab);
            TerminalNode dynamiteNode = ScriptableObject.CreateInstance<TerminalNode>();
            dynamiteNode.clearPreviousText = true;
            dynamiteNode.displayText = "Explodes after a few seconds";
            Items.RegisterShopItem(dynamite, null, null, dynamiteNode, 50);

            var karma = bundle.LoadAsset<Item>("Assets/Karma/KarmaItem.asset");
            karma.spawnPrefab.AddComponent<KarmaBehaviour>().itemProperties = karma;
            Utilities.FixMixerGroups(karma.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(karma.spawnPrefab);
            TerminalNode karmaNode = ScriptableObject.CreateInstance<TerminalNode>();
            karmaNode.clearPreviousText = true;
            karmaNode.displayText = "A massive railgun, requires battery to fire and has a charge time";
            Items.RegisterShopItem(karma, null, null, karmaNode, 500);

            var sypo = bundle.LoadAsset<Item>("Assets/Sypo/SypoItem.asset");
            sypo.spawnPrefab.AddComponent<SypoBehaviour>().itemProperties = sypo;
            Utilities.FixMixerGroups(sypo.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(sypo.spawnPrefab);
            Items.RegisterScrap(sypo, 15, Levels.LevelTypes.All);

            var minikit = bundle.LoadAsset<Item>("Assets/Minikit/MinikitItem.asset");
            Utilities.FixMixerGroups(minikit.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(minikit.spawnPrefab);
            Items.RegisterScrap(minikit, 15, Levels.LevelTypes.All);

            var coil = bundle.LoadAsset<Item>("Assets/GravityCoil/GravityCoilItem.asset");
            coil.spawnPrefab.AddComponent<GravityCoilBehaviour>().itemProperties = coil;
            Utilities.FixMixerGroups(coil.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(coil.spawnPrefab);
            Items.RegisterScrap(coil, 15, Levels.LevelTypes.All);

            var frame = bundle.LoadAsset<Item>("Assets/FramedPic/FramedPicItem.asset");
            frame.spawnPrefab.AddComponent<FramedPicBehaviour>().itemProperties = frame;
            Utilities.FixMixerGroups(frame.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(frame.spawnPrefab);
            Items.RegisterScrap(frame, 15, Levels.LevelTypes.All);

            var phone = bundle.LoadAsset<Item>("Assets/Phone/PhoneItem.asset");
            phone.spawnPrefab.AddComponent<PhoneBehaviour>().itemProperties = phone;
            Utilities.FixMixerGroups(phone.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(phone.spawnPrefab);
            Items.RegisterScrap(phone, 15, Levels.LevelTypes.All);

            var slaphand = bundle.LoadAsset<Item>("Assets/SlapHand/SlapHandItem.asset");
            slaphand.spawnPrefab.AddComponent<SlapHandBehaviour>().itemProperties = slaphand;
            Utilities.FixMixerGroups(slaphand.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(slaphand.spawnPrefab);
            Items.RegisterScrap(slaphand, 15, Levels.LevelTypes.All);

            var adren = bundle.LoadAsset<Item>("Assets/Adrenaline/AdrenalineItem.asset");
            adren.spawnPrefab.AddComponent<AdrenalineBehaviour>().itemProperties = adren;
            Utilities.FixMixerGroups(adren.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(adren.spawnPrefab);
            TerminalNode adrenalineNode = ScriptableObject.CreateInstance<TerminalNode>();
            adrenalineNode.clearPreviousText = true;
            adrenalineNode.displayText = "Gives a temporary speed and stamina boost (stackable)";
            Items.RegisterShopItem(adren, null, null, adrenalineNode, 35);

            var banana = bundle.LoadAsset<Item>("Assets/BananaPeel/BananaItem.asset");
            banana.spawnPrefab.AddComponent<BananaBehaviour>().itemProperties = banana;
            Utilities.FixMixerGroups(banana.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(banana.spawnPrefab);
            Items.RegisterScrap(banana, 15, Levels.LevelTypes.All);

            var bottle = bundle.LoadAsset<Item>("Assets/Bottle/BottleItem.asset");
            bottle.spawnPrefab.AddComponent<BottleBehaviour>().itemProperties = bottle;
            Utilities.FixMixerGroups(bottle.spawnPrefab);
            NetworkPrefabs.RegisterNetworkPrefab(bottle.spawnPrefab);
            Items.RegisterScrap(bottle, 15, Levels.LevelTypes.All);


            harmony.PatchAll();
        }
    }
}
