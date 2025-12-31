using AdvancedDealing.Economy;

namespace AdvancedDealing.NPCs.Actions
{
    public class StealCashAction : ActionBase
    {
        private readonly DealerExtension _dealer;

        private readonly float _range;

        protected override string ActionName => "Steal Products";

        protected override bool RemoveOnEnd => true;

        public StealCashAction(DealerExtension dealerExtension, float minRange, float maxRange)
        {
            _dealer = dealerExtension;
            _range = UnityEngine.Random.Range(minRange, maxRange + 1f);
        }

        public override void Start()
        {
            base.Start();

            StealCash();
        }

        private void StealCash()
        {
            float cashToSteal = _dealer.Dealer.Cash * _range / 100;

            Utils.Logger.Debug($"{_dealer.Dealer.fullName} has stolen some money: ${cashToSteal}");

            _dealer.Dealer.ChangeCash(0f - cashToSteal);

            End();
        }
    }
}
