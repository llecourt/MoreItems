using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using GameNetcodeStuff;
using Steamworks;

namespace MoreItems.Behaviours
{
    internal class KarmaBehaviour : GrabbableObject
    {
        NetworkVariable<bool> coolingDown = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        NetworkVariable<bool> broken = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        float cooldown = 10f;
        float timeSinceShot = 0f;
        float maxChargeBeforeShooting = 2.5f;
        float maxChargeBeforeExplode = 3.5f;
        float chargeTime = 0f;
        float energyLostPerShot = 0.5f;
        
        int interval = 10;

        bool charging = false;
        bool activated = false;
        bool shoot = false;

        string[] sources = new string[] { "ChargeSFX", "ShootSFX", "TrailSFX", "WarningSFX", "ExplosionSFX", "CooldownSFX", "CoolupSFX", "OnCdShotSFX" };
        Dictionary<string, AudioSource> sourcesDict = new Dictionary<string, AudioSource>();
        string[] ps = new string[] { "MuzzleFlash", "Trail", "LingeringFire", "MainBlast" };
        Dictionary<string, ParticleSystem> psDict = new Dictionary<string, ParticleSystem>();

        Color readyColor = new Color32(20, 20, 20, 255);
        Color shootColor = new Color32(20, 200, 20, 255);
        Color dangerColor = new Color32(200, 20, 20, 255);
        Color unreadyColor = new Color32(250, 100, 0, 255);

        GameObject laserLight;
        GameObject ScopeChargeTimer;

        void Awake()
        {
            grabbable = true;
            grabbableToEnemies = true;
            sourcesDict = Utils.getAllAudioSources(this.gameObject, "KarmaSFX", sources);
            psDict = Utils.getAllParticleSystems(this.gameObject, "KarmaParticles", ps);
            ScopeChargeTimer = this.gameObject.transform.Find("ScopeChargeTimer").gameObject;
            laserLight = this.gameObject.transform.Find("laserLight").gameObject;

            this.insertedBattery.charge = 1f;
        }

        public override void PocketItem()
        {
            base.PocketItem();
            activated = false;
            StopChargingServerRpc(false);
            laserLight.GetComponent<Light>().enabled = false;
        }

        public override void EquipItem()
        {
            base.EquipItem();
            laserLight.GetComponent<Light>().enabled = true;
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            StopChargingServerRpc(false);
            activated = false;
            if (!coolingDown.Value && this.insertedBattery.charge > 0 && !broken.Value)
            {
                if (UnityEngine.Random.Range(1, 20) == 1)
                {
                    ShootServerRpc();
                }
            }
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            if(playerHeldBy != null && !isPocketed && wasOwnerLastFrame && !broken.Value)
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
            RepairServerRpc();
        }

        public override void Update()
        {
            base.Update();
            if (broken.Value)
                return;
            
            if (coolingDown.Value)
            {
                if (Time.frameCount % interval != 0)
                    return;

                timeSinceShot += Time.deltaTime * interval;
                if (timeSinceShot > cooldown)
                {
                    ResetCooldownServerRpc();
                }
            }
            else if (wasOwnerLastFrame)
            {
                if (Time.frameCount % interval != 0)
                    return;

                if (activated)
                {
                    StartChargingServerRpc();
                    if (chargeTime > maxChargeBeforeExplode)
                    {
                        ExplodeServerRpc();
                    }
                    chargeTime += Time.deltaTime * interval;
                }
                else
                {
                    if (chargeTime >= maxChargeBeforeShooting && chargeTime <= maxChargeBeforeExplode)
                    {
                        StopChargingServerRpc(true);
                        if(!shoot)
                        {
                            shoot = true;
                            ShootServerRpc();
                        }
                    }
                    else
                    {
                        StopChargingServerRpc(false);
                    }
                }
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
            sourcesDict["ShootSFX"].Play();
            sourcesDict["TrailSFX"].PlayDelayed(0.1f);
            psDict["MuzzleFlash"].Play();
            psDict["Trail"].Play();
            this.insertedBattery.charge -= energyLostPerShot;
            sourcesDict["CooldownSFX"].PlayDelayed(0.35f);
            changeCooldownValueServerRpc(true);
            ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = unreadyColor;

            if(playerHeldBy != null && playerHeldBy.playerClientId == GameNetworkManager.Instance.localPlayerController.playerClientId)
                HUDManager.Instance.ShakeCamera(ScreenShakeType.Big);

            RaycastHit hit;
            int mask = LayerMask.GetMask(new string[] { "Player", "Enemies", "Room" });
            if (Physics.Raycast(psDict["Trail"].transform.position, psDict["Trail"].transform.forward, out hit, float.MaxValue, mask))
            {
                var player = hit.collider.GetComponent<PlayerControllerB>();
                if(player != null)
                {
                    if (IsHost || IsServer)
                    {
                        player.DamagePlayerFromOtherClientClientRpc(100, psDict["Trail"].transform.forward, (int)player.playerClientId, player.health - 100);
                    }
                    else
                    {
                        player.DamagePlayerFromOtherClientServerRpc(100, psDict["Trail"].transform.forward, (int)player.playerClientId);
                    }
                }

                var enemy = hit.collider.transform.root.GetComponent<EnemyAI>();
                if(enemy != null)
                {
                    enemy.HitEnemy(100, playerHeldBy, true);
                }
            }
            shoot = false;
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

        [ServerRpc(RequireOwnership = false)]
        void StopChargingServerRpc(bool hasShot)
        {
            StopChargingClientRpc(hasShot);
        }

        [ClientRpc]
        void StopChargingClientRpc(bool hasShot)
        {
            if (charging)
            {
                charging = false;
                sourcesDict["ChargeSFX"].Stop();
                chargeTime = 0f;
                if(!hasShot)
                {
                    ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = readyColor;
                }
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
            changeCooldownValueServerRpc(false);
            sourcesDict["CoolupSFX"].Play();
            ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = readyColor;
        }

        [ServerRpc(RequireOwnership = false)]
        void ExplodeServerRpc()
        {
            ExplodeClientRpc();
        }

        [ClientRpc]
        void ExplodeClientRpc()
        {
            activated = false;
            changeBrokenValueServerRpc(true);
            StopChargingServerRpc(false);
            sourcesDict["ExplosionSFX"].Play();
            this.insertedBattery.charge = 0f;
            ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = dangerColor;

            psDict["LingeringFire"].Play();
            psDict["MainBlast"].Play();

            Utils.Explode(this.gameObject, 5f, 50, 180);
        }

        [ServerRpc(RequireOwnership = false)]
        void RepairServerRpc()
        {
            RepairClientRpc();
        }

        [ClientRpc]
        void RepairClientRpc()
        {
            changeBrokenValueServerRpc(false);
            if (coolingDown.Value)
            {
                ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = unreadyColor;
            }
            else
            {
                ScopeChargeTimer.GetComponent<MeshRenderer>().material.color = readyColor;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        void changeCooldownValueServerRpc(bool value)
        {
            coolingDown.Value = value;
        }

        [ServerRpc(RequireOwnership = false)]
        void changeBrokenValueServerRpc(bool value)
        {
            broken.Value = value;
        }
    }
}
