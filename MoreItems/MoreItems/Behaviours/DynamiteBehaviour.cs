using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class DynamiteBehaviour : GrabbableObject
    {
        public Ray grenadeThrowRay;
        public RaycastHit grenadeHit;

        float minExplosionTime = 2f;
        float maxExplosionTime = 10f;
        float explodeTimer = 0f;
        float timeToExplode = 999f;
        int interval = 10;

        private AudioSource MeshBurnSFX;
        private AudioSource ExplosionSFX;
        private AudioSource EarRingSFX;

        private ParticleSystem MeshSmoke;
        private ParticleSystem LingeringFire;
        private ParticleSystem MainBlast;
        private ParticleSystem Smoke;

        bool active = false;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            this.MeshBurnSFX = Utils.getAudioSource(this.gameObject, "DynamiteSFX", "MeshBurnSFX");
            this.ExplosionSFX = Utils.getAudioSource(this.gameObject, "DynamiteSFX", "ExplosionSFX");
            this.EarRingSFX = Utils.getAudioSource(this.gameObject, "DynamiteSFX", "EarRingSFX");

            this.MeshSmoke = Utils.getParticleSystem(this.gameObject, "DynamiteParticles", "MeshSmoke");
            this.LingeringFire = Utils.getParticleSystem(this.gameObject, "DynamiteParticles", "LingeringFire");
            this.MainBlast = Utils.getParticleSystem(this.gameObject, "DynamiteParticles", "MainBlast");
            this.Smoke = Utils.getParticleSystem(this.gameObject, "DynamiteParticles", "Smoke");
        }

        [ServerRpc(RequireOwnership = false)]
        void startDynamiteBurnServerRpc()
        {
            startDynamiteBurnClientRpc();
        }

        [ClientRpc]
        void startDynamiteBurnClientRpc()
        {
            MeshBurnSFX.Play();
            WalkieTalkie.TransmitOneShotAudio(MeshBurnSFX, MeshBurnSFX.clip);
            MeshSmoke.Play();
            Utils.destroyObj(this.gameObject, "Mesh");
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if (playerHeldBy != null)
            {
                if (!active)
                {
                    active = true;
                    timeToExplode = UnityEngine.Random.Range(maxExplosionTime, minExplosionTime);
                    if (IsHost || IsServer)
                    {
                        startDynamiteBurnClientRpc();
                    }
                    else
                    {
                        startDynamiteBurnServerRpc();
                    }
                }
                else
                {
                    playerHeldBy.DiscardHeldObject(true, placePosition: GetGrenadeThrowDestination());
                }
            }
        }

        public override void PocketItem()
        {
            base.PocketItem();
            editMeshAnimRpc(false);
        }

        public override void EquipItem()
        {
            base.EquipItem();
            if(active)
            {
                editMeshAnimRpc(true);
            }
        }

        public override void Update()
        {
            base.Update();
            if (Time.frameCount % interval != 0)
                return;

            if (active)
            {
                if (explodeTimer > timeToExplode)
                {
                    active = false;
                    if (IsHost || IsServer)
                    {
                        ExplodeDynamiteClientRpc();
                    }
                    else
                    {
                        ExplodeDynamiteServerRpc();
                    }
                }
                this.explodeTimer += Time.deltaTime * interval;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void ExplodeDynamiteServerRpc()
        {
            ExplodeDynamiteClientRpc();
        }

        [ClientRpc]
        public void ExplodeDynamiteClientRpc()
        {
            grabbable = false;
            grabbableToEnemies = false;

            MeshBurnSFX.Stop();
            MeshSmoke.Stop();

            ExplosionSFX.Play();
            WalkieTalkie.TransmitOneShotAudio(ExplosionSFX, ExplosionSFX.clip);

            MainBlast.Play();
            LingeringFire.Play();
            Smoke.Play();

            explodeTimer = 0f;

            Utils.Explode(this.gameObject, 8f, 50, 300, playerHeldBy == null);

            if (playerHeldBy != null)
                this.DestroyObjectInHand(playerHeldBy);

            Utils.destroyObj(this.gameObject, "Base");
            Destroy(this.gameObject, ExplosionSFX.clip.length);
        }

        void editMeshAnimRpc(bool active)
        {
            if(IsServer || IsHost)
            {
                editMeshAnimClientRpc(active);
            }
            else
            {
                editMeshAnimServerRpc(active);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void editMeshAnimServerRpc(bool active)
        {
            editMeshAnimClientRpc(active);
        }

        [ClientRpc]
        void editMeshAnimClientRpc(bool active)
        {
            if(active)
            {
                MeshSmoke.Play();
            }
            else
            {
                MeshSmoke.Stop();
            }
        }

        public Vector3 GetGrenadeThrowDestination()
        {
            Debug.DrawRay(this.playerHeldBy.gameplayCamera.transform.position, this.playerHeldBy.gameplayCamera.transform.forward, Color.yellow, 15f);
            grenadeThrowRay = new Ray(this.playerHeldBy.gameplayCamera.transform.position, this.playerHeldBy.gameplayCamera.transform.forward);
            Vector3 vector3 = !Physics.Raycast(this.grenadeThrowRay, out this.grenadeHit, 12f, StartOfRound.Instance.collidersAndRoomMaskAndDefault) ? this.grenadeThrowRay.GetPoint(10f) : this.grenadeThrowRay.GetPoint(this.grenadeHit.distance - 0.05f);
            Debug.DrawRay(vector3, Vector3.down, Color.blue, 15f);
            grenadeThrowRay = new Ray(vector3, Vector3.down);
            return !Physics.Raycast(this.grenadeThrowRay, out this.grenadeHit, 30f, StartOfRound.Instance.collidersAndRoomMaskAndDefault) ? this.grenadeThrowRay.GetPoint(30f) : this.grenadeHit.point + Vector3.up * 0.05f;
        }
    }
}
