using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class SypoBehaviour : GrabbableObject
    {
        string[] sources = new string[] { "criSFX", "explosionSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();
        string[] ps = new string[] { "Smoke", "LingeringFire", "MainBlast" };
        Dictionary<string, ParticleSystem> psDict = new Dictionary<string, ParticleSystem>();

        int timeBeforeExplode = UnityEngine.Random.Range(15, 25);
        int timeBeforeCountdown = 5;
        Vector3 scale;
        Vector3 defaultScale;
        float defaultPitch;
        bool init = false;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "SypoSFX", sources);
            psDict = Utils.getAllParticleSystems(this.gameObject, "SypoParticles", ps);
            scale = this.gameObject.transform.localScale;
            defaultScale = this.gameObject.transform.localScale;
            defaultPitch = sourcesDict["criSFX"].pitch;
            init = true;
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            playActivateSoundServerRpc();
        }

        public override void PocketItem()
        {
            base.PocketItem();
            setActiveServerRpc(false);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            setActiveServerRpc(true);
        }

        public override void Update()
        {
            base.Update();
            if (Time.frameCount % 10 != 0 || !init)
                return;
            this.gameObject.transform.localScale = scale;
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
            if(timeBeforeCountdown > 0)
            {
                timeBeforeCountdown--;
            }
            else if(timeBeforeExplode > 0)
            {
                timeBeforeExplode--;
                scale += new Vector3(0.01f, 0.01f, 0.01f);
                sourcesDict["criSFX"].pitch += 0.03f;
            }
            else
            {
                explodeServerRpc();
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

        [ServerRpc(RequireOwnership = false)]
        void explodeServerRpc()
        {
            explodeClientRpc();
        }

        [ClientRpc]
        void explodeClientRpc()
        {
            Utils.destroyObj(this.gameObject, "body");
            grabbable = false;
            grabbableToEnemies = false;
            sourcesDict["explosionSFX"].Play();
            WalkieTalkie.TransmitOneShotAudio(sourcesDict["explosionSFX"], sourcesDict["explosionSFX"].clip);

            psDict["MainBlast"].Play();
            psDict["LingeringFire"].Play();
            psDict["Smoke"].Play();

            Utils.Explode(this.gameObject, 5f, 50, 180);

            timeBeforeCountdown = 5;
            timeBeforeExplode = UnityEngine.Random.Range(15, 25);
            scale = defaultScale;
            sourcesDict["criSFX"].pitch = defaultPitch;
            this.DestroyObjectInHand(playerHeldBy);
        }
    }
}
