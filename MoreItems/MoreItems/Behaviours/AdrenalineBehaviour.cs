using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class AdrenalineBehaviour : GrabbableObject
    {
        float runSpeedMultiplier = 1.5f;
        bool triggered = false;
        float timer = 15f;
        PlayerControllerB playerActiveOn = null;

        string[] sources = new string[] { "useSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "AdrSFX", sources);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (playerHeldBy != null)
            {
                activateSyringeServerRpc();
            }
        }

        IEnumerator StartTimer()
        {
            yield return new WaitForSeconds(timer);
            triggered = false;
            playerActiveOn.movementSpeed /= runSpeedMultiplier;
            yield return new WaitForSeconds(0.5f);
            Destroy(gameObject);
        }

        public override void Update()
        {
            base.Update();
            if (triggered && playerActiveOn != null)
            {
                playerActiveOn.sprintMeter = 1;
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if(playerActiveOn != null)
            {
                playerActiveOn.movementSpeed = 4.6f;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            if (playerActiveOn != null)
            {
                playerActiveOn.movementSpeed = 4.6f;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void activateSyringeServerRpc()
        {
            activateSyringeClientRpc();
        }

        [ClientRpc]
        void activateSyringeClientRpc()
        {
            sourcesDict["useSFX"].Play();
            playerActiveOn = playerHeldBy;
            playerActiveOn.movementSpeed *= runSpeedMultiplier;
            playerActiveOn.health = 100;
            DestroyObjectInHand(playerActiveOn);
            grabbable = false;
            grabbableToEnemies = false;
            triggered = true;
            Utils.destroyObj(gameObject, "SyringeBody");
            StartCoroutine(StartTimer());
        }
    }
}
