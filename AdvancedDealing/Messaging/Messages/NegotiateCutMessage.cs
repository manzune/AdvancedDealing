using AdvancedDealing.Economy;
using AdvancedDealing.UI;
using System;
using AdvancedDealing.Persistence;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class NegotiateCutMessage(DealerExtension dealerExtension) : MessageBase
    {
        private readonly DealerExtension _dealer = dealerExtension;

        public override string Text => "Let's talk about your cut";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            if (_dealer.Dealer.IsRecruited && _dealer.DaysUntilNextNegotiation <= 0)
            {
                return true;
            }
            return false;
        }

        public override void OnSelected()
        {
            float current = (float)Math.Round(_dealer.Cut, 2);

            UIBuilder.SliderPopup.Open($"Negotiate Cut % ({_dealer.Dealer.name})", $"Current: {current:P0}", current, 0f, 1f, 0.01f, 2, OnSend, null, "{0:P0}");
        }

        private void OnSend(float value)
        {
            _dealer.SendPlayerMessage($"Joo! We need to talk about your cut.. How about {value:P0}?");

            if (value == _dealer.Cut)
            {
                _dealer.SendMessage("Bro that's the same amount i get atm!", false, true, 2f);

                return;
            }
            else if (value > _dealer.Cut)
            {
                _dealer.SendMessage("Haha.. you idiot! Yeah sure", false, true, 2f);
                _dealer.Loyality -= -5f;
            }
            else
            {
                bool accepted = CalculateResponse(_dealer.Cut, value);

                if (accepted)
                {
                    _dealer.SendMessage("Okay i'm fine with that. We got a deal!", false, true, 2f);
                }
                else
                {
                    _dealer.SendMessage("Naah.. no chance!", false, true, 2f);

                    _dealer.DaysUntilNextNegotiation = 3;
                    _dealer.Loyality -= -10f;

                    if (NetworkSynchronizer.IsSyncing)
                    {
                        NetworkSynchronizer.Instance.SendData(_dealer.FetchData());
                    }

                    return;
                }
            }

            _dealer.Cut = value;
            _dealer.DaysUntilNextNegotiation = 7;
            _dealer.HasChanged = true;

            if (NetworkSynchronizer.IsSyncing)
            {
                NetworkSynchronizer.Instance.SendData(_dealer.FetchData());
            }
        }

        private bool CalculateResponse(float oldCut, float newCut)
        {
            float baseChance = _dealer.Loyality;
            float difference = Math.Abs(oldCut - newCut);
            float chance = (baseChance - (baseChance * (difference * 100) / 100)) / 100;

            return UnityEngine.Random.value <= chance;
        }
    }
}
