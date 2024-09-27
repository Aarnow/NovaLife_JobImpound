using Life.Network;
using Life.UI;
using ModKit.Helper;
using SQLite;
using mk = ModKit.Helper.TextFormattingHelper;

namespace JobImpound.Panels.Admin
{
    public class AdminPanels
    {
        [Ignore] public ModKit.ModKit Context { get; set; }

        public AdminPanels(ModKit.ModKit context)
        {
            Context = context;
        }

        public void JobImpoundPanel(Player player)
        {
            //Déclaration
            Panel panel = Context.PanelHelper.Create("JobImpound", UIPanel.PanelType.TabPrice, player, () => JobImpoundPanel(player));

            //Corps
            panel.AddTabLine($"{mk.Color("Appliquer la configuration", mk.Colors.Info)}", _ =>
            {
                JobImpound._jobImpoundConfig = JobImpound.LoadConfigFile(JobImpound.ConfigJobImpoundPath);
                panel.Refresh();
            });

            panel.NextButton("Sélectionner", () => panel.SelectTab());
            panel.AddButton("Retour", _ => AAMenu.AAMenu.menu.AdminPanel(player, AAMenu.AAMenu.menu.AdminTabLines));
            panel.CloseButton();

            //Affichage
            panel.Display();
        }
    }
}
