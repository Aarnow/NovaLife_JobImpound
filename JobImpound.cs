using FirstGearGames.Utilities.Networks;
using JobImpound.Classes;
using JobImpound.Entities;
using JobImpound.Panels;
using Life;
using Life.AreaSystem;
using Life.BizSystem;
using Life.DB;
using Life.Network;
using Life.VehicleSystem;
using ModKit.Helper;
using ModKit.Interfaces;
using Newtonsoft.Json;
using System;
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
            Orm.RegisterTable<JobImpound_Certificate>();

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

        #region UTILS
        public static Vehicle GetClosestVehicle(Player player, List<int> exceptions = null)
        {
            Vehicle[] objectsOfType = UnityEngine.Object.FindObjectsOfType<Vehicle>();
            if(exceptions == null) exceptions = new List<int>();

            foreach (Vehicle vehicle in objectsOfType)
            {
                float distance = Vector3.Distance(player.setup.transform.position, vehicle.transform.position);
                if (distance < _jobImpoundConfig.MaxDistance && !exceptions.Any(e => e == Nova.v.GetVehicle(vehicle.vehicleDbId).modelId))
                {
                    return vehicle;
                }
            }

            return null;
        }
        #endregion

        public void InsertMenu()
        {
            _menu.AddAdminTabLine(PluginInformations, 5, "JobImpound", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ImpoundPanelsManager.AdminPanels.JobImpoundPanel(player);
            });

            #region CITIZEN SKILLS
            _menu.AddDocumentTabLine(PluginInformations, "Cartes grises", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                //code
            });
            #endregion

            #region LAW ENFORCEMENT SKILLS
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, $"{mk.Color($"Proximité {mk.Italic("[fourrière]")}", mk.Colors.Purple)}", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                //code
            }, 2);

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, "Contrôler la carte grise", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                //code
            });
            #endregion

            #region MECANIC SKILLS
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Mecanic }, null, $"Délivrer une carte grise", async (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                Player target = player.GetClosestPlayer();
                Vehicle vehicle = GetClosestVehicle(player);

                if(player.setup.areaId == player.biz.TerrainId)
                {
                    if (target != null)
                    {
                        if (vehicle != null)
                        {
                            var vehicleInfo = Nova.v.GetVehicle(vehicle.vehicleDbId);
                            if(vehicleInfo != null && vehicleInfo.permissions.owner.characterId == target.character.Id)
                            {
                                List<JobImpound_Certificate> certificates = await JobImpound_Certificate.Query(c => c.Plate == vehicle.plate);
                                if (certificates != null && certificates.Count > 0)
                                {
                                    //code
                                }
                                else player.Notify("Carte Grise", $"Ce véhicule possède déjà une carte grise", NotificationManager.Type.Info);
                            }
                            else player.Notify("Carte Grise", $"Ce véhicule n'appartient pas à {target.GetFullName()}", NotificationManager.Type.Info);
                        }
                        else player.Notify("Carte Grise", $"Aucun véhicule n'est à proximité", NotificationManager.Type.Info);
                    }
                    else player.Notify("Carte Grise", $"Aucun citoyen n'est à proximité", NotificationManager.Type.Info);
                }
                else player.Notify("Carte Grise", $"Vous ne pouvez délivrer une carte grise qu'en étant sur le terrain de votre société", NotificationManager.Type.Info);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere }, null, $"{mk.Color("Proximité", mk.Colors.Info)}", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ImpoundPanelsManager.ImpoundProximityPanel(player);
            }, 1);

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere, Activity.Type.Mecanic }, null, "Dépannage", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ImpoundPanelsManager.SkillPanels.TroubleshootingPanel(player);
            });

            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.Fourriere }, null, "Immobiliser un véhicule", async (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);

                if (player.setup.areaId == player.biz.TerrainId)
                {
                    Vehicle vehicle = GetClosestVehicle(player, new List<int> { JobImpound.TOWTRUCK_ID });
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
                            newVehicle.BizId = vehicle.bizId;

                            var biz = Nova.biz.FetchBiz(vehicle.bizId);
                            if (Nova.biz.FetchBiz(vehicle.bizId) != null) newVehicle.BizName = biz.BizName;

                            if (vehicleInfo != null)
                            {
                                var owner = await LifeDB.db.Table<Characters>().Where(c => c.Id == vehicleInfo.permissions.owner.characterId).FirstOrDefaultAsync();

                                if (owner != null)
                                {
                                    Console.WriteLine("owner: " + owner != null);

                                    newVehicle.OwnerId = owner.Id;
                                    newVehicle.OwnerFullName = owner.Firstname + " " + owner.Lastname;
                                }
                            }

                            newVehicle.LStatus = VehicleStatus.Immobilise;

                            ImpoundPanelsManager.SkillPanels.ImmobiliseVehicleReasonPanel(player, newVehicle);
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
