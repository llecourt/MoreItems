using DigitalRuby.ThunderAndLightning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class BottleBehaviour : GrabbableObject
    {
        string[] sources = new string[] { "drinkSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        NetworkVariable<bool> empty = new NetworkVariable<bool>(false);
        bool drinking = false;
        bool destroyedOnClient = false;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;

            sourcesDict = Utils.getAllAudioSources(gameObject, "BottleSFX", sources);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if(!empty.Value && !drinking)
            {
                drinking = true;
                drinkServerRpc();
            }
        }
        
        void FixedUpdate()
        {
            if(Utils.frameCount(20)) return;
            if(!destroyedOnClient && empty.Value)
            {
                Utils.destroyObj(gameObject, "liquid");
                destroyedOnClient = true;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void drinkServerRpc()
        {
            drinkClientRpc();
        }

        [ClientRpc]
        void drinkClientRpc()
        {
            StartCoroutine(Drink());
        }

        [ServerRpc(RequireOwnership = false)]
        void emptyBottleServerRpc()
        {
            empty.Value = true;
        }

        IEnumerator Drink()
        {
            playerHeldBy.activatingItem = true;
            playerHeldBy.playerBodyAnimator.SetBool("useTZPItem", true);

            sourcesDict["drinkSFX"].Play();
            
            StartCoroutine(DrinkAnimation(new Vector3(0.1f, 0.15f, 0.25f), new Vector3(120, 100, -95)));

            yield return new WaitForSeconds(4f);

            playerHeldBy.drunkness = 1f;
            playerHeldBy.drunknessSpeed = 0.5f;
            playerHeldBy.activatingItem = false;
            playerHeldBy.playerBodyAnimator.SetBool("useTZPItem", false);

            Utils.destroyObj(gameObject, "liquid");
            destroyedOnClient = true;

            StartCoroutine(DrinkAnimation(new Vector3(-0.15f, 0.08f, 0), new Vector3(120, 0, -95), 3));

            emptyBottleServerRpc();
            drinking = false;
        }

        IEnumerator DrinkAnimation(Vector3 targetPos, Vector3 targetRot, float multiplier = 1f)
        {
            while (itemProperties.positionOffset != targetPos || itemProperties.rotationOffset != targetRot)
            {
                itemProperties.positionOffset = Vector3.MoveTowards(itemProperties.positionOffset, targetPos, Time.deltaTime * multiplier);
                itemProperties.rotationOffset = Vector3.MoveTowards(itemProperties.rotationOffset, targetRot, Time.deltaTime * 1000);
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
