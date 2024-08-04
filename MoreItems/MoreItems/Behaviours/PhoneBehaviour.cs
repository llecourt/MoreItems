using Dissonance.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Unity.Netcode;
using GameNetcodeStuff;

namespace MoreItems.Behaviours
{
    internal class PhoneBehaviour : GrabbableObject
    {
        string[] sources = new string[] { "pictureSFX", "randSFX1", "randSFX2", "randSFX3", "randSFX4", "randSFX5", "randSFX6", "randSFX7", "randSFX8" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        Animator flashAnimator;
        float playInterval = 30f;
        float time = 0f;
        int interval = 10;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "PhoneSFX", sources);
            this.insertedBattery.charge = 1f;

            flashAnimator = this.transform.Find("flash").GetComponent<Animator>();
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if(this.insertedBattery.charge > 0)
            {
                flashRpc();
                this.insertedBattery.charge -= 0.1f;
            }
        }

        public override void Update()
        {
            base.Update();
            if (Time.frameCount % interval != 0)
                return;
            time += Time.deltaTime * interval;
            if(time >= playInterval)
            {
                time = 0f;
                playInterval = UnityEngine.Random.Range(30f, 120f);
                playRandomSoundRpc();
            }
        }

        void playRandomSoundRpc()
        {
            if (IsHost || IsServer)
            {
                playRandomSoundClientRpc();
            }
            else
            {
                playRandomSoundServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void playRandomSoundServerRpc()
        {
            playRandomSoundClientRpc();
        }

        [ClientRpc]
        void playRandomSoundClientRpc()
        {
            var index = UnityEngine.Random.Range(1, sources.Length - 1);
            sourcesDict[sources[index]].Play();
        }

        void flashRpc()
        {
            if (IsHost || IsServer)
            {
                flashClientRpc();
            }
            else
            {
                flashServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void flashServerRpc()
        {
            flashClientRpc();
        }

        [ClientRpc]
        void flashClientRpc()
        {
            sourcesDict["pictureSFX"].Play();
            flashAnimator.Play("base.flash");

            int mask = LayerMask.GetMask(new string[] { "Player", "Enemies" });
            List<int> hitEntities = new List<int>();
            Ray ray = new Ray(playerHeldBy.transform.position, playerHeldBy.transform.forward);

            Collider[] colliders = Physics.OverlapCapsule(ray.GetPoint(4f), ray.GetPoint(5f), 4f, mask);
            foreach (Collider collider in colliders)
            {
                var player = collider.GetComponent<PlayerControllerB>();

                var playerColliderGameObject = collider.gameObject;
                var enemyColliderRoot = collider.transform.root;

                if (player != null && !hitEntities.Exists(i => i == playerColliderGameObject.GetInstanceID()))
                {
                    hitEntities.Add(playerColliderGameObject.GetInstanceID());

                    if (player.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId 
                        && player.playerClientId != playerHeldBy.playerClientId
                        && !Physics.Linecast(playerHeldBy.transform.position + Vector3.up * 0.5f, player.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    {
                        float flashStr = determineFlashStrength(playerHeldBy.transform.forward, player.transform.forward) / 2;
                        HUDManager.Instance.flashFilter = flashStr;
                    }
                }

                var enemy = enemyColliderRoot.GetComponent<EnemyAI>();

                if (enemy != null && !hitEntities.Exists(i => i == enemyColliderRoot.gameObject.GetInstanceID()))
                {
                    hitEntities.Add(enemyColliderRoot.gameObject.GetInstanceID());

                    if(!Physics.Linecast(playerHeldBy.transform.position + Vector3.up * 0.5f, enemy.transform.position + Vector3.up * 0.5f, StartOfRound.Instance.collidersAndRoomMaskAndDefault, QueryTriggerInteraction.Ignore))
                    {
                        float flashStr = determineFlashStrength(playerHeldBy.transform.forward, enemy.transform.forward);
                        enemy.SetEnemyStunned(true, flashStr, playerHeldBy);
                    }
                }
            }
        }

        float determineFlashStrength(Vector3 playerHeld, Vector3 entityHit)
        {
            float dot = Vector3.Dot(playerHeld.normalized, entityHit.normalized);
            double flashStr = Math.Max(0, -0.1 + 2 * -dot);
            return Convert.ToSingle(flashStr);
        }
    }
}
