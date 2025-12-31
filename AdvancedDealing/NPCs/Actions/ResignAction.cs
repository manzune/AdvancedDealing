using AdvancedDealing.Economy;

namespace AdvancedDealing.NPCs.Actions
{
    public class ResignAction : ActionBase
    {
        private readonly DealerExtension _dealer;

        protected override string ActionName => "Steal Products";

        protected override bool RemoveOnEnd => true;

        public ResignAction(DealerExtension dealerExtension)
        {
            _dealer = dealerExtension;
        }

        public override void Start()
        {
            base.Start();

            Resign();
        }

        private void Resign()
        {
            int random = UnityEngine.Random.Range(0, 100);

            if (random < 40)
            {
                _dealer.SendMessage("I cannot work for you anymore. I'm out!");
                _dealer.Fire();

                Utils.Logger.Debug($"{_dealer.Dealer.fullName} has resigned");
            }
            else
            {
                _dealer.SendMessage("Hey, i'm really mad ... Change something or i will resign.");
            }

            End();
        }
    }
}
