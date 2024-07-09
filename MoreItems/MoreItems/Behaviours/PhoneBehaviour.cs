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
                this.insertedBattery.charge -= 0.334f;
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

            RaycastHit hit;
            int mask = LayerMask.GetMask(new string[] { "Player", "Enemies", "Room" });
            if (Physics.BoxCast(playerHeldBy.transform.position, playerHeldBy.transform.localScale * 2f, playerHeldBy.transform.forward, out hit, playerHeldBy.transform.rotation, 10f, mask))
            {
                print("boxcast hit !!!");
                print(hit.collider);
                var player = hit.collider.GetComponent<PlayerControllerB>();
                if (player != null)
                {
                    print("player hit !!!");
                    if(player.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId)
                    {
                        HUDManager.Instance.flashFilter = 0.6f;
                    }
                }

                var enemy = hit.collider.GetComponent<EnemyAICollisionDetect>();
                if (enemy != null)
                {
                    if (!enemy.mainScript.isEnemyDead)
                    {
                        print("enemy hit !!!");
                        enemy.mainScript.SetEnemyStunned(true, 0.5f, playerHeldBy);
                    }
                }
            }
        }
    }
}
