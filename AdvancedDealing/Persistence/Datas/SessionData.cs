namespace AdvancedDealing.Persistence.Datas
{
    public class SessionData : DataBase
    {
        public bool AccessInventory;

        public bool SettingsMenu;

        public float NegotiationModifier;

        public SessionData(string identifier) : base(identifier) { }
    }
}
