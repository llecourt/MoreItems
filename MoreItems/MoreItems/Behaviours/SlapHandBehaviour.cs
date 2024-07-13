using GameNetcodeStuff;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class SlapHandBehaviour : Shovel
    {
        string[] sources = new string[] { "reelSFX", "swingSFX", "hitSFX", "goofySFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        bool hittingPlayer = false;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            shovelHitForce = 0;

            sourcesDict = Utils.getAllAudioSources(this.gameObject, "SHSFX", sources);

            shovelAudio = this.gameObject.GetComponent<AudioSource>();
            reelUp = sourcesDict["reelSFX"].clip;
            swing = sourcesDict["swingSFX"].clip;
            hitSFX = new AudioClip[] { sourcesDict["hitSFX"].clip };
        }

        public override void Update()
        {
            base.Update();
            if (Time.frameCount % 50 != 0)
                return;
            if(playerHeldBy != null && reelingUp && !isHoldingButton && !hittingPlayer)
            {
                hittingPlayer = true;
                hitPlayerServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void hitPlayerServerRpc()
        {
            hitPlayerClientRpc();
        }

        [ClientRpc]
        void hitPlayerClientRpc()
        {
            int mask = LayerMask.GetMask(new string[] { "Player", "Enemies" });
            List<int> hitEntities = new List<int>();
            var camTransform = playerHeldBy.gameplayCamera.transform;

            var hits = Physics.SphereCastAll(camTransform.position + camTransform.right * -0.35f, 0.8f, camTransform.forward, 1.5f, mask, QueryTriggerInteraction.Ignore);
            foreach (var collider in hits)
            {
                var player = collider.collider.GetComponent<PlayerControllerB>();

                var playerColliderGameObject = collider.collider.gameObject;
                var enemyColliderRoot = collider.collider.transform.root;

                if (player != null 
                    && playerHeldBy.playerClientId != GameNetworkManager.Instance.localPlayerController.playerClientId 
                    && !hitEntities.Exists(i => i == playerColliderGameObject.GetInstanceID()))
                {
                    print("player hit !!");
                    print(player.name);

                    hitEntities.Add(playerColliderGameObject.GetInstanceID());
                    StartCoroutine(knockback(player));
                }
                var enemy = enemyColliderRoot.GetComponent<EnemyAI>();

                if (enemy != null && !hitEntities.Exists(i => i == enemyColliderRoot.gameObject.GetInstanceID()))
                {
                    hitEntities.Add(enemyColliderRoot.gameObject.GetInstanceID());
                    StartCoroutine(knockback(enemy));
                }
            }

            hittingPlayer = false;
        }

        public IEnumerator knockback(NetworkBehaviour entity)
        {
            float time = 1.0f;
            float currentTime = 0f;
            var initialPosition = entity.transform.position;
            var playerLookingDirection = playerHeldBy.gameplayCamera.transform.forward;
            if (playerLookingDirection.y < 0)
            {
                playerLookingDirection.y = -playerLookingDirection.y;
            }

            var direction = playerLookingDirection + Vector3.up * 0.25f;

            Ray ray = new Ray(initialPosition + Vector3.up * 0.5f, direction);
            float distance = 20f;

            while (Physics.Linecast(ray.GetPoint(0f), ray.GetPoint(distance), StartOfRound.Instance.collidersAndRoomMaskAndDefault))
            {
                distance -= 1f;
            }

            var finalPosition = ray.GetPoint(distance);

            while (currentTime < time)
            {
                entity.transform.position = Vector3.MoveTowards(entity.transform.position, finalPosition, currentTime);
                currentTime += Time.deltaTime;
            }

            yield return null;
        }
    }
}
