using AdvancedDealing.Economy;
using AdvancedDealing.UI;
using System;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
#elif MONO
using ScheduleOne.Messaging;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public class NegotiateCutMessage(DealerManager dealerManager) : MessageMessage
    {
        private readonly DealerManager _dealerManager = dealerManager;

        public override string Text => "Let's talk about your cut";

        public override bool DisableDefaultSendBehaviour => true;

        public override bool ShouldShowCheck(SendableMessage sMsg)
        {
            if (_dealerManager.Dealer.IsRecruited && ModConfig.LoyalityMode && _dealerManager.DaysUntilNextNegotiation <= 0)
            {
                return true;
            }
            return false;
        }

        public override void OnSelected()
        {
            float current = (float)Math.Round(_dealerManager.Cut, 2);

            UIInjector.SliderPopup.Open($"Negotiate Cut % ({_dealerManager.Dealer.name})", $"Current: {current:n2}", current, 0f, 1f, 0.01f, 2, OnSend, null, "P0", null);
        }

        private void OnSend(float value)
        {
            _dealerManager.SendPlayerMessage($"Joo! We need to talk about your cut.. How about {value:P0}?");

            if (value == _dealerManager.Cut)
            {
                _dealerManager.SendMessage("Bro that's the same amount i get atm!", false, true, 2f);

                return;
            }
            else if (value > _dealerManager.Cut)
            {
                _dealerManager.SendMessage("Haha.. you idiot! Yeah sure", false, true, 2f);
            }
            else
            {
                bool accepted = CalculateResponse(_dealerManager.Cut, value);

                if (accepted)
                {
                    _dealerManager.SendMessage("Okay i'm fine with that. We got a deal!", false, true, 2f);
                }
                else
                {
                    _dealerManager.SendMessage("Naah.. no chance!", false, true, 2f);

                    _dealerManager.DaysUntilNextNegotiation = 3;

                    return;
                }
            }

            _dealerManager.Cut = value;
            _dealerManager.DaysUntilNextNegotiation = 7;

            _dealerManager.HasChanged = true;
        }

        private static bool CalculateResponse(float oldCut, float newCut)
        {
            float baseChance = 50f;
            float difference = Math.Abs(oldCut - newCut);
            float chance = (baseChance - (baseChance * (difference * 100) / 100)) / 100;

            return UnityEngine.Random.value <= chance;
        }
    }
}
