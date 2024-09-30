using SQLite;
using ModKit.Utils;
using System;
using mk = ModKit.Helper.TextFormattingHelper;
using Life.VehicleSystem;
using System.Threading.Tasks;

namespace JobImpound.Entities
{
    public class JobImpound_Vehicle : ModKit.ORM.ModEntity<JobImpound_Vehicle>
    {
        public enum VehicleStatus
        {
            [EnumDisplayName("Immobilisé")]//le véhicule est en attente de récupération
            Immobilise,
            [EnumDisplayName("Libéré")] //le propriétaire à payé et récupéré son vehicule
            Libere,
            [EnumDisplayName("Non réclamé")]// le propriétaire n'a pas réclamé son véhicule dans un délai de X jours
            NonReclame,
            [EnumDisplayName("Recyclé")]//le véhicule, précédemment non réclamé, à été recyclé
            Recycle,
            [EnumDisplayName("Vendu")]//le véhicule, précédemment non réclamé, à été vendu
            Vendu,
            [EnumDisplayName("Saisi")] //la police à récupéré le véhicule pour une enquete
            Saisi
        }

        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int VehicleId { get; set; }
        public int ModelId { get; set; }
        public string Plate { get; set; }
        public int ReasonId { get; set; }
        public string Evidence { get; set; }

        public string Status { get; set; }
        [Ignore]
        public VehicleStatus LStatus
        {
            get { return Enum.TryParse(Status, out VehicleStatus status) ? status : VehicleStatus.Immobilise; }
            set { Status = value.ToString(); }
        }

        public long CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public long ReleasedAt { get; set; }
        public string ReleasedBy { get; set; }

        public JobImpound_Vehicle()
        {
        }


        public async Task UpdateStatus()
        {
            if (LStatus == VehicleStatus.Immobilise && DateUtils.IsGreater(CreatedAt, 60 * JobImpound._jobImpoundConfig.MaximumDowntimeInMinute))
            {
                LStatus = VehicleStatus.NonReclame;
                await Save();
            }
        }

        public mk.Colors ReturnColorOfStatus()
        {
            mk.Colors color = mk.Colors.Verbose;
            switch (LStatus)
            {
                case VehicleStatus.Immobilise:
                    color = mk.Colors.Warning;
                    break;
                case VehicleStatus.Libere:
                    color = mk.Colors.Success;
                    break;
                case VehicleStatus.NonReclame:
                    color = mk.Colors.Error;
                    break;
                case VehicleStatus.Recycle:
                    color = mk.Colors.Info;
                    break;
                case VehicleStatus.Vendu:
                    color = mk.Colors.Purple;
                    break;
                case VehicleStatus.Saisi:
                    color = mk.Colors.Orange;
                    break;
                default:
                    color = mk.Colors.Verbose;
                    break;
            }
            return color;
        }

        public double GetAmountOfStorage()
        {
            long time = DateUtils.GetCurrentTime() - CreatedAt;
            int hours = (int)Math.Ceiling((double)time / 3600);

            return JobImpound._jobImpoundConfig.StorageCostsPerHour * hours;
        }
    }
}
