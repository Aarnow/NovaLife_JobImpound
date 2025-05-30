﻿using JobImpound.Entities;
using Life;
using Life.Network;
using Life.UI;
using Life.VehicleSystem;
using Mirror;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace JobImpound.Panels.Skill
{
    public class ImpoundSkillPanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public ImpoundSkillPanels(ModKit.ModKit context)
        {
            Context = context;
        }

        #region COROUTINES
        public static IEnumerator Cycle(Player player, Vehicle towTruck, Vehicle target)
        {
            while (true)
            {
                float distance = Vector3.Distance(towTruck.transform.position, target.transform.position);

                if (distance > JobImpound._jobImpoundConfig.MaxDistance)
                {
                    player.Notify("Avertissement", "Vous ne pouvez pas vous éloigner de votre dépanneuse.", NotificationManager.Type.Warning);
                    target.EjectAll();
                    Nova.v.TryReplaceCarWithFake(target, true);
                    ModKit.Internal.Logger.LogError($"Fourrière", "Un  joueur vient de s'éloigner de sa dépanneuse avec un véhicule qu'il devait remorquer");
                    yield break;
                }
                else if (player.setup.driver.NetworkcurrentVehicle == 0)
                {
                    target.NetworkengineStarted = false;
                    target.newController.SetEngine(false);
                    if (!target.newController.EngineRunning) yield break;
                }
                yield return new WaitForSeconds(0.5f);
            }
        }
        #endregion

        public void TroubleshootingPanel(Player player)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Dépannage - Véhicule", UIPanel.PanelType.Tab, player, () => TroubleshootingPanel(player));

            //Corps
            panel.AddTabLine("Démarrer", async _ =>
            {
                if (player.setup.driver.NetworkcurrentVehicle != 0 && player.setup.driver.seatId == 0)
                {
                    Vehicle[] allVehicles = Object.FindObjectsOfType<Vehicle>();
                    bool towTruckNearby = false;
                    foreach (var vehicle in allVehicles.Where(v => v.bizId == player.biz.Id))
                    {
                        float distance = Vector3.Distance(player.setup.driver.transform.position, vehicle.transform.position);
                        if (distance < JobImpound._jobImpoundConfig.MaxDistance && Nova.v.GetVehicle(vehicle.vehicleDbId).modelId == 12)
                        {
                            towTruckNearby = true;
                            player.setup.driver.vehicle.NetworkengineStarted = true;
                            player.Notify("Dépannage", "Vous tentez de démarrer le véhicule...", NotificationManager.Type.Info);

                            await Task.Delay(1500);
                            if (!player.setup.driver.vehicle.newController.EngineRunning) player.Notify("Dépannage", "Le véhicule ne démarre pas.", NotificationManager.Type.Warning);
                            else
                            {
                                if (JobImpound.activeCoroutines.ContainsKey(player.netId)) player.setup.StopCoroutine(JobImpound.activeCoroutines[player.netId]);
                                player.Notify("Dépannage", "Le véhicule est démarré !", NotificationManager.Type.Success);
                                JobImpound.activeCoroutines[player.netId] = player.setup.StartCoroutine(Cycle(player, NetworkServer.spawned[vehicle.netId].GetComponent<Vehicle>(), player.setup.driver.vehicle));
                            }
                            break;
                        }
                    }
                    if (!towTruckNearby) player.Notify("Dépannage", "Vous êtes trop loin de votre dépanneuse", NotificationManager.Type.Info);
                }
                if (!player.setup.driver.vehicle.newController.EngineRunning) panel.Refresh();
            });
            panel.AddTabLine("Déverrouiller", async _ =>
            {
                Vehicle vehicle = VehicleUtils.GetClosestVehicle(player, 4,new List<int> {JobImpound.TOWTRUCK_ID});
                if(vehicle != null)
                {
                    vehicle.NetworkisLocked = false;
                    player.Notify("Dépannage", $"Véhicule déverrouillé pendant {JobImpound._jobImpoundConfig.UnlockingDuration} secondes.", NotificationManager.Type.Info);
                    await Task.Delay(1000 * JobImpound._jobImpoundConfig.UnlockingDuration);
                    vehicle.NetworkisLocked = true;
                }
                else player.Notify("Dépannage", $"Aucun véhicule à proximité", NotificationManager.Type.Info);
            });

            //Boutons
            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.BizPanel(player));
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        #region IMMOBILISE
        public async void ImmobiliseVehicleReasonPanel(Player player, JobImpound_Vehicle vehicle)
        {
            //Query
            List<JobImpound_Reason> query = await JobImpound_Reason.QueryAll();

            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Sélectionner l'infraction", UIPanel.PanelType.TabPrice, player, () => ImmobiliseVehicleReasonPanel(player, vehicle));

            //Corps
            if(query != null && query.Count > 0)
            {
                foreach (var reason in query)
                {
                    panel.AddTabLine($"{reason.Title}", $"{reason.Money}", reason.IconItem != default ? ItemUtils.GetIconIdByItemId(reason.IconItem) : IconUtils.Others.None.Id, _ =>
                    {
                        vehicle.ReasonId = reason.Id;
                        ImmobiliseVehicleEvidencePanel(player, vehicle);
                    });
                }
                panel.NextButton("Sélectionner", () => panel.SelectTab());
            }
            else panel.AddTabLine("Aucune infraction", _ => { });

            //Boutons
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.BizPanel(player));
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

            public void ImmobiliseVehicleEvidencePanel(Player player, JobImpound_Vehicle vehicle)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Preuve de l'infraction", UIPanel.PanelType.Input, player, () => ImmobiliseVehicleEvidencePanel(player, vehicle));

            //Corps
            panel.TextLines.Add("Renseigner des informations permettant de consulter les preuves de l'infraction");

            //Boutons
            panel.CloseButtonWithAction("Enregistrer", async () =>
            {
                vehicle.Evidence = panel.inputText;
                vehicle.CreatedAt = DateUtils.GetCurrentTime();
                vehicle.CreatedBy = player.GetFullName();
                if (await vehicle.Save())
                {
                    player.Notify("Fourrière", "Immobilisation enregistré", NotificationManager.Type.Success);
                    return true;
                }
                else
                {
                    player.Notify("Fourrière", "Nous n'avons pas pu enregistrer cette immobilisation", NotificationManager.Type.Error);
                    return false;
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        #endregion
    }
}
