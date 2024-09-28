using JobImpound.Panels.Admin;
using JobImpound.Panels.Impound;
using JobImpound.Panels.Skill;
using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;

namespace JobImpound.Panels
{
    public class ImpoundPanelsManager
    {
        public AdminPanels AdminPanels;
        public ImpoundPanels ImpoundPanels;
        public ImpoundSkillPanels ImpoundSkillPanels;

        [Ignore] public ModKit.ModKit Context { get; set; }

        public ImpoundPanelsManager(ModKit.ModKit context)
        {
            Context = context;
            AdminPanels = new AdminPanels(context);
            ImpoundPanels = new ImpoundPanels(context);
            ImpoundSkillPanels = new ImpoundSkillPanels(context);
        }

        public void ImpoundProximityPanel(Player player)
        {
            //Query

            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Objets interactifs", UIPanel.PanelType.TabPrice, player, () => ImpoundProximityPanel(player));

            //Corps

            panel.AddTabLine("Consulter l'ordinateur", "", ItemUtils.GetIconIdByItemId(1134), _ =>
            {
                if ((Context.ProximityHelper.IsObjectNearby(player, 1199) || Context.ProximityHelper.IsObjectNearby(player, 1134) || Context.ProximityHelper.IsObjectNearby(player, 73)) && player.setup.areaId == player.biz.TerrainId)
                {
                    LawEnforcementComputerPanel(player);
                }
                else player.Notify("Trop loin", "Vous devez être à proximité d'un ordinateur du commissariat", NotificationManager.Type.Warning);
            });

            //Boutons
            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", ui =>
            {
                AAMenu.AAMenu.menu.BizPanel(player, AAMenu.AAMenu.menu.BizTabLines);
            });
            panel.CloseButton();

            //Affichage
            panel.Display();
        }

        public void LawEnforcementComputerPanel(Player player)
        {
            //Query

            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Ordinateur", UIPanel.PanelType.TabPrice, player, () => LawEnforcementComputerPanel(player));

            //Corps
            panel.AddTabLine("Rechercher un véhicule", "", ItemUtils.GetIconIdByItemId(1134), _ =>
            {
                //CriminalRecordPanels.LawEnforcementCriminalRecordPanel(player);
            });
            panel.AddTabLine("Véhicules disponibles", "", IconUtils.Vehicles.C4GrandPicasso.Id, _ =>
            {
                ImpoundPanels.ImpoundVehiclePanel(player);
            });
            panel.AddTabLine("Suivi des véhicules", "", ItemUtils.GetIconIdByItemId(1803), _ =>
            {
                //CriminalRecordPanels.LawEnforcementCriminalRecordPanel(player);
            });
            panel.AddTabLine("Raisons d'une immobilisation", "", ItemUtils.GetIconIdByItemId(1033), _ =>
            {
                //CriminalRecordPanels.LawEnforcementCriminalRecordPanel(player);
            });

            //Boutons
            panel.NextButton("Consulter", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
