using JobImpound.Entities;
using Life.Network;
using Life.UI;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;
using System.Threading.Tasks;
using mk = ModKit.Helper.TextFormattingHelper;

namespace JobImpound.Panels.Skill
{
    internal class MecanicSkillPanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public MecanicSkillPanels(ModKit.ModKit context)
        {
            Context = context;
        }

        public void CertificateRequestPanel(Player player, Player target, LifeVehicle vehicle)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("Carte grise - Création", UIPanel.PanelType.Text, player, () => CertificateRequestPanel(player, target, vehicle));

            //Corps
            panel.TextLines.Add($"{mk.Color($"{player.GetFullName()}", mk.Colors.Orange)} souhaite réaliser la carte grise");
            panel.TextLines.Add($"du véhicule immatriculé {mk.Color($"{vehicle.plate}", mk.Colors.Orange)}");
            panel.TextLines.Add($"pour la somme de {mk.Color($"{JobImpound._jobImpoundConfig.MecanicAdministrativeCosts}", mk.Colors.Orange)}€");
            panel.TextLines.Add($"{mk.Size($"{mk.Italic($"{mk.Color("(frais administratifs non-négociable)",mk.Colors.Grey)}")}", 14)}");

            //Boutons
            panel.CloseButtonWithAction($"Accepter<br>{mk.Size("(soi-même)", 14)}", async () =>
            {
                if(target.character.Money < JobImpound._jobImpoundConfig.MecanicAdministrativeCosts)
                {
                    JobImpound_Certificate newCertificate = JobImpound_Certificate.CreateCertificate(player, target, vehicle);

                    if (await newCertificate.Save())
                    {
                        target.AddMoney(-JobImpound._jobImpoundConfig.MecanicAdministrativeCosts, "JobImpound - Création d'une carte grise");
                        player.biz.Bank += JobImpound._jobImpoundConfig.MecanicAdministrativeCosts;
                        player.biz.Save();

                        player.Notify("Carte Grise", $"Le citoyen obtient sa carte grise.<br>Votre société reçoit {mk.Color($"{JobImpound._jobImpoundConfig.MecanicAdministrativeCosts}", mk.Colors.Orange)}€", Life.NotificationManager.Type.Success);
                        target.Notify("Carte Grise", "Nous n'avons pas pu créer la carte-grise", Life.NotificationManager.Type.Success);
                        return true;
                    }
                    else
                    {
                        player.Notify("Carte Grise", "Nous n'avons pas pu créer la carte-grise", Life.NotificationManager.Type.Error);
                        target.Notify("Carte Grise", "Nous n'avons pas pu créer la carte-grise", Life.NotificationManager.Type.Error);
                        return false;
                    }
                }
                else
                {
                    player.Notify("Carte Grise", "Le citoyen n'est pas en mesure de régler les frais administratifs", Life.NotificationManager.Type.Info);
                    target.Notify("Carte Grise", "Vous ne pouvez pas régler les frais administratifs", Life.NotificationManager.Type.Info);
                    return false;
                }
            });
            panel.CloseButtonWithAction($"Accepter<br>{mk.Size("(société)", 14)}", async () =>
            {
                if (target.biz.Bank < JobImpound._jobImpoundConfig.MecanicAdministrativeCosts)
                {
                    JobImpound_Certificate newCertificate = JobImpound_Certificate.CreateCertificate(player, target, vehicle, true);

                    if (await newCertificate.Save())
                    {
                        target.biz.Bank -= JobImpound._jobImpoundConfig.MecanicAdministrativeCosts;
                        target.biz.Save();
                        player.biz.Bank += JobImpound._jobImpoundConfig.MecanicAdministrativeCosts;
                        player.biz.Save();

                        player.Notify("Carte Grise", $"Le citoyen obtient sa carte grise.<br>Votre société reçoit {mk.Color($"{JobImpound._jobImpoundConfig.MecanicAdministrativeCosts}", mk.Colors.Orange)}€", Life.NotificationManager.Type.Success);
                        target.Notify("Carte Grise", "Nous n'avons pas pu créer la carte-grise", Life.NotificationManager.Type.Success);
                        return true;
                    }
                    else
                    {
                        player.Notify("Carte Grise", "Nous n'avons pas pu créer la carte-grise", Life.NotificationManager.Type.Error);
                        target.Notify("Carte Grise", "Nous n'avons pas pu créer la carte-grise", Life.NotificationManager.Type.Error);
                        return false;
                    }
                }
                else
                {
                    player.Notify("Carte Grise", "La société du citoyen n'est pas en mesure de régler les frais administratifs", Life.NotificationManager.Type.Info);
                    target.Notify("Carte Grise", "Votre société ne peut pas régler les frais administratifs", Life.NotificationManager.Type.Info);
                    return false;
                }
            });
            panel.CloseButtonWithAction("Refuser", () =>
            {
                player.Notify("Carte Grise", "Le citoyen refuse de créer une carte grise", Life.NotificationManager.Type.Info);
                target.Notify("Carte Grise", "Vous refusez de créer une carte grise", Life.NotificationManager.Type.Info);
                return Task.FromResult(true);
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
