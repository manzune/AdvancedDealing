namespace AdvancedDealing.Persistence.Datas
{
    public class DealerData : DataBase
    {
        public string DeadDrop;

        public int MaxCustomers;

        public int ItemSlots;

        public float Cut;

        public float SpeedMultiplier;

        public bool DeliverCash;

        public bool PickupProducts;

        public float CashThreshold;

        public int ProductThreshold;

        public int DaysUntilNextNegotiation;

        public DealerData(string identifier, bool loadDefaults = false) : base(identifier)
        {
            if (loadDefaults)
            {
                LoadDefaults();
            }
        }

        public void LoadDefaults()
        {
            DeadDrop = null;
            MaxCustomers = 8;
            ItemSlots = 5;
            Cut = 0.2f;
            SpeedMultiplier = 1f;
            DeliverCash = false;
            PickupProducts = false;
            CashThreshold = 1500f;
            ProductThreshold = 20;
            DaysUntilNextNegotiation = 0;
        }
    }
}
