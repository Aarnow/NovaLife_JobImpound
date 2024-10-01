using JobImpound.Entities;
using Life;
using Life.DB;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using System.Collections.Generic;
using mk = ModKit.Helper.TextFormattingHelper;

namespace JobImpound.Panels.Impound
{
    public class ReasonPanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public ReasonPanels(ModKit.ModKit context)
        {
            Context = context;
        }

        public async void ReasonPanel(Player player, bool isArchived = false)
        {
            //Query
            List<JobImpound_Reason> reasons = await JobImpound_Reason.QueryAll();

            //Déclaration
            Panel panel = Context.PanelHelper.Create($"Fourrière - Infractions", UIPanel.PanelType.TabPrice, player, () => ReasonPanel(player, isArchived));

            //Corps
            panel.AddTabLine($"{mk.Color("Ajouter une nouvelle infraction", mk.Colors.Info)}", _ =>
            {
                JobImpound_Reason newReason = new JobImpound_Reason();
                ReasonDetailsPanel(player, newReason);
            });


            if (reasons != null && reasons.Count > 0)
            {
                foreach (var reason in reasons)
                {
                    panel.AddTabLine($"{(reason.Title != null ? $"{reason.Title}" : "à définir")}", $"{reason.Money} €", reason.IconItem != default ? ItemUtils.GetIconIdByItemId(reason.IconItem) : IconUtils.Others.None.Id, _ =>
                    {
                        ReasonDetailsPanel(player, reason);
                    });
                }
            }
            else panel.AddTabLine("Aucune infractions", _ => { });

            //Boutons
            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public async void ReasonDetailsPanel(Player player, JobImpound_Reason reason)
        {
            //Query
            Characters createdBy = await LifeDB.db.Table<Characters>().Where(c => c.Id == reason.CreatedBy).FirstOrDefaultAsync();

            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Ajout d'une nouvelle infraction", UIPanel.PanelType.TabPrice, player, () => ReasonDetailsPanel(player, reason));

            //Corps
            panel.AddTabLine($"{mk.Color("Titre:", mk.Colors.Info)} {(reason.Title != null ? $"{reason.Title}" : "à définir")}", _ => SetReasonTitle(player, reason));

            if (reason.Title != null)
            {

                panel.AddTabLine($"{mk.Color("Montant de l'amende:", mk.Colors.Info)}  {reason.Money}€", _ => SetReasonMoney(player, reason));
                panel.AddTabLine($"{mk.Color("Icône:", mk.Colors.Info)}  [{reason.IconItem}] {ItemUtils.GetItemById(reason.IconItem)?.itemName}","", reason.IconItem != default ? ItemUtils.GetIconIdByItemId(reason.IconItem) : IconUtils.Others.None.Id, _ => SetReasonIcon(player, reason));

                panel.AddTabLine($"{mk.Color("Créer le:", mk.Colors.Orange)} {(reason.CreatedAt != default ? DateUtils.FormatUnixTimestamp(reason.CreatedAt) : "-")}", _ =>
                {
                    player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                    panel.Refresh();
                });
                panel.AddTabLine($"{mk.Color("Créer par:", mk.Colors.Orange)} {(reason.CreatedBy != default ? $"{createdBy.Firstname} {createdBy.Lastname}" : "-")}", _ =>
                {
                    player.Notify("Central", "Vous ne pouvez pas modifier cette valeur", NotificationManager.Type.Info);
                    panel.Refresh();
                });
            }

            //Boutons
            panel.NextButton("Modifier", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        #region OFFENSE SETTERS
        public void SetReasonTitle(Player player, JobImpound_Reason reason)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Titre de l'infraction", UIPanel.PanelType.Input, player, () => SetReasonTitle(player, reason));

            //Corps
            panel.inputPlaceholder = "Titre de l'infraction";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (panel.inputText.Length >= 3)
                {
                    reason.Title = panel.inputText;

                    reason.CreatedAt = DateUtils.GetCurrentTime();
                    reason.CreatedBy = player.character.Id;
                    if (await reason.Save())
                    {
                        player.Notify("Fourrière", $"Modification enregistrée", NotificationManager.Type.Success);
                        return true;
                    }
                    else
                    {
                        player.Notify("Fourrière", $"Nous n'avons pas pu enregistrer cette modificatione", NotificationManager.Type.Error);
                        return false;
                    }
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

        public void SetReasonMoney(Player player, JobImpound_Reason reason)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Montant de l'amende", UIPanel.PanelType.Input, player, () => SetReasonMoney(player, reason));

            //Corps
            panel.inputPlaceholder = "Montant de l'amende";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (panel.inputText.Length > 0)
                {
                    if (int.TryParse(panel.inputText, out int money))
                    {
                        if (money > 0)
                        {
                            reason.Money = money;
                            if (await reason.Save())
                            {
                                player.Notify("Fourrière", $"Modification enregistrée", NotificationManager.Type.Success);
                                return true;
                            }
                            else
                            {
                                player.Notify("Fourrière", $"Nous n'avons pas pu enregistrer cette modificatione", NotificationManager.Type.Error);
                                return false;
                            }
                        }
                        else
                        {
                            player.Notify("Fourrière", "Montant de l'amende négatif", NotificationManager.Type.Warning);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("Fourrière", "Format incorrect", NotificationManager.Type.Warning);
                        return false;
                    }
                }
                else
                {
                    player.Notify("Fourrière", "Définir le montant de l'amende", NotificationManager.Type.Warning);
                    return false;
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void SetReasonIcon(Player player, JobImpound_Reason reason)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Icône de l'infraction", UIPanel.PanelType.Input, player, () => SetReasonIcon(player, reason));

            //Corps
            panel.TextLines.Add("Renseigner l'identifiant de l'objet pour utiliser son icône");
            panel.TextLines.Add($"{mk.Size($"{mk.Italic($"{mk.Color("(utiliser le wiki Nova-Life : Amboise)", mk.Colors.Grey)}")}", 14)}");
            panel.inputPlaceholder = "exemple pour l'icône du panneau stop: 1039";

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                if (int.TryParse(panel.inputText, out int itemId))
                {
                    if (ItemUtils.GetItemById(itemId) != null)
                    {
                        reason.IconItem = itemId;
                        if (await reason.Save())
                        {
                            player.Notify("Fourrière", $"Modification enregistrée", NotificationManager.Type.Success);
                            return true;
                        }
                        else
                        {
                            player.Notify("Fourrière", $"Nous n'avons pas pu enregistrer cette modificatione", NotificationManager.Type.Error);
                            return false;
                        }
                    }
                    else
                    {
                        player.Notify("Fourrière", $"Aucun objet ne correspond à l'ID {itemId}", NotificationManager.Type.Warning);
                        return false;
                    }
                }
                else
                {
                    player.Notify("Fourrière", "Format incorrect", NotificationManager.Type.Warning);
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
