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
            _range = UnityEngine.Random.Range(minRange, maxRange);
        }

        public override void Start()
        {
            base.Start();

            StealCash();
        }

        private void StealCash()
        {
            _dealer.Dealer.SetCash(_dealer.Dealer.Cash - (_dealer.Dealer.Cash * _range / 100));

            End();
        }

        public override bool ShouldOverrideOriginalSchedule()
        {
            return false;
        }
    }
}
