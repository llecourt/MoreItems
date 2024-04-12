using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class GravityCoilBehaviour : GrabbableObject
    {
        string[] sources = new string[] { "jumpSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        float baseJumpForce = 13f;
        float multiplier = 2f;

        PlayerControllerB boostedPlayer = null;
        
        bool active = false;
        void Awake()
        {
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "CoilSFX", sources);
        }

        public override void Update()
        {
            base.Update();
            if (!active || playerHeldBy == null || !wasOwnerLastFrame || Time.frameCount % 10 != 0)
                return;

            if (playerHeldBy.playerActions.FindAction("Jump").IsPressed() && playerHeldBy.thisController.isGrounded && !playerHeldBy.inSpecialInteractAnimation 
                && !playerHeldBy.inAnimationWithEnemy && !playerHeldBy.isClimbingLadder && !playerHeldBy.isPlayerDead && !playerHeldBy.inTerminalMenu 
                && !playerHeldBy.isTypingChat && !playerHeldBy.isCrouching)
            {
                PlayJumpSFXRpc();
            }
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            if (boostedPlayer == null)
                return;
            boostedPlayer.jumpForce = baseJumpForce;
            boostedPlayer = null;
        }

        public override void GrabItem()
        {
            base.GrabItem();
            boostedPlayer = playerHeldBy;
        }

        public override void GrabItemFromEnemy(EnemyAI enemy)
        {
            base.GrabItemFromEnemy(enemy);
            boostedPlayer = playerHeldBy;
        }

        public override void PocketItem()
        {
            base.PocketItem();
            boostedPlayer.jumpForce = baseJumpForce;
            active = false;
        }

        public override void EquipItem()
        {
            base.EquipItem();
            boostedPlayer = playerHeldBy;
            boostedPlayer.jumpForce = baseJumpForce * multiplier;
            active = true;
        }

        void PlayJumpSFXRpc()
        {
            if(IsHost || IsServer)
            {
                PlayJumpSFXClientRpc();
            }
            else
            {
                PlayJumpSFXServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void PlayJumpSFXServerRpc()
        {
            PlayJumpSFXClientRpc();
        }

        [ClientRpc]
        void PlayJumpSFXClientRpc()
        {
            sourcesDict["jumpSFX"].Play();
        }
    }
}
