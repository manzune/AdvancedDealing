using AdvancedDealing.Economy;

namespace AdvancedDealing.Persistence.Datas
{
    public class DealerData(string identifier) : DataBase(identifier)
    {
        public string DeadDrop;

        public int MaxCustomers;

        public int ItemSlots;

        public float Cut;

        public float SpeedMultiplier;

        // Stats
        public float Experience;

        public int Level;

        public float Loyality;

        // Behavior
        public bool DeliverCash;

        public bool NotifyOnCashDelivery;

        public float CashThreshold;

        public int DaysUntilNextNegotiation;

        public override void SetDefaults()
        {
            DeadDrop = null;
            MaxCustomers = 8;
            ItemSlots = 5;
            Cut = 0.2f;
            SpeedMultiplier = 1f;
            Experience = 1f;
            Level = 1;
            Loyality = 50f;
            DeliverCash = false;
            NotifyOnCashDelivery = true;
            CashThreshold = 1500f;
            DaysUntilNextNegotiation = 0;

            if (ModConfig.RealisticMode)
            {
                MaxCustomers = LevelWatcher.MaxCustomersBase;
                ItemSlots = LevelWatcher.ItemSlotsBase;
                Cut = 0.5f;
                SpeedMultiplier = LevelWatcher.SpeedMultiplierBase;

            }
        }
    }
}
