using Life.Network;
using Life.VehicleSystem;
using ModKit.Utils;
using SQLite;

namespace JobImpound.Entities
{
    public class JobImpound_Certificate : ModKit.ORM.ModEntity<JobImpound_Certificate>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public int CharacterId { get; set; }

        public int VehicleId { get; set; }
        public int ModelId { get; set; }
        public string Plate { get; set; }

        public int BizId { get; set; }
        public string BizName { get; set; }
        public int OwnerId { get; set; }
        public string OwnerFullName { get; set; }

        public long CreatedAt { get; set; }
        public long DelivredAt { get; set; }
        public string DelivredBy { get; set; }
        
        public JobImpound_Certificate()
        {
        }

        public static JobImpound_Certificate CreateCertificate(Player player, Player target, LifeVehicle vehicle, bool isBizOwner = false)
        {
            JobImpound_Certificate newCertificate = new JobImpound_Certificate();

            newCertificate.CharacterId = target.character.Id;
            newCertificate.VehicleId = vehicle.vehicleId;
            newCertificate.ModelId = vehicle.modelId;
            newCertificate.Plate = vehicle.plate;

            if(isBizOwner)
            {
                newCertificate.BizId = target.biz.Id;
                newCertificate.BizName = target.biz.BizName;
            }
            else
            {
                newCertificate.OwnerId = target.character.Id;
                newCertificate.OwnerFullName = target.GetFullName();
            }


            newCertificate.CreatedAt = DateUtils.GetCurrentTime();
            newCertificate.DelivredAt = DateUtils.GetCurrentTime();
            newCertificate.DelivredBy = player.biz.BizName;

            return newCertificate;
        }
    }
}
