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

        static float baseJumpForce = 0f;
        float multiplier = 2f;

        static PlayerControllerB boostedPlayer = null;
        
        static bool currentlyHoldingCoil = false;
        bool boosted = false;

        void Awake()
        {
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "CoilSFX", sources);
        }

        public override void Update()
        {
            base.Update();
            if (playerHeldBy == null)
                return;

            if (boostedPlayer == null)
            {
                baseJumpForce = playerHeldBy.jumpForce;
                boostedPlayer = playerHeldBy;
            }
            else if (wasOwnerLastFrame)
            {
                if (!isPocketed)
                {
                    currentlyHoldingCoil = true;
                    if (!boosted)
                    {
                        boosted = true;
                        playerHeldBy.jumpForce = baseJumpForce * multiplier;
                    }
                }
                else if(!currentlyHoldingCoil)
                {
                    if (boosted)
                    {
                        boosted = false;
                        playerHeldBy.jumpForce = baseJumpForce;
                    }
                }
                var jumpAction = playerHeldBy.playerActions.FindAction("Jump");
                if (boosted && currentlyHoldingCoil && jumpAction.IsPressed() && playerHeldBy.thisController.isGrounded && !playerHeldBy.inSpecialInteractAnimation 
                    && !playerHeldBy.inAnimationWithEnemy && !playerHeldBy.isClimbingLadder && !playerHeldBy.isPlayerDead && !playerHeldBy.inTerminalMenu 
                    && !playerHeldBy.isTypingChat && !playerHeldBy.isCrouching)
                {
                    PlayJumpSFXRpc();
                }
            }
            
            if (Time.frameCount % 20 != 0)
                return;

            var currentItem = playerHeldBy.ItemSlots[playerHeldBy.currentItemSlot];
            if (currentItem != null && currentItem.itemProperties.name == "GravityCoilItem")
            {
                currentlyHoldingCoil = true;
                return;
            }
            currentlyHoldingCoil = false;
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            if (boostedPlayer == null)
                return;
            boosted = false;
            boostedPlayer.jumpForce = baseJumpForce;
            boostedPlayer = null;
            baseJumpForce = 0f;
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
