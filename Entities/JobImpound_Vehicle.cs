using SQLite;
using ModKit.Utils;
using System;

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
        public double BizId { get; set; }
        public string BizName { get; set; }
        public int OwnerId { get; set; }
        public string OwnerFullName { get; set; }
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
        public int CreatedBy { get; set; }
        public bool IsArchived { get; set; }
        public int ArchivedBy { get; set; }

        public JobImpound_Vehicle()
        {
        }
    }
}
