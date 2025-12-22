using System;
using System.Collections.Generic;
using AdvancedDealing.Economy;
using AdvancedDealing.Persistence.Datas;

#if IL2CPP
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
#elif MONO
using ScheduleOne.Messaging;
using ScheduleOne.NPCs;
#endif

namespace AdvancedDealing.Messaging
{
    public class ConversationManager
    {
        private static readonly List<ConversationManager> _cache = [];

        public readonly NPC npc;

        private MSGConversation Conversation =>
            npc.MSGConversation;

        public bool SendableMessagesCreated { get; private set; }

        public ConversationManager(NPC npc)
        {
            this.npc = npc;

            Utils.Logger.Debug("ConversationManager", $"Conversation for {npc.name} created");

            _cache.Add(this);
        }

        public void CreateSendableMessages()
        {
            if (SendableMessagesCreated) return;

            SendableMessage deliverCashMsg = Conversation.CreateSendableMessage("Deliver cash");
#if IL2CPP
            deliverCashMsg.ShouldShowCheck = (SendableMessage.BoolCheck)ShouldShowDeliverCash;
#elif MONO
            deliverCashMsg.ShouldShowCheck = ShouldShowDeliverCash;
#endif
            deliverCashMsg.disableDefaultSendBehaviour = true;
            deliverCashMsg.onSelected = new Action(OnDeliverCashSelected);

            SendableMessage stopCashDeliveryMsg = Conversation.CreateSendableMessage("Stop cash delivery");
#if IL2CPP
            stopCashDeliveryMsg.ShouldShowCheck = (SendableMessage.BoolCheck)ShouldShowDeliverCash;
#elif MONO
            stopCashDeliveryMsg.ShouldShowCheck = ShouldShowDeliverCash;
#endif
            stopCashDeliveryMsg.disableDefaultSendBehaviour = true;
            stopCashDeliveryMsg.onSelected = new Action(OnStopCashDeliverySelected);

            npc.ConversationCanBeHidden = false;

            Conversation.EnsureUIExists();
            Conversation.SetEntryVisibility(true);

            SendableMessagesCreated = true;
        }

        private bool ShouldShowDeliverCash(SendableMessage msg)
        {
            DealerManager dealerManager = DealerManager.GetManager(npc.GUID.ToString());
            bool check = dealerManager.DealerData.DeliverCash;
            if (msg.Text == "Deliver cash")
            {
                return !check;
            }
            else if (msg.Text == "Stop cash delivery")
            {
                return check;
            }
            return false;
        }

        private void OnDeliverCashSelected()
        {
            DealerManager dealerManager = DealerManager.GetManager(npc.GUID.ToString());
            dealerManager.DealerData.DeliverCash = true;

            float threshold = 1500f;
            Message msg = new($"I will deliver cash if i got ${threshold} in my pockets.", Message.ESenderType.Other);
            Conversation.SendMessage(msg, false, true);
        }

        private void OnStopCashDeliverySelected()
        {
            DealerManager dealerManager = DealerManager.GetManager(npc.GUID.ToString());
            dealerManager.DealerData.DeliverCash = false;

            Message msg = new($"I will not deliver cash anymore.", Message.ESenderType.Other);
            Conversation.SendMessage(msg, false, true);
        }

        public static ConversationManager GetConversation(string nPCName)
        {
            ConversationManager manager = _cache.Find(x => x.npc.name.Contains(nPCName));

            if (manager == null)
            {
                Utils.Logger.Debug("ConversationManager", $"Could not find conversation for: {nPCName}");
                return null;
            }

            return manager;
        }

        public static List<ConversationManager> GetAllConversations() =>
            _cache;

        public static void ClearAll()
        {
            _cache.Clear();

            Utils.Logger.Debug("ConversationManager", "Conversations deinitialized");
        }

        public static bool ConversationExists(string nPCName)
        {
            ConversationManager instance = _cache.Find(x => x.npc.name.Contains(nPCName));

            return instance != null;
        }
    }
}
