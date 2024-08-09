using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class SlapHandBehaviour : GrabbableObject
    {
        string[] sources = new string[] { "reelSFX", "swingSFX", "hitSFX", "goofySFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        float time = 1.0f;
        float hitCD = 0.4f;
        float timeSinceHit;
        bool startCounting = false;
        int slapHandMask = 1084754248;
        bool reelingUp = false;
        Coroutine reelingUpCoroutine = null;
        float slapHandHitRadius = 0.8f;
        float slapHandHitDistance = 1.5f;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            
            sourcesDict = Utils.getAllAudioSources(gameObject, "SHSFX", sources);
            timeSinceHit = hitCD;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            if (playerHeldBy == null) return;
            reelingUp = buttonDown;
            if (!buttonDown || timeSinceHit < hitCD) return;
            if (reelingUpCoroutine != null)
                StopCoroutine(reelingUpCoroutine);
            reelingUpCoroutine = StartCoroutine(reelUpSlapHand());
        }

        public override void Update()
        {
            base.Update();
            if(startCounting)
            {
                if (timeSinceHit >= hitCD)
                    startCounting = false;
                timeSinceHit += Time.deltaTime;
            }
        }

        private IEnumerator reelUpSlapHand()
        {
            var slapHand = this;
            slapHand.playerHeldBy.activatingItem = true;
            slapHand.playerHeldBy.twoHanded = true;
            slapHand.playerHeldBy.playerBodyAnimator.ResetTrigger("shovelHit");
            slapHand.playerHeldBy.playerBodyAnimator.SetBool("reelingUp", true);
            slapHand.ReelUpSFXServerRpc();
            yield return new WaitForSeconds(0.35f);
            yield return new WaitUntil(() => slapHand.reelingUp == false);
            slapHand.SwingSlapHandServerRpc(!slapHand.isHeld);
            yield return new WaitForSeconds(0.13f);
            yield return new WaitForEndOfFrame();
            slapHand.HitSlapHandServerRpc(!slapHand.isHeld);
            yield return new WaitForSeconds(0.3f);
            slapHand.reelingUpCoroutine = null;
            slapHand.playerHeldBy.activatingItem = false;
            slapHand.playerHeldBy.twoHanded = false;
            slapHand.timeSinceHit = 0;
            slapHand.startCounting = true;
        }


        [ServerRpc(RequireOwnership = false)]
        void ReelUpSFXServerRpc()
        {
            ReelUpSFXClientRpc();
        }

        [ClientRpc]
        void ReelUpSFXClientRpc()
        {
            sourcesDict["reelSFX"].Play();
        }

        [ServerRpc(RequireOwnership = false)]
        void SwingSlapHandServerRpc(bool cancel = false)
        {
            SwingSlapHandClientRpc(cancel);
        }

        [ClientRpc]
        void SwingSlapHandClientRpc(bool cancel = false)
        {
            playerHeldBy.playerBodyAnimator.SetBool("reelingUp", false);
            if (cancel) return;
            sourcesDict["swingSFX"].Play();
            playerHeldBy.UpdateSpecialAnimationValue(true, (short)playerHeldBy.transform.localEulerAngles.y, 0.4f);
        }

        [ServerRpc(RequireOwnership = false)]
        void HitSlapHandServerRpc(bool cancel = false)
        {
            HitSlapHandClientRpc(cancel);
        }

        [ClientRpc]
        void HitSlapHandClientRpc(bool cancel = false)
        {
            if (cancel) return;
            sourcesDict["hitSFX"].Play();

            List<int> hitEntities = new List<int>();
            var camTransform = playerHeldBy.gameplayCamera.transform;

            var hits = Physics.SphereCastAll(camTransform.position + camTransform.right * -0.35f, slapHandHitRadius, camTransform.forward, slapHandHitDistance, slapHandMask, QueryTriggerInteraction.Collide);
            foreach (var collider in hits)
            {
                var playerColliderGameObject = collider.collider.gameObject;
                var player = collider.collider.GetComponent<PlayerControllerB>();

                var enemyColliderRoot = collider.collider.transform.root;
                var enemy = enemyColliderRoot.GetComponent<EnemyAI>();

                if (player != null
                    && !hitEntities.Exists(i => i == playerColliderGameObject.GetInstanceID())
                    && playerHeldBy.playerClientId != GameNetworkManager.Instance.localPlayerController.playerClientId
                    && playerHeldBy.playerClientId != player.playerClientId)
                {
                    hitEntities.Add(playerColliderGameObject.GetInstanceID());
                    player.DamagePlayer(0);
                    knockback(playerColliderGameObject);
                }

                if (enemy != null && !hitEntities.Exists(i => i == enemyColliderRoot.gameObject.GetInstanceID()))
                {
                    hitEntities.Add(enemyColliderRoot.gameObject.GetInstanceID());
                    knockback(enemyColliderRoot.gameObject);
                }
            }
        }

        void knockback(GameObject entity)
        {
            float currentTime = 0f;
            var initialPosition = entity.transform.position;
            var playerLookingDirection = playerHeldBy.gameplayCamera.transform.forward;
            if (playerLookingDirection.y < 0)
            {
                playerLookingDirection.y = -playerLookingDirection.y;
            }

            var direction = playerLookingDirection + Vector3.up * 0.3f;

            Ray ray = new Ray(initialPosition + Vector3.up * 0.1f, direction);
            float distance = 20f;

            while (Physics.Linecast(ray.GetPoint(0f), ray.GetPoint(distance), StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                distance -= 1f;
            }

            var finalDistance = distance > 0 ? distance - 0.5f : distance;
            var finalPosition = ray.GetPoint(finalDistance);

            while (currentTime < time)
            {
                entity.transform.position = Vector3.MoveTowards(entity.transform.position, finalPosition, currentTime);
                currentTime += Time.deltaTime;
            }
        }
    }
}
