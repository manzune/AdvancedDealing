namespace AdvancedDealing.Persistence.Datas
{
    public class DeadDropData : DataBase
    {
        public string CashCollectionQuest;

        public string RefillProductsQuest;

        public DeadDropData(string identifier, bool loadDefaults = false) : base(identifier)
        {
            if (loadDefaults)
            {
                LoadDefaults();
            }
        }

        public void LoadDefaults()
        {

        }
    }
}
