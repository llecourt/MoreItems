using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;

namespace MoreItems.Behaviours
{
    internal class KarmaBehaviour : GrabbableObject
    {
        readonly float cooldown = 15f;
        float timeSinceShot = 0f;
        NetworkVariable<bool> coolingDown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        readonly float maxChargeBeforeShooting = 2.5f;
        readonly float maxChargeBeforeExplode = 3.5f;
        readonly float explosionRadius = 5f;
        readonly int explosionDamage = 100;
        readonly int bulletDamage = 6;
        float chargeTime = 0f;
        bool charging = false;

        bool activated = false;
        bool shoot = false;
        bool explode = false;
        bool broken = false;

        readonly string[] sources = new string[] { "ChargeSFX", "ShootSFX", "TrailSFX", "WarningSFX", "ExplosionSFX", "CooldownSFX", "CoolupSFX", "OnCdShotSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();
        readonly string[] ps = new string[] { "MuzzleFlash", "Trail", "LingeringFire", "MainBlast" };
        Dictionary<string, ParticleSystem> psDict = new Dictionary<string, ParticleSystem>();

        Color readyColor = new Color32(20, 20, 20, 255);
        Color shootColor = new Color32(20, 200, 20, 255);
        Color dangerColor = new Color32(200, 20, 20, 255);
        Color unreadyColor = new Color32(250, 100, 0, 255);

        GameObject ScopeChargeTimer;

        void Awake()
        {
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "KarmaSFX", sources);
            psDict = Utils.getAllParticleSystems(this.gameObject, "KarmaParticles", ps);
            ScopeChargeTimer = this.gameObject.transform.Find("ScopeChargeTimer").gameObject;
            
            this.insertedBattery.charge = 1f;
        }

