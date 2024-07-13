using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using Unity.Netcode;
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

        public static void Explode(GameObject go, float radius, float baseDamage, float distanceInfluencedDamage, bool increaseHeightForLinecast = false)
        {
            int mask = LayerMask.GetMask(new string[] { "Player", "Enemies" });
            List<int> hitEntities = new List<int>();

            Collider[] colliders = Physics.OverlapSphere(go.transform.position, radius, mask);
            foreach (Collider collider in colliders)
            {
                var player = collider.GetComponent<PlayerControllerB>();

                var playerColliderGameObject = collider.gameObject;
                var enemyColliderRoot = collider.transform.root;

                if (player != null && !hitEntities.Exists(i => i == playerColliderGameObject.GetInstanceID()))
                {
                    hitEntities.Add(playerColliderGameObject.GetInstanceID());
                    var blocked = Physics.Linecast(
                        player.transform.position + Vector3.up * 0.5f,
                        go.transform.position + Vector3.up * (increaseHeightForLinecast ? 0.5f : 0f),
                        StartOfRound.Instance.collidersAndRoomMaskAndDefault
                        );
                    if (!blocked)
                    {
                        if(player.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId)
                        {
                            var distance = Vector3.Distance(player.transform.position + Vector3.up * 0.5f, go.transform.position);
                            var damage = distanceInfluencedDamage / distance + baseDamage;
                            player.DamagePlayer((int)damage, causeOfDeath: CauseOfDeath.Blast);
                        }
                    }
                }
                var enemy = enemyColliderRoot.GetComponent<EnemyAI>();

                if (enemy != null && !hitEntities.Exists(i => i == enemyColliderRoot.gameObject.GetInstanceID()))
                {
                    if (!enemy.isEnemyDead)
                    {
                        hitEntities.Add(enemyColliderRoot.gameObject.GetInstanceID());
                        var blocked = Physics.Linecast(
                            enemy.transform.position + Vector3.up * 0.5f,
                            go.transform.position + Vector3.up * (increaseHeightForLinecast ? 0.5f : 0f),
                            StartOfRound.Instance.collidersAndRoomMaskAndDefault
                        );
                        if (!blocked)
                        {
                            if (enemy.creatureVoice != null && enemy.dieSFX != null)
                            {
                                enemy.creatureVoice.PlayOneShot(enemy.dieSFX);
                            }
                            enemy.KillEnemy(true);
                        }
                    }
                }
            }

            hitEntities.Clear();
        }
    }
}
