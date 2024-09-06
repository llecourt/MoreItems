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
        NetworkVariable<float> playInterval = new NetworkVariable<float>(30f);
        NetworkVariable<float> time = new NetworkVariable<float>(0f);
        NetworkVariable<int> index = new NetworkVariable<int>(1);

        bool playing = false;
        int interval = 50;

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
                flashServerRpc();
                this.insertedBattery.charge -= 0.1f;
            }
        }

        public void FixedUpdate()
        {
            if (Utils.frameCount(interval))
                return;

            addToTimeServerRpc(Time.fixedDeltaTime * interval);
            if (time.Value >= playInterval.Value && !playing)
            {
                playing = true;
                playRandomSoundServerRpc();
                setNewNetworkValuesServerRpc();
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
            sourcesDict[sources[index.Value]].Play();
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

        [ServerRpc(RequireOwnership = false)]
        void setNewNetworkValuesServerRpc()
        {
            playInterval.Value = UnityEngine.Random.Range(60f, 180f);
            index.Value = UnityEngine.Random.Range(1, sources.Length - 1);
            time.Value = 0f;
            playing = false;
        }

        [ServerRpc(RequireOwnership = false)]
        void addToTimeServerRpc(float value)
        {
            time.Value += value;
        }
    }
}