        public override void PocketItem()
        {
            base.PocketItem();
            StopChargingRpc();
            activated = false;
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            StopChargingRpc();
            activated = false;
            if (!coolingDown.Value && this.insertedBattery.charge > 0 && !broken)
            {
                if (UnityEngine.Random.Range(1, 20) == 1)
                {
                    shoot = true;
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if(playerHeldBy != null && !isPocketed && wasOwnerLastFrame && !broken)
            {
                if (buttonDown)
                {
                    if (!coolingDown.Value && this.insertedBattery.charge > 0)
                    {
                        activated = true;
                    }
                    else
                    {
                        sourcesDict["OnCdShotSFX"].Play();
                    }
                }
                else
                {
                    activated = false;
                }
            }
        }

        public override void ChargeBatteries()
        {
            base.ChargeBatteries();
            RepairRpc();
        }

        public override void Update()
        {
            base.Update();
            if(!broken)
            {
                if (coolingDown.Value)
                {
                    timeSinceShot += Time.deltaTime;
                    if (timeSinceShot > cooldown)
                    {
                        ResetCooldownRpc();
                    }
                }
                else if (wasOwnerLastFrame)
                {
                    if (activated)
                    {
                        StartChargingRpc();
                        if (chargeTime > maxChargeBeforeExplode)
                        {
                            explode = true;
                        }
                        chargeTime += Time.deltaTime;
                    }
                    else
                    {
                        if (chargeTime >= maxChargeBeforeShooting && chargeTime <= maxChargeBeforeExplode)
                        {
                            shoot = true;
                        }
                        StopChargingRpc();
                    }
                }
                if (shoot)
                {
                    ShootRpc();
                }
                if (explode)
                {
                    ExplodeRpc();
                }
            }
        }

        void ShootRpc()
        {
            if (IsHost || IsServer)
            {
                ShootClientRpc();
            }
            else
            {
                ShootServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void ShootServerRpc()
        {
            ShootClientRpc();
        }

        [ClientRpc]
        void ShootClientRpc()
        {
            if (!shoot)
                return;
            shoot = false;
            sourcesDict["ShootSFX"].Play();
            sourcesDict["TrailSFX"].PlayDelayed(0.1f);
            psDict["MuzzleFlash"].Play();
            psDict["Trail"].Play();
            this.insertedBattery.charge -= 0.5f;
            sourcesDict["CooldownSFX"].PlayDelayed(0.35f);
            changeCooldownValueRpc(true);
            ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = unreadyColor;
            HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);

            RaycastHit hit;
            int mask = LayerMask.GetMask(new string[] { "Player", "Enemies", "Room" });
            int playerDamage = bulletDamage * 5;
            if (Physics.Raycast(psDict["Trail"].transform.position, psDict["Trail"].transform.forward, out hit, float.MaxValue, mask))
            {
                var player = hit.collider.GetComponent<PlayerControllerB>();
                if(player != null)
                {
                    if (IsHost || IsServer)
                    {
                        player.DamagePlayerFromOtherClientClientRpc(playerDamage, psDict["Trail"].transform.forward, (int)player.playerClientId, player.health - playerDamage);
                    }
                    else
                    {
                        player.DamagePlayerFromOtherClientServerRpc(playerDamage, psDict["Trail"].transform.forward, (int)player.playerClientId);
                    }
                }

                var enemy = hit.collider.GetComponent<EnemyAICollisionDetect>();
                if(enemy != null)
                {
                    if (!enemy.mainScript.isEnemyDead)
                    {
                        enemy.mainScript.HitEnemy(bulletDamage, playerHeldBy);
                    }
                }
            }
        }

        void StartChargingRpc()
        {
            if (IsHost || IsServer)
            {
                StartChargingClientRpc();
            }
            else
            {
                StartChargingServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void StartChargingServerRpc()
        {
            StartChargingClientRpc();
        }

        [ClientRpc]
        void StartChargingClientRpc()
        {
            if(!charging)
            {
                charging = true;
                sourcesDict["ChargeSFX"].Play();
            }
            if (chargeTime > maxChargeBeforeShooting)
            {
                ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = shootColor;
            }
        }

        void StopChargingRpc()
        {
            if (IsHost || IsServer)
            {
                StopChargingClientRpc();
            }
            else
            {
                StopChargingServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void StopChargingServerRpc()
        {
            StopChargingClientRpc();
        }

        [ClientRpc]
        void StopChargingClientRpc()
        {
            if (charging)
            {
                sourcesDict["ChargeSFX"].Stop();
                charging = false;
                chargeTime = 0f;
                ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = readyColor;
            }
        }

        void ResetCooldownRpc()
        {
            if(IsHost || IsServer)
            {
                ResetCooldownClientRpc();
            }
            else
            {
                ResetCooldownServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void ResetCooldownServerRpc()
        {
            ResetCooldownClientRpc();
        }

        [ClientRpc]
        void ResetCooldownClientRpc()
        {
            timeSinceShot = 0f;
            changeCooldownValueRpc(false);
            sourcesDict["CoolupSFX"].Play();
            ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = readyColor;
        }

        void ExplodeRpc()
        {
            if (IsHost || IsServer)
            {
                ExplodeClientRpc();
            }
            else
            {
                ExplodeServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void ExplodeServerRpc()
        {
            ExplodeClientRpc();
        }

        [ClientRpc]
        void ExplodeClientRpc()
        {
            if (!explode)
                return;
            explode = false;
            StopChargingRpc();
            activated = false;
            broken = true;
            sourcesDict["ExplosionSFX"].Play();
            this.insertedBattery.charge = 0f;
            ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = dangerColor;

            psDict["LingeringFire"].Play();
            psDict["MainBlast"].Play();
            int mask = LayerMask.GetMask(new string[] { "Player", "Enemies" });
            List<int> hitEntities = new List<int>();

            Collider[] colliders = Physics.OverlapSphere(this.gameObject.transform.position, explosionRadius, mask);
            foreach (Collider collider in colliders)
            {
                var player = collider.GetComponent<PlayerControllerB>();

                var playerColliderGameObject = collider.gameObject;
                var enemyColliderRoot = collider.transform.root;

                int damage = explosionDamage / 2;
                if (player != null && !hitEntities.Exists(i => i == playerColliderGameObject.GetInstanceID()))
                {
                    hitEntities.Add(playerColliderGameObject.GetInstanceID());
                    if (IsHost || IsServer)
                    {
                        player.DamagePlayerFromOtherClientClientRpc(damage, player.velocityLastFrame, (int)player.playerClientId, player.health - damage);
                    }
                    else
                    {
                        player.DamagePlayerFromOtherClientServerRpc(damage, player.velocityLastFrame, (int)player.playerClientId);
                    }
                }
                var enemy = enemyColliderRoot.GetComponent<EnemyAI>();

                if (enemy != null && !hitEntities.Exists(i => i == enemyColliderRoot.gameObject.GetInstanceID()))
                {
                    if (!enemy.isEnemyDead)
                    {
                        hitEntities.Add(enemyColliderRoot.gameObject.GetInstanceID());
                        if (enemy.creatureVoice != null && enemy.dieSFX != null)
                        {
                            enemy.creatureVoice.PlayOneShot(enemy.dieSFX);
                        }
                        enemy.KillEnemy(true);
                    }
                }
            }
        }

        void RepairRpc()
        {
            if (IsHost || IsServer)
            {
                RepairClientRpc();
            }
            else
            {
                RepairServerRpc();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void RepairServerRpc()
        {
            RepairClientRpc();
        }

        [ClientRpc]
        void RepairClientRpc()
        {
            broken = false;
            if (coolingDown.Value)
            {
                ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = unreadyColor;
            }
            else
            {
                ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = readyColor;
            }
        }

        void changeCooldownValueRpc(bool value)
        {
            if(IsHost || IsServer)
            {
                coolingDown.Value = value;
            }
            else
            {
                changeCooldownValueServerRpc(value);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void changeCooldownValueServerRpc(bool value)
        {
            coolingDown.Value = value;
        }
    }
}
