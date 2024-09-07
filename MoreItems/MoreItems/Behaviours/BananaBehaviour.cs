using GameNetcodeStuff;
using LethalLib.Modules;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class BananaBehaviour : GrabbableObject
    {
        string[] sources = new string[] { "slipSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        List<PlayerControllerB> playerSlipping = new List<PlayerControllerB>();
        Vector2 forwardVector = new Vector2(0, 1);
        List<EnemyAI> enemySlipping = new List<EnemyAI>();
        float slipTime = 3f;
        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "BananaSFX", sources);
        }

        public override void Update()
        {
            base.Update();
            if (Utils.frameCount(20))
                return;
            if (!playerHeldBy)
            {
                int mask = LayerMask.GetMask(new string[] { "Player", "Enemies" });
                var hits = Physics.OverlapSphere(gameObject.transform.position + Vector3.up * 0.1f, 0.5f, mask);

                foreach(var collider in hits)
                {
                    var player = collider.GetComponent<PlayerControllerB>();

                    if (player != null && !playerSlipping.Exists(i => i.playerClientId == player.playerClientId) && player.moveInputVector != Vector2.zero)
                    {
                        playerSlipping.Add(player);
                        StartCoroutine(TriggerSlip(player));
                    }
                }
            }
        }

        IEnumerator TriggerSlip(PlayerControllerB player)
        {
            var timer = 0f;
            playSlipSoundServerRpc();
            player.disableMoveInput = true;
            var camera = player.gameplayCamera.transform.forward;
            var forward = new Vector3(camera.x, 0, camera.z).normalized;
            var moveVector = player.moveInputVector;
            var angle = Vector3.Angle(forwardVector, moveVector);
            if(moveVector.x < 0 || moveVector.y < 0)
                angle *= -1;
            var direction = Quaternion.Euler(0, angle, 0) * forward;
            var forceMult = player.movementSpeed * 2;
            while (timer < slipTime)
            {
                player.externalForces = direction * forceMult;
                timer += Time.fixedDeltaTime;
                yield return new WaitForEndOfFrame();
            }
            playerSlipping.Remove(player);
            player.disableMoveInput = false;
        }

        [ServerRpc(RequireOwnership = false)]
        void playSlipSoundServerRpc()
        {
            playSlipSoundClientRpc();
        }

        [ClientRpc]
        void playSlipSoundClientRpc()
        {
            sourcesDict["slipSFX"].Play();
        }
    }
}
