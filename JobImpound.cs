using JobImpound.Classes;
using JobImpound.Entities;
using JobImpound.Panels;
using Life;
using Life.BizSystem;
using Life.Network;
using ModKit.Helper;
using ModKit.Interfaces;
using Socket.Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using _menu = AAMenu.Menu;
using mk = ModKit.Helper.TextFormattingHelper;

namespace JobImpound
{
    public class JobImpound : ModKit.ModKit
    {
        #region CONFIG
        public static string ConfigDirectoryPath;
        public static string ConfigJobImpoundPath;
        public static JobImpoundConfig _jobImpoundConfig;
        #endregion

        public static Dictionary<uint, Coroutine> activeCoroutines = new Dictionary<uint, Coroutine>();
        public ImpoundPanelsManager ImpoundPanelsManager;

        public JobImpound(IGameAPI api) : base(api)
        {
            ImpoundPanelsManager = new ImpoundPanelsManager(this);
            PluginInformations = new PluginInformations(AssemblyHelper.GetName(), "1.0.0", "Aarnow");
        }

        public override void OnPluginInit()
        {
            base.OnPluginInit();
            InitConfig();
            _jobImpoundConfig = LoadConfigFile(ConfigJobImpoundPath);

            Orm.RegisterTable<JobImpound_Vehicle>();
            Orm.RegisterTable<JobImpound_Reason>();

            InsertMenu();
            ModKit.Internal.Logger.LogSuccess($"{PluginInformations.SourceName} v{PluginInformations.Version}", "initialisé");
        }

        #region Config
        private void InitConfig()
        {
            try
            {
                ConfigDirectoryPath = DirectoryPath + "/JobImpound";
                ConfigJobImpoundPath = Path.Combine(ConfigDirectoryPath, "jobImpoundConfig.json");

                if (!Directory.Exists(ConfigDirectoryPath)) Directory.CreateDirectory(ConfigDirectoryPath);
                if (!File.Exists(ConfigJobImpoundPath)) InitJobImpoundConfig();
            }
            catch (IOException ex)
            {
                ModKit.Internal.Logger.LogError("InitDirectory", ex.Message);
            }
        }

        private void InitJobImpoundConfig()
        {
            JobImpoundConfig jobImpoundConfig = new JobImpoundConfig();
            string json = JsonConvert.SerializeObject(jobImpoundConfig);
            File.WriteAllText(ConfigJobImpoundPath, json);
        }

        public static JobImpoundConfig LoadConfigFile(string path)
        {
            if (File.Exists(path))
            {
                string jsonContent = File.ReadAllText(path);
                JobImpoundConfig jobImpoundConfig = JsonConvert.DeserializeObject<JobImpoundConfig>(jsonContent);

                return jobImpoundConfig;
            }
            else return null;
        }
        #endregion

        public void InsertMenu()
        {
            _menu.AddAdminTabLine(PluginInformations, 5, "JobImpound", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ImpoundPanelsManager.AdminPanels.JobImpoundPanel(player);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere }, null, $"{mk.Color("proximité", mk.Colors.Info)}", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ImpoundPanelsManager.ImpoundProximityPanel(player);
            }, 2);

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, $"{mk.Color("[Fourrière] proximité", mk.Colors.Purple)}", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                //code
            }, 2);

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere, Activity.Type.Mecanic }, null, "Dépannage", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ImpoundPanelsManager.SkillPanels.TroubleshootingPanel(player);
            });
        }
    }
}
