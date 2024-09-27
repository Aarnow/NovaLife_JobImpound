using JobImpound.Entities;
using Life;
using Life.DB;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using System.Collections.Generic;
using System.Linq;
using static JobImpound.Entities.JobImpound_Vehicle;
using mk = ModKit.Helper.TextFormattingHelper;

namespace JobImpound.Panels.Impound
{
    public class ImpoundPanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public ImpoundPanels(ModKit.ModKit context)
        {
            Context = context;
        }

        public async void ImpoundVehiclePanel(Player player)
        {
            //Query
            string immobiliseStatus = VehicleStatus.Immobilise.ToString();
            string nonReclameStatus = VehicleStatus.NonReclame.ToString();
            List<JobImpound_Vehicle> vehicles = await JobImpound_Vehicle.Query(v => v.Status == immobiliseStatus || v.Status == nonReclameStatus);

            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Fourrière - Véhicules disponibles", UIPanel.PanelType.TabPrice, player, () => ImpoundVehiclePanel(player));

            //Corps
            if (vehicles.Any())
            {
                foreach (var vehicle in vehicles)
                {
                    panel.AddTabLine($"{(vehicle.ModelId != default ? $"{VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}" : "inconnu")}", $"{(vehicle.Plate != null ? $"{vehicle.Plate}" : "inconnu")}", vehicle.ModelId != default ? VehicleUtils.GetIconId(vehicle.ModelId) : IconUtils.Others.None.Id, _ =>
                    {
                        //LawEnforcementCreateVehiclePanel(player, vehicle);
                    });
                }
            }
            else panel.AddTabLine("Aucun véhicule", _ => { });


            //Boutons
            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public async void ImpoundVehicleDetailsPanel(Player player, JobImpound_Vehicle vehicle)
        {
            //Query
            List<JobImpound_Reason> reason = await JobImpound_Reason.Query(r => r.Id == vehicle.ReasonId);
            Characters createdBy = await LifeDB.db.Table<Characters>().Where(c => c.Id == vehicle.CreatedBy).FirstOrDefaultAsync();
            Characters archivedBy = await LifeDB.db.Table<Characters>().Where(c => c.Id == vehicle.ArchivedBy).FirstOrDefaultAsync();

            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Fourrière - détails du véhicule", UIPanel.PanelType.TabPrice, player, () => ImpoundVehicleDetailsPanel(player, vehicle));

            //Corps
            panel.AddTabLine($"{mk.Color("Modèle:", mk.Colors.Info)} {(vehicle.Plate != null ? $"{VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}" : $"{mk.Color("inconnu", mk.Colors.Grey)}")}", "", vehicle.ModelId != default ? VehicleUtils.GetIconId(vehicle.ModelId) : IconUtils.Others.None.Id, _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Plaque:", mk.Colors.Info)} {(vehicle.Plate != null ? $"{vehicle.Plate}" : $"{mk.Color("inconnu", mk.Colors.Grey)}")}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Propriétaire:", mk.Colors.Info)} {(vehicle.BizId != default ? $"{vehicle.BizName}" : $"{(vehicle.OwnerId != default ? $"{vehicle.OwnerFullName}" : $"{mk.Color("inconnu", mk.Colors.Grey)}")}")}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Raison:", mk.Colors.Info)} {(reason != null && reason.Count > 0 ? reason[0].Title : $"{mk.Color("inconnu", mk.Colors.Grey)}")}", _ => { });
            panel.AddTabLine($"{mk.Color("Preuve:", mk.Colors.Info)} {(vehicle.Evidence.Length > 0 ? $"{vehicle.Evidence}" : $"{mk.Color("aucune", mk.Colors.Grey)}")}", _ => { });
            panel.AddTabLine($"{mk.Color("Statut:", mk.Colors.Info)} {EnumUtils.GetDisplayName(vehicle.LStatus)}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });

            panel.AddTabLine($"{mk.Color("Créer le:", mk.Colors.Orange)} {(vehicle.CreatedAt != default ? DateUtils.FormatUnixTimestamp(vehicle.CreatedAt) : "-")}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Créer par:", mk.Colors.Orange)} {(vehicle.CreatedBy != default ? $"{createdBy.Firstname} {createdBy.Lastname}" : "-")}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Archivé par:", mk.Colors.Orange)} {(vehicle.ArchivedBy != default ? $"{archivedBy.Firstname} {archivedBy.Lastname}" : "-")}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });

            //Boutons
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
