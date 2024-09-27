using SQLite;

namespace JobImpound.Entities
{
    public class JobImpound_Reason : ModKit.ORM.ModEntity<JobImpound_Reason>
    {
        [AutoIncrement][PrimaryKey] public int Id { get; set; }
        public string Title { get; set; }
        public int CreatedAt { get; set; }
        public int CreatedBy { get; set; }
        public JobImpound_Reason()
        {
        }
    }
}
