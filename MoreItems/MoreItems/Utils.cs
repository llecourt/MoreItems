using System.Collections.Generic;
using UnityEngine;

namespace MoreItems
{
    internal class Utils
    {
        public static AudioSource getAudioSource(GameObject go, string rootNodeName, string gameObjectName)
        {
            return go.transform.Find(rootNodeName).Find(gameObjectName).gameObject.GetComponent<AudioSource>();
        }

        public static ParticleSystem getParticleSystem(GameObject go, string rootNodeName, string gameObjectName)
        {
            return go.transform.Find(rootNodeName).Find(gameObjectName).gameObject.GetComponent<ParticleSystem>();
        }

        public static Dictionary<string, ParticleSystem> getAllParticleSystems(GameObject go, string rootNodeName, string[] gameObjectsName)
        {
            var dict = new Dictionary<string, ParticleSystem>();
            foreach(var str in gameObjectsName)
            {
                dict.Add(str, getParticleSystem(go, rootNodeName, str));
            }
            return dict;
        }

        public static Dictionary<string, AudioSource> getAllAudioSources(GameObject go, string rootNodeName, string[] gameObjectsName)
        {
            var dict = new Dictionary<string, AudioSource>();
            foreach (var str in gameObjectsName)
            {
                dict.Add(str, getAudioSource(go, rootNodeName, str));
            }
            return dict;
        }

        public static void destroyObj(GameObject go, string obj)
        {
            try
            {
                UnityEngine.Object.Destroy(go.transform.Find(obj).gameObject);
            }
            catch
            {
                MonoBehaviour.print("Cannot destroy " + obj);
            }
        }
    }
}
