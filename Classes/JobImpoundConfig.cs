namespace JobImpound.Classes
{
    public class JobImpoundConfig
    {
        public int CityHallId = 0; //ID de la Mairie
        public int MaximumDowntimeInMinute = 2; //Nombre de minute avant que le véhicule soit considéré comme étant non-réclamé
        public double StorageCostsPerHour = 25.0f; //Coût de gardiennage par heure
        public double TowingCosts = 200.0f; //Coût de remorquage
        public double ImpoundAdministrativeCosts = 80.0f; //Frais administratif de la fourrière
        public float MaxDistance = 30.0f; // Distance maximale pour dépanner autour d'une dépanneuse
        public int UnlockingDuration = 60; // Durée d'accès à un véhicule dévérouillé par la compétence du dépanneur
        public double CommissionPercentage = 2.5f; //Commission de l'agent de la fourrière lors d'une vente
    }
}
