using JobImpound.Entities;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using System.Collections.Generic;
using mk = ModKit.Helper.TextFormattingHelper;

namespace JobImpound.Panels.Skill
{
    public class CitizenSkillPanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public CitizenSkillPanels(ModKit.ModKit context)
        {
            Context = context;
        }

        public async void CertificatesPanel(Player player)
        {
            //Query
            List<JobImpound_Certificate> certificates = await JobImpound_Certificate.Query(c => c.CharacterId == player.character.Id);

            //Déclaration
            Panel panel = Context.PanelHelper.Create("Documents - Cartes grises", UIPanel.PanelType.TabPrice, player, () => CertificatesPanel(player));

            //Corps
            if (certificates != null && certificates.Count > 0)
            {
                foreach (JobImpound_Certificate certificate in certificates)
                {
                    panel.AddTabLine($"{(certificate.ModelId != default ? $"{VehicleUtils.GetModelNameByModelId(certificate.ModelId)}" : "inconnu")}<br>{mk.Size($"{mk.Color($"{DateUtils.FormatUnixTimestamp(certificate.DelivredAt)}", mk.Colors.Orange)}", 14)}", $"{mk.Color($"{(certificate.Plate != null ? $"{certificate.Plate}" : "inconnu")}", mk.Colors.Verbose)}<br>{mk.Color($"{(certificate.DelivredBy != null ? $"{certificate.DelivredBy}" : "inconnu")}", mk.Colors.Info)}", VehicleUtils.GetIconId(certificate.ModelId), _ =>
                    {
                        CertificatesDetailsPanel(player, certificate);
                    });
                }
            }
            else panel.AddTabLine("Aucune carte grise", _ => { });

            //Boutons
            panel.NextButton("Regarder", () => panel.SelectTab());
            panel.NextButton("Donner", () =>
            {
                Player target = player.GetClosestPlayer();
                if (target != null)
                {
                    CertificatesConfirmRequestPanel(player, target, certificates[panel.selectedTab]);
                }
                else
                {
                    player.Notify("Carte Grise", "Aucun citoyen n'est à proximité", Life.NotificationManager.Type.Info);
                    panel.Refresh();
                }
            });
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.DocumentPanel(player, AAMenu.AAMenu.menu.DocumentTabLines));
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void CertificatesDetailsPanel(Player player, JobImpound_Certificate certificate)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Documents - Regarder la carte grise", UIPanel.PanelType.TabPrice, player, () => CertificatesDetailsPanel(player, certificate));

            //Corps
            panel.AddTabLine($"{mk.Color("Modèle:", mk.Colors.Info)} {VehicleUtils.GetModelNameByModelId(certificate.ModelId)}", "", VehicleUtils.GetIconId(certificate.ModelId),_=>{});
            panel.AddTabLine($"{mk.Color("Plaque:", mk.Colors.Info)} {(certificate.Plate != null ? $"{certificate.Plate}" : $"{mk.Color("inconnu", mk.Colors.Grey)}")}",_=>{});
            panel.AddTabLine($"{mk.Color("Propriétaire:", mk.Colors.Info)} {(certificate.BizId != default ? $"{certificate.BizName}" : $"{(certificate.OwnerId != default ? $"{certificate.OwnerFullName}" : $"{mk.Color("inconnu", mk.Colors.Grey)}")}")}",_=>{});
            panel.AddTabLine($"{mk.Color("Date de fabrication:", mk.Colors.Orange)} {(certificate.CreatedAt != default ? DateUtils.FormatUnixTimestamp(certificate.CreatedAt) : "-")}",_ =>{});
            panel.AddTabLine($"{mk.Color("Délivré le:", mk.Colors.Orange)} {(certificate.DelivredAt != default ? DateUtils.FormatUnixTimestamp(certificate.DelivredAt) : "-")}", _ => { });
            panel.AddTabLine($"{mk.Color("Délivré par:", mk.Colors.Orange)} {(certificate.DelivredBy != null ? $"{certificate.DelivredBy}" : "inconnu")}",_=>{});

            //Boutons
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void CertificatesConfirmRequestPanel(Player player, Player target, JobImpound_Certificate certificate)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Documents - Donner la carte grise", UIPanel.PanelType.Text, player, () => CertificatesConfirmRequestPanel(player, target, certificate));

            //Corps
            panel.TextLines.Add($"Voulez-vous vraiment donner votre carte grise à {mk.Color($"{target.GetFullName()}", mk.Colors.Orange)} ?");

            //Boutons
            panel.PreviousButtonWithAction("Confirmer", async () =>
            {
                certificate.CharacterId = target.character.Id;
                if(await certificate.Save())
                {
                    player.Notify("Carte Grise", "Vous avez donner votre carte grise", Life.NotificationManager.Type.Success);
                    target.Notify("Carte Grise", $"Vous recevez la carte grise du véhicule immatriculé {certificate.Plate}", Life.NotificationManager.Type.Success);
                    return true;
                }
                else
                {
                    player.Notify("Carte Grise", "Nous n'avons pas donner votre carte-grise", Life.NotificationManager.Type.Error);
                    return false;
                }
            });
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
