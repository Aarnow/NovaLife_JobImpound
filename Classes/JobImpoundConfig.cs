namespace JobImpound.Classes
{
    public class JobImpoundConfig
    {
        public int CityHallId = 0;
        public int MaximumDowntimeInMinute = 0;
        public double StorageCostsPerHour = 0.0f;
        public double TowingCosts = 0.0f;
        public double ImpoundAdministrativeCosts = 0.0f;
        public float MaxDistance = 20.0f;
        public int UnlockingDuration = 60;
        public double CommissionPercentage = 2.5f;
    }
}
