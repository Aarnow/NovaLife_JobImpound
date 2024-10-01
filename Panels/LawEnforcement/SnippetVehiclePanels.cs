using JobImpound.Entities;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using static JobImpound.Entities.JobImpound_Vehicle;
using System.Collections.Generic;
using Life.Network;
using Life.UI;
using mk = ModKit.Helper.TextFormattingHelper;
using Life;

namespace JobImpound.Panels.LawEnforcement
{
    public class SnippetVehiclePanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public SnippetVehiclePanels(ModKit.ModKit context)
        {
            Context = context;
        }

        public async void SnippetVehiclePanel(Player player)
        {
            //Query
            List<JobImpound_Vehicle> vehicles;
            string immobiliseStatus = VehicleStatus.Immobilise.ToString();
            string nonReclameStatus = VehicleStatus.NonReclame.ToString();
            string saisiStatus = VehicleStatus.Saisi.ToString();
            vehicles = await JobImpound_Vehicle.Query(v => v.Status == immobiliseStatus || v.Status == nonReclameStatus || v.Status == saisiStatus);

            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Central - Véhicules à la fourrière", UIPanel.PanelType.TabPrice, player, () => SnippetVehiclePanel(player));

            //Corps
            if (vehicles != null && vehicles.Count > 0)
            {
                foreach (var vehicle in vehicles)
                {
                    await vehicle.UpdateStatus();

                    panel.AddTabLine($"{(vehicle.ModelId != default ? $"{VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}" : "inconnu")}<br>{mk.Size($"{mk.Color($"{DateUtils.FormatUnixTimestamp(vehicle.CreatedAt)}", mk.Colors.Orange)}", 14)}", $"{mk.Color($"{(vehicle.Plate != null ? $"{vehicle.Plate}" : "inconnu")}", mk.Colors.Verbose)}<br>{mk.Color($"{EnumUtils.GetDisplayName(vehicle.LStatus)}", vehicle.ReturnColorOfStatus())}", VehicleUtils.GetIconId(vehicle.ModelId), _ =>
                    {
                        SnippetVehicleDetailsPanel(player, vehicle);
                    });
                }
            }
            else panel.AddTabLine("Aucun véhicule", _ => { });


            //Boutons
            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.ProximityBizPanel(player, AAMenu.AAMenu.menu.ProximityBizTabLines);
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public async void SnippetVehicleDetailsPanel(Player player, JobImpound_Vehicle vehicle, bool isLocked = false, bool isFree = false)
        {
            //Query
            List<JobImpound_Reason> reason = await JobImpound_Reason.Query(r => r.Id == vehicle.ReasonId);

            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Central - détails d'un véhicule à la fourrière", UIPanel.PanelType.TabPrice, player, () => SnippetVehicleDetailsPanel(player, vehicle, isLocked, isFree));

            //Corps
            panel.AddTabLine($"{mk.Color("Modèle:", mk.Colors.Info)} {VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}", "", VehicleUtils.GetIconId(vehicle.ModelId), _ => {});
            panel.AddTabLine($"{mk.Color("Plaque:", mk.Colors.Info)} {(vehicle.Plate != null ? $"{vehicle.Plate}" : $"{mk.Color("inconnu", mk.Colors.Grey)}")}", _ =>{});
            panel.AddTabLine($"{mk.Color("Raison:", mk.Colors.Info)} {(reason != null && reason.Count > 0 ? reason[0].Title : $"{mk.Color("inconnu", mk.Colors.Grey)}")}", _ => {});
            panel.AddTabLine($"{mk.Color("Preuve:", mk.Colors.Info)} {(vehicle.Evidence?.Length > 0 ? $"{vehicle.Evidence}" : $"{mk.Color("aucune", mk.Colors.Grey)}")}", _ => {});
            panel.AddTabLine($"{mk.Color("Statut:", mk.Colors.Info)} {EnumUtils.GetDisplayName(vehicle.LStatus)}", _ =>{});

            //Boutons
            if (vehicle.LStatus != VehicleStatus.Saisi && !isLocked) panel.NextButton("Saisir", () => SnippetVehicleDetailsPanel(player, vehicle, true, false));
            else if(vehicle.LStatus != VehicleStatus.Saisi && isLocked) panel.PreviousButtonWithAction($"{mk.Size("Confirmer la<br>saisie", 12)}", async () =>
            {
                vehicle.LStatus = VehicleStatus.Saisi;
                if (await vehicle.Save())
                {
                    player.Notify("Central", "Modification enregistrée", NotificationManager.Type.Success);
                    return true;
                }
                else player.Notify("Central", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                return false;
            });

            if (vehicle.LStatus == VehicleStatus.Saisi && !isFree) panel.NextButton("Libérer", () => SnippetVehicleDetailsPanel(player, vehicle, false, true));
            else if(vehicle.LStatus == VehicleStatus.Saisi && isFree)
            {               
                panel.PreviousButtonWithAction($"{mk.Size("Confirmer la<br>libération", 12)}", async () =>
                {
                    vehicle.LStatus = VehicleStatus.Immobilise;
                    await vehicle.UpdateStatus();

                    if (await vehicle.Save())
                    {
                        player.Notify("Central", "Modification enregistrée", NotificationManager.Type.Success);
                        return true;
                    }
                    else player.Notify("Central", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                    return false;
                });
            }

            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
