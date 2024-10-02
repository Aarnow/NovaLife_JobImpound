using Crosstales.Radio;
using JobImpound.Entities;
using Life;
using Life.BizSystem;
using Life.DB;
using Life.Network;
using Life.UI;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static JobImpound.Entities.JobImpound_Vehicle;
using mk = ModKit.Helper.TextFormattingHelper;
using MK = ModKit.Helper.VehicleHelper.Classes;

namespace JobImpound.Panels.Impound
{
    public class VehiclePanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public VehiclePanels(ModKit.ModKit context)
        {
            Context = context;
        }

        public async void VehiclePanel(Player player, bool isInPound = true)
        {
            //Query
            List<JobImpound_Vehicle> vehicles;
            string immobiliseStatus = VehicleStatus.Immobilise.ToString();
            string nonReclameStatus = VehicleStatus.NonReclame.ToString();
            if (isInPound) vehicles = await JobImpound_Vehicle.Query(v => v.Status == immobiliseStatus || v.Status == nonReclameStatus);
            else vehicles = await JobImpound_Vehicle.Query(v => v.Status != immobiliseStatus && v.Status != nonReclameStatus);
            

            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Fourrière - Véhicules disponibles", UIPanel.PanelType.TabPrice, player, () => VehiclePanel(player, isInPound));

            //Corps
            if (vehicles != null && vehicles.Count > 0)
            {
                foreach (var vehicle in vehicles)
                {
                    await vehicle.UpdateStatus();

                    panel.AddTabLine($"{(vehicle.ModelId != default ? $"{VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}" : "inconnu")}<br>{mk.Size($"{mk.Color($"{DateUtils.FormatUnixTimestamp(vehicle.CreatedAt)}", mk.Colors.Orange)}", 14)}", $"{mk.Color($"{(vehicle.Plate != null ? $"{vehicle.Plate}" : "inconnu")}", mk.Colors.Verbose)}<br>{mk.Color($"{EnumUtils.GetDisplayName(vehicle.LStatus)}", vehicle.ReturnColorOfStatus())}",VehicleUtils.GetIconId(vehicle.ModelId), _ =>
                    {
                        VehicleDetailsPanel(player, vehicle);
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
        public async void VehicleDetailsPanel(Player player, JobImpound_Vehicle vehicle)
        {
            //Query
            List<JobImpound_Reason> reason = await JobImpound_Reason.Query(r => r.Id == vehicle.ReasonId);
            List<MK.Vehicle> mk_vehicles = await MK.Vehicle.Query(v => v.ModelId == vehicle.ModelId);

            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Fourrière - détails du véhicule", UIPanel.PanelType.TabPrice, player, () => VehicleDetailsPanel(player, vehicle));

            //Corps
            panel.AddTabLine($"{mk.Color("Modèle:", mk.Colors.Info)} {VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}", "",VehicleUtils.GetIconId(vehicle.ModelId), _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Plaque:", mk.Colors.Info)} {(vehicle.Plate != null ? $"{vehicle.Plate}" : $"{mk.Color("inconnu", mk.Colors.Grey)}")}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });
            panel.AddTabLine($"{mk.Color("Raison:", mk.Colors.Info)} {(reason != null && reason.Count > 0 ? reason[0].Title : $"{mk.Color("inconnu", mk.Colors.Grey)}")}", _ => SetVehicleReason(player,vehicle));
            panel.AddTabLine($"{mk.Color("Preuve:", mk.Colors.Info)} {(vehicle.Evidence?.Length > 0 ? $"{vehicle.Evidence}" : $"{mk.Color("aucune", mk.Colors.Grey)}")}", _ => SetVehicleEvidence(player,vehicle));
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
            panel.AddTabLine($"{mk.Color("Créer par:", mk.Colors.Orange)} {(vehicle.CreatedBy != null ? $"{vehicle.CreatedBy}" : $"{mk.Color("aucune", mk.Colors.Grey)}")}", _ =>
            {
                player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                panel.Refresh();
            });

            if(vehicle.LStatus != VehicleStatus.Immobilise && vehicle.LStatus != VehicleStatus.NonReclame)
            {
                panel.AddTabLine($"{mk.Color("Libéré le:", mk.Colors.Orange)} {(vehicle.ReleasedAt != default ? DateUtils.FormatUnixTimestamp(vehicle.ReleasedAt) : "-")}", _ =>
                {
                    player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                    panel.Refresh();
                });
                panel.AddTabLine($"{mk.Color("Libéré par:", mk.Colors.Orange)} {(vehicle.ReleasedBy != null ? $"{vehicle.ReleasedBy}" : $"{mk.Color("aucune", mk.Colors.Grey)}")}", _ =>
                {
                    player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                    panel.Refresh();
                });
            }

            //Boutons
            if(vehicle.LStatus == VehicleStatus.Immobilise || vehicle.LStatus == VehicleStatus.NonReclame)
            {
                //verbalise + libere
                panel.CloseButtonWithAction("Verbaliser", () =>
                {
                    Player closestPlayer = player.GetClosestPlayer();

                    if (closestPlayer != null)
                    {
                        var vehicleInfo = Nova.v.GetVehicle(vehicle.VehicleId);
                        if (vehicleInfo != null && (vehicleInfo.permissions.owner.characterId == closestPlayer.character.Id || (vehicleInfo.bizId == closestPlayer.biz.Id && closestPlayer.character.Id == closestPlayer.biz.OwnerId)))
                        {
                            bool isBizOwner = vehicleInfo.bizId != default;
                            bool isPlayerOwner = vehicleInfo.permissions.owner.characterId != default;
                            double amountOfStorage = vehicle.GetAmountOfStorage();
                            double total = amountOfStorage + JobImpound._jobImpoundConfig.TowingCosts + JobImpound._jobImpoundConfig.ImpoundAdministrativeCosts + reason[0].Money;

                            VehicleFineRequestPanel(player, closestPlayer, vehicle, isBizOwner, reason[0], amountOfStorage, total);
                            player.Notify("Fourrière", $"Vous tendez l'amende à {closestPlayer.GetFullName()}", NotificationManager.Type.Info);
                            return Task.FromResult(true);
                        }
                        else player.Notify("Carte Grise", $"Ce citoyen n'est pas propriétaire du véhicule ou PDG de la société possédant le véhicule", NotificationManager.Type.Info);
                    }
                    else player.Notify("Fourrière", "Aucun citoyen n'est à proximité", NotificationManager.Type.Warning);

                    return Task.FromResult(false);
                });
            }
            if(vehicle.LStatus == VehicleStatus.NonReclame && mk_vehicles != null && mk_vehicles.Count > 0)
            {
                panel.CloseButtonWithAction($"{mk.Size($"Vendre<br>{mk_vehicles[0].Price}€", 14)}", async () =>
                {
                    Player closestPlayer = player.GetClosestPlayer();
                    Bizs cityHall = Nova.biz.FetchBiz(JobImpound._jobImpoundConfig.CityHallId);

                    if (cityHall != null)
                    {
                        if (closestPlayer != null)
                        {
                            if (closestPlayer.HasBiz() && Nova.biz.GetBizActivities(closestPlayer.biz.Id).First() == Activity.Type.Mecanic)
                            {
                                if (PermissionUtils.PlayerIsOwner(closestPlayer) || await PermissionUtils.PlayerCanManageTheBank(closestPlayer))
                                {
                                    VehicleSalesRequestPanel(player, closestPlayer, vehicle, mk_vehicles[0].Price);
                                    player.Notify("Fourrière", $"Vous proposez l'offre de vente à la société {closestPlayer.biz.BizName}", NotificationManager.Type.Info);
                                    return true;
                                }
                                else player.Notify("Fourrière", "Le citoyen à proximité n'a pas la permission d'acheter un véhicule pour sa société", NotificationManager.Type.Info);
                            }
                            else player.Notify("Fourrière", "Le citoyen à proximité n'est pas mécanicien", NotificationManager.Type.Info);
                        }
                        else player.Notify("Fourrière", "Aucun citoyen n'est à proximité", NotificationManager.Type.Warning);
                    }
                    else player.Notify("Fourrière", "La mairie n'est pas joignable", NotificationManager.Type.Warning);

                    return false;
                });
            }
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        public void VehicleFineRequestPanel(Player player, Player closestPlayer, JobImpound_Vehicle vehicle, bool isBizOwner, JobImpound_Reason reason, double amountOfStorage, double total)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Amende", UIPanel.PanelType.Text, closestPlayer, () => VehicleFineRequestPanel(player, closestPlayer, vehicle, isBizOwner, reason, amountOfStorage, total));

            //Corps
            panel.TextLines.Add($"{mk.Color($"{player.GetFullName()}", mk.Colors.Orange)}");
            panel.TextLines.Add($"tend l'amende permettant la libération");
            panel.TextLines.Add($"du véhicule immatriculé {mk.Color($"{vehicle.Plate}", mk.Colors.Orange)}");
            panel.TextLines.Add($"pour la somme de {mk.Color($"{total}", mk.Colors.Orange)}€");
            panel.TextLines.Add($"{mk.Size($"{mk.Italic($"{mk.Color($"{reason.Title} - {reason.Money}€", mk.Colors.Grey)}")}", 14)}");
            panel.TextLines.Add($"{mk.Size($"{mk.Italic($"{mk.Color($"Coûts de remorquage - {JobImpound._jobImpoundConfig.TowingCosts}€", mk.Colors.Grey)}")}", 14)}");
            panel.TextLines.Add($"{mk.Size($"{mk.Italic($"{mk.Color($"Frais de gardiennage - {amountOfStorage}€", mk.Colors.Grey)}")}", 14)}");
            panel.TextLines.Add($"{mk.Size($"{mk.Italic($"{mk.Color($"Frais administratifs - {JobImpound._jobImpoundConfig.ImpoundAdministrativeCosts}€", mk.Colors.Grey)}")}", 14)}");

            //Boutons
            panel.CloseButtonWithAction("Accepter", async () =>
            {
                if ((isBizOwner && closestPlayer.biz.Bank >= total) || (!isBizOwner && closestPlayer.character.Money >= total))
                {
                    vehicle.LStatus = VehicleStatus.Libere;
                    vehicle.ReleasedAt = DateUtils.GetCurrentTime();
                    vehicle.ReleasedBy = player.GetFullName();
                    if (await vehicle.Save())
                    {
                        if (isBizOwner)
                        {
                            closestPlayer.biz.Bank -= total;
                            closestPlayer.biz.Save();
                        }
                        else closestPlayer.AddMoney(-total, "JobImpound - Amende fourrière");
                        player.biz.Bank += total;
                        player.biz.Save();

                        player.Notify("Fourrière", $"Le citoyen accepte l'amende. Votre société reçoit {total}€", NotificationManager.Type.Success);
                        closestPlayer.Notify("Fourrière", $"Vous acceptez de pyaer l'amende de {total}€", NotificationManager.Type.Success);
                        return true;
                    }
                    else
                    {
                        player.Notify("Fourrière", "Nous n'avons pas pu libérer le véhicule", NotificationManager.Type.Error);
                        closestPlayer.Notify("Fourrière", "Nous n'avons pas pu libérer votre véhicule", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("Fourrière", "Le citoyen n'est pas en mesure de régler l'amende", NotificationManager.Type.Info);
                    closestPlayer.Notify("Fourrière", "Vous ne pouvez pas régler l'amende", NotificationManager.Type.Info);
                }
                return false;
            });
            panel.CloseButtonWithAction("Refuser", () =>
            {
                player.Notify("Fourrière", "Le citoyen refuse de payer l'amende", NotificationManager.Type.Info);
                closestPlayer.Notify("Fourrière", "Vous refusez de payer l'amende", NotificationManager.Type.Info);
                return Task.FromResult(true);
            });

            //Affichage
            panel.Display();
        }
        public void VehicleSalesRequestPanel(Player player, Player closestPlayer, JobImpound_Vehicle vehicle, double price)
        {
            //Query
            Bizs cityHall = Nova.biz.FetchBiz(JobImpound._jobImpoundConfig.CityHallId);

            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Vente d'un véhicule non réclamé", UIPanel.PanelType.Text, closestPlayer, () => VehicleSalesRequestPanel(player, closestPlayer, vehicle, price));

            //Corps
            panel.TextLines.Add($"{mk.Color($"{player.GetFullName()}", mk.Colors.Orange)}");
            panel.TextLines.Add($"propose une {VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}");
            panel.TextLines.Add($"pour la somme de {mk.Color($"{price}", mk.Colors.Orange)}€");
            panel.TextLines.Add($"{mk.Size($"{mk.Italic($"{mk.Color("(prix non-négociable)", mk.Colors.Grey)}")}", 14)}");

            //Boutons
            panel.CloseButtonWithAction("Accepter", async () =>
            {
                if (closestPlayer.biz.Bank >= price)
                {
                    vehicle.LStatus = VehicleStatus.Vendu;
                    vehicle.ReleasedAt = DateUtils.GetCurrentTime();
                    vehicle.ReleasedBy = player.GetFullName();

                    if (await vehicle.Save())
                    {

                        double commission = price * (JobImpound._jobImpoundConfig.CommissionPercentage / 100);


                        closestPlayer.biz.Bank -= price;
                        closestPlayer.biz.Save();
                        player.AddMoney(commission, "JFourrière - Vente d'un véhicule non-réclamé");
                        cityHall.Bank += price - commission;
                        cityHall.Save();

                        LifeVehicle currentVehicle = Nova.v.GetVehicle(vehicle.VehicleId);
                        currentVehicle.permissions.owner.characterId = 0;
                        currentVehicle.bizId = closestPlayer.biz.Id;
                        currentVehicle.Save();


                        player.Notify("Fourrière", $"Vous avez vendu une {VehicleUtils.GetModelNameByModelId(vehicle.ModelId)} pour {price}€. Vous obtenez {commission}€ de commission.", NotificationManager.Type.Success);
                        closestPlayer.Notify("Fourrière", $"Votre société vient d'acquérir une {VehicleUtils.GetModelNameByModelId(vehicle.ModelId)} pour {price}€.", NotificationManager.Type.Success);

                        return true;
                    }
                    else
                    {
                        player.Notify("Fourrière", "Nous n'avons pas pu procéder à la vente", NotificationManager.Type.Error);
                        closestPlayer.Notify("Fourrière", "Nous n'avons pas pu procéder à la vente", NotificationManager.Type.Error);
                    }
                }
                else
                {
                    player.Notify("Fourrière", $"La société {player.biz.BizName} n'a pas les moyens d'acheter ce véhicule", NotificationManager.Type.Info);
                    closestPlayer.Notify("Fourrière", "Votre société n'a pas les moyens d'acheter ce véhicule", NotificationManager.Type.Info);
                }

                return false;
            });
            panel.CloseButtonWithAction("Refuser", () =>
            {
                player.Notify("Fourrière", "Le citoyen refuse d'acheter ce véhicule", NotificationManager.Type.Info);
                closestPlayer.Notify("Fourrière", "Vous refusez d'acheter ce véhicule", NotificationManager.Type.Info);
                return Task.FromResult(true);
            });

            //Affichage
            panel.Display();
        }
        public void VehicleSearchPanel(Player player)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Rechercher un véhicule", UIPanel.PanelType.Input, player, () => VehicleSearchPanel(player));

            //Corps
            panel.TextLines.Add("Indiquer la plaque d'immatriculation");
            panel.inputPlaceholder = "exemple MD-123-RP";

            //Boutons
            panel.NextButton("Rechercher", async () =>
            {
                if (panel.inputText.Length > 0)
                {
                    List<JobImpound_Vehicle> occurences = await JobImpound_Vehicle.Query(v => v.Plate.Contains(panel.inputText));

                    if(occurences != null && occurences.Count > 0)
                    {
                        VehicleSearchResultPanel(player, occurences, panel.inputText);
                        return;
                    }
                    else player.Notify("Fourrière", "Aucun occurence", NotificationManager.Type.Info);
                }
                else player.Notify("Fourrière", "Format incorrect", NotificationManager.Type.Warning);

                panel.Refresh();
            });
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        public async void VehicleSearchResultPanel(Player player, List<JobImpound_Vehicle> vehicles, string input)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Fourrière - Résultat de la recherche", UIPanel.PanelType.TabPrice, player, () => VehicleSearchResultPanel(player, vehicles, input));

            //Corps
            if (vehicles.Any())
            {
                foreach (var vehicle in vehicles)
                {
                    await vehicle.UpdateStatus();

                    string plate = mk.ReturnTextWithOccurence(vehicle.Plate, input, mk.Colors.Orange);

                    panel.AddTabLine($"{(vehicle.ModelId != default ? $"{VehicleUtils.GetModelNameByModelId(vehicle.ModelId)}" : "inconnu")}<br>{mk.Size($"{mk.Color($"{DateUtils.FormatUnixTimestamp(vehicle.CreatedAt)}", mk.Colors.Orange)}", 14)}", $"{plate}<br>{mk.Color($"{EnumUtils.GetDisplayName(vehicle.LStatus)}", vehicle.ReturnColorOfStatus())}", VehicleUtils.GetIconId(vehicle.ModelId), _ =>
                    {
                        VehicleDetailsPanel(player, vehicle);
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


        #region VEHICLE SETTERS
        public void SetVehicleReason(Player player, JobImpound_Vehicle vehicle)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - modifier la raison", UIPanel.PanelType.TabPrice, player, () => SetVehicleReason(player, vehicle));

            //Corps
            foreach (VehicleStatus status in Enum.GetValues(typeof(VehicleStatus)))
            {
                panel.AddTabLine($"{status.GetDisplayName()}", async _ =>
                {
                    vehicle.LStatus = status;
                    var result = await vehicle.Save();
                    if (result) panel.Previous();
                    else player.Notify("Fourrière", "Nous n'avons pas pu enregistrer cette modification", NotificationManager.Type.Error);
                });
            }

            //Boutons
            panel.AddButton("Sélectionner", _ => { panel.SelectTab(); });
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
        public void SetVehicleEvidence(Player player, JobImpound_Vehicle vehicle)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Véhicule - preuves", UIPanel.PanelType.Input, player, () => SetVehicleEvidence(player, vehicle));

            //Corps
            panel.inputPlaceholder = "renseigner le nom du post discord contenant vos photos";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (panel.inputText.Length >= 3)
                {
                    vehicle.Evidence = panel.inputText;
                    return await vehicle.Save();
                }
                else
                {
                    player.Notify("Fourrière", "3 lettres minimum", NotificationManager.Type.Warning);
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
