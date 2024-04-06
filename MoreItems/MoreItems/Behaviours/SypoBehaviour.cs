using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class SypoBehaviour : GrabbableObject
    {
        readonly string[] sources = new string[] { "criSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();

        void Awake()
        {
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "SypoSFX", sources);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            playActivateSoundRpc();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            setActiveRpc(false);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            setActiveRpc(true);
        }

        void playActivateSoundRpc()
        {
            if(IsHost || IsServer)
            {
                playActivateSoundClientRpc();
            }
            else
            {
                playActivateSoundServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void playActivateSoundServerRpc()
        {
            playActivateSoundClientRpc();
        }

        [ClientRpc]
        void playActivateSoundClientRpc()
        {
            sourcesDict["criSFX"].Play();
        }

        void setActiveRpc(bool active)
        {
            if (IsHost || IsServer)
            {
                setActiveClientRpc(active);
            }
            else
            {
                setActiveServerRpc(active);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void setActiveServerRpc(bool active)
        {
            setActiveClientRpc(active);
        }

        [ClientRpc]
        void setActiveClientRpc(bool active)
        {
            this.gameObject.SetActive(active);
        }
    }
}
