using JobImpound.Panels.Admin;
using JobImpound.Panels.Impound;
using JobImpound.Panels.LawEnforcement;
using JobImpound.Panels.Skill;
using Life;
using Life.Network;
using Life.UI;
using ModKit.Helper;
using ModKit.Utils;
using SQLite;

namespace JobImpound.Panels
{
    public class PanelsManager
    {
        public AdminPanels AdminPanels;
        public VehiclePanels VehiclePanels;
        public ImpoundSkillPanels ImpoundSkillPanels;
        public ReasonPanels ReasonPanels;
        public SnippetVehiclePanels SnippetVehiclePanels;

        [Ignore] public ModKit.ModKit Context { get; set; }

        public PanelsManager(ModKit.ModKit context)
        {
            Context = context;
            AdminPanels = new AdminPanels(context);
            VehiclePanels = new VehiclePanels(context);
            ImpoundSkillPanels = new ImpoundSkillPanels(context);
            ReasonPanels = new ReasonPanels(context);
            SnippetVehiclePanels = new SnippetVehiclePanels(context);
        }

        public void ImpoundComputerPanel(Player player)
        {
            //Query

            //Déclaration
            Panel panel = Context.PanelHelper.Create("Fourrière - Ordinateur", UIPanel.PanelType.TabPrice, player, () => ImpoundComputerPanel(player));

            //Corps
            panel.AddTabLine("Rechercher un véhicule", "", ItemUtils.GetIconIdByItemId(1134), _ => VehiclePanels.VehicleSearchPanel(player));

            panel.AddTabLine("Véhicules disponibles", "", IconUtils.Vehicles.C4GrandPicasso.Id, _ => VehiclePanels.VehiclePanel(player));

            panel.AddTabLine("Suivi des véhicules", "", ItemUtils.GetIconIdByItemId(1803), _ => VehiclePanels.VehiclePanel(player, false));

            panel.AddTabLine("Raisons d'une immobilisation", "", ItemUtils.GetIconIdByItemId(1033), _ => ReasonPanels.ReasonPanel(player));

            //Boutons
            panel.NextButton("Consulter", () => panel.SelectTab());
            panel.PreviousButton();
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
