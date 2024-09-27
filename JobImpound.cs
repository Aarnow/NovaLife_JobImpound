using Life;
using Life.BizSystem;
using Life.Network;
using Life.UI;
using Life.VehicleSystem;
using Mirror;
using ModKit.Helper;
using ModKit.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using _menu = AAMenu.Menu;

namespace JobImpound
{
    public class JobImpound : ModKit.ModKit
    {
        #region CONFIG
        public float MaxDistance = 20.0f;
        public int UnlockingDuration = 60;
        #endregion
        private Dictionary<uint, Coroutine> activeCoroutines = new Dictionary<uint, Coroutine>();

        public JobImpound(IGameAPI api) : base(api)
        {
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();

            InsertMenu();
            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        #region COROUTINES
        public IEnumerator Cycle(Player player, Vehicle towTruck, Vehicle target)
        {
            while (true)
            { 
                float distance = Vector3.Distance(towTruck.transform.position, target.transform.position);
                
                if (distance > MaxDistance)
                {
                    player.Notify("Avertissement", "Vous ne pouvez pas vous éloigner de votre dépanneuse.", NotificationManager.Type.Warning);
                    target.EjectAll();
                    Nova.v.TryReplaceCarWithFake(target, true);
                    ModKit.Internal.Logger.LogError($"{PluginInformations.SourceName}", "Un  joueur vient de s'éloigner de sa dépanneuse avec un véhicule qu'il devait remorquer");
                    yield break;
                } else if(player.setup.driver.NetworkcurrentVehicle == 0)
                {
                    target.NetworkengineStarted = false;
                    target.newController.SetEngine(false);
                    if(!target.newController.EngineRunning) yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
        #endregion

        public void InsertMenu()
        {
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere, Activity.Type.Mecanic }, null, "Dépannage", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                TroubleshootingPanel(player);
            });
        }

        public void TroubleshootingPanel(Player player)
        {
            //Déclaration
            Panel panel = PanelHelper.Create("Dépannage - Véhicule", UIPanel.PanelType.Tab, player, () => TroubleshootingPanel(player));

            //Corps
            panel.AddTabLine("Déverrouiller", async _ =>
            {
                var vehicle = player.GetClosestVehicle();
                vehicle.NetworkisLocked = false;
                player.Notify("Dépannage", "Véhicule déverrouillé pendant 60 secondes.", NotificationManager.Type.Info);
                await Task.Delay(1000 * UnlockingDuration);
                vehicle.NetworkisLocked = true;
            });
            panel.AddTabLine("Démarrer", async _ =>
            {
                if (player.setup.driver.NetworkcurrentVehicle != 0 && player.setup.driver.seatId == 0)
                {
                    Vehicle[] allVehicles = UnityEngine.Object.FindObjectsOfType<Vehicle>();
                    bool towTruckNearby = false;
                    foreach (var vehicle in allVehicles.Where(v => v.bizId == player.biz.Id))
                    {
                        float distance = Vector3.Distance(player.setup.driver.transform.position, vehicle.transform.position);
                        if (distance < MaxDistance && Nova.v.GetVehicle(vehicle.vehicleDbId).modelId == 12)
                        {
                            towTruckNearby = true;
                            player.setup.driver.vehicle.NetworkengineStarted = true;
                            player.Notify("Dépannage", "Vous tentez de démarrer le véhicule...", NotificationManager.Type.Info);

                            await Task.Delay(1500);
                            if (!player.setup.driver.vehicle.newController.EngineRunning) player.Notify("Dépannage", "Le véhicule ne démarre pas.", NotificationManager.Type.Warning);
                            else
                            {
                                if (activeCoroutines.ContainsKey(player.netId)) player.setup.StopCoroutine(activeCoroutines[player.netId]);
                                player.Notify("Dépannage", "Le véhicule est démarré !", NotificationManager.Type.Success);
                                activeCoroutines[player.netId] = player.setup.StartCoroutine(Cycle(player, NetworkServer.spawned[vehicle.netId].GetComponent<Vehicle>(), player.setup.driver.vehicle));
                            }
                            break;
                        }
                    }
                    if(!towTruckNearby) player.Notify("Dépannage", "Vous êtes trop loin de votre dépanneuse", NotificationManager.Type.Info);
                }
                if (!player.setup.driver.vehicle.newController.EngineRunning) panel.Refresh();
            });

            // Boutons
            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.BizPanel(player, AAMenu.AAMenu.menu.BizTabLines));
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
