﻿using GameNetcodeStuff;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace MoreItems.Behaviours
{
    internal class DynamiteBehaviour : GrabbableObject
    {
        public Ray grenadeThrowRay;
        public RaycastHit grenadeHit;

        float explodeTimer = 0f;
        float timeToExplode = 999f;

        readonly float minExplosionTime = 2f;
        readonly float maxExplosionTime = 10f;
        readonly float radius = 10f;
        readonly int interval = 10;
        readonly int damage = 100;

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
            Utils.destroyObj(this.gameObject, "Base");
            MeshBurnSFX.Stop();
            MeshSmoke.Stop();

            ExplosionSFX.Play();
            WalkieTalkie.TransmitOneShotAudio(ExplosionSFX, ExplosionSFX.clip);

            MainBlast.Play();
            LingeringFire.Play();
            Smoke.Play();

            explodeTimer = 0f;

            int mask = LayerMask.GetMask(new string[]{ "Player", "Enemies" });
            List<int> hitEntities = new List<int>();

            Collider[] colliders = Physics.OverlapSphere(this.gameObject.transform.position, radius, mask);
            foreach(Collider collider in colliders)
            {
                var player = collider.GetComponent<PlayerControllerB>();

                var playerColliderGameObject = collider.gameObject;
                var enemyColliderRoot = collider.transform.root;

                if (player != null && !hitEntities.Exists(i => i == playerColliderGameObject.GetInstanceID()))
                {
                    hitEntities.Add(playerColliderGameObject.GetInstanceID());
                    player.DamagePlayer(damage, causeOfDeath: CauseOfDeath.Blast);
                    player.statusEffectAudio.PlayOneShot(EarRingSFX.clip);
                }
                var enemy = enemyColliderRoot.GetComponent<EnemyAI>();

                if (enemy != null && !hitEntities.Exists(i => i == enemyColliderRoot.gameObject.GetInstanceID()))
                {
                    if (!enemy.isEnemyDead)
                    {
                        hitEntities.Add(enemyColliderRoot.gameObject.GetInstanceID());
                        if(enemy.creatureVoice != null && enemy.dieSFX != null)
                        {
                            enemy.creatureVoice.PlayOneShot(enemy.dieSFX);
                        }
                        enemy.KillEnemy(true);
                    }
                }
            }

            hitEntities.Clear();
            this.DiscardItem();
            Destroy(this.gameObject, EarRingSFX.clip.length);
        }

        void editMeshAnimRpc(bool active)
        {
            if(IsServer || IsHost)
            {
                editMeshAnimClientRpc(active);
            }
            else
            {
                editMeshAnimClientRpc(active);
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
