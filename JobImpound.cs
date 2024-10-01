using JobImpound.Classes;
using JobImpound.Entities;
using JobImpound.Panels;
using JobImpound.Panels.LawEnforcement;
using Life;
using Life.BizSystem;
using Life.Network;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Interfaces;
using ModKit.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static JobImpound.Entities.JobImpound_Vehicle;
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

        public const int TOWTRUCK_ID = 12;

        public static Dictionary<uint, Coroutine> activeCoroutines = new Dictionary<uint, Coroutine>();
        public PanelsManager PanelsManager;

        public JobImpound(IGameAPI api) : base(api)
        {
            PanelsManager = new PanelsManager(this);
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
                PanelsManager.AdminPanels.JobImpoundPanel(player);
            });

            #region LAW ENFORCEMENT SKILLS
            _menu.AddProximityBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, 1199, $"{mk.Color("Parc de la fourrière", mk.Colors.Purple)}", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                PanelsManager.SnippetVehiclePanels.SnippetVehiclePanel(player);
            }, 101, null);
            #endregion

            #region IMPOUND SKILLS
            _menu.AddProximityBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere }, null, 1199, "Consulter l'ordinateur", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);

                if (player.HasBiz() && player.setup.areaId == player.biz.TerrainId)
                {
                    PanelsManager.ImpoundComputerPanel(player);
                }
                else player.Notify("Ordinateur", "Vous n'avez pas accès à cette ordinateur", NotificationManager.Type.Info);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere, Activity.Type.Mecanic }, null, "Dépannage", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                PanelsManager.ImpoundSkillPanels.TroubleshootingPanel(player);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere }, null, "Immobiliser un véhicule", async (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);

                if (player.setup.areaId == player.biz.TerrainId)
                {
                    Vehicle vehicle = VehicleUtils.GetClosestVehicle(player, 4, new List<int> { TOWTRUCK_ID });
                    if (vehicle != null)
                    {
                        var vehicleInfo = Nova.v.GetVehicle(vehicle.vehicleDbId);

                        string immobiliseStatus = VehicleStatus.Immobilise.ToString();
                        string nonReclameStatus = VehicleStatus.NonReclame.ToString();
                        List<JobImpound_Vehicle> vehicles = await JobImpound_Vehicle.Query(v => v.Status == immobiliseStatus || v.Status == nonReclameStatus);

                        if (vehicles.Any(v => v.VehicleId == vehicle.vehicleDbId))
                        {
                            player.Notify("Fourrière", "Ce véhicule est déjà immobilisé", NotificationManager.Type.Info);
                        }
                        else
                        {
                            JobImpound_Vehicle newVehicle = new JobImpound_Vehicle();
                            newVehicle.VehicleId = vehicle.vehicleDbId;
                            newVehicle.ModelId = vehicleInfo.modelId;
                            newVehicle.Plate = vehicle.plate;
                            newVehicle.LStatus = VehicleStatus.Immobilise;

                            PanelsManager.ImpoundSkillPanels.ImmobiliseVehicleReasonPanel(player, newVehicle);
                        }
                    }
                    else player.Notify("Fourrière", $"Aucun véhicule à proximité", NotificationManager.Type.Info);
                }
                else player.Notify("Fourrière", $"Vous ne pouvez immobilser un véhicule qu'en étant sur le terrain de votre société", NotificationManager.Type.Info);
            });
            #endregion
        }
    }
}
