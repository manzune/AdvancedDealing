using AdvancedDealing.Messaging.Messages;
using System;
using System.Collections.Generic;

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
        public readonly NPC NPC;

        public readonly MSGConversation Conversation;

        public bool UIPatched;

        private static readonly List<ConversationManager> cache = [];

        private readonly List<MessageBase> _messageList = [];

        private readonly List<MessageBase> _sendableMessages = [];

        public ConversationManager(NPC npc)
        {
            NPC = npc;
            Conversation = npc.MSGConversation;

            Utils.Logger.Debug("ConversationManager", $"Conversation created: {npc.fullName}");

            cache.Add(this);
        }

        public void CreateSendableMessages()
        {
            foreach (MessageBase msg in _messageList)
            {
#if IL2CPP
                bool exists = Conversation.Sendables.Exists((Func<SendableMessage, bool>)(x => x.Text == msg.Text));
#elif MONO
                bool exists = Conversation.Sendables.Exists(x => x.Text == msg.Text);
#endif
                if (!_sendableMessages.Contains(msg) && !exists)
                {
                    SendableMessage sMsg = Conversation.CreateSendableMessage(msg.Text);
#if IL2CPP
                    sMsg.ShouldShowCheck = (SendableMessage.BoolCheck)msg.ShouldShowCheck;
#elif MONO
                    sMsg.ShouldShowCheck = msg.ShouldShowCheck;
#endif
                    sMsg.disableDefaultSendBehaviour = msg.DisableDefaultSendBehaviour;
                    sMsg.onSelected = new Action(msg.OnSelected);
                    sMsg.onSent = new Action(msg.OnSent);

                    _sendableMessages.Add(msg);
                }
            }

            if (!UIPatched)
            {
                NPC.ConversationCanBeHidden = false;

                Conversation.EnsureUIExists();
                Conversation.SetEntryVisibility(true);

                UIPatched = true;
            }
        }

        public void Destroy()
        {
            if (UIPatched && NPC != null && Conversation !=null)
            {
                NPC.ConversationCanBeHidden = true;

                Conversation.EnsureUIExists();
                Conversation.SetEntryVisibility(false);
            }

            UIPatched = false;
            cache.Remove(this);
        }

        public void AddMessage(MessageBase message)
        {
            Type type = message.GetType();

            if (_messageList.Exists(a => a.GetType() == type)) return;

            message.SetReferences(NPC, this, Conversation);
            _messageList.Add(message);
        }

        public static ConversationManager GetManager(string npcGuid)
        {
            ConversationManager manager = cache.Find(x => x.NPC.GUID.ToString().Contains(npcGuid));

            if (manager == null)
            {
                Utils.Logger.Debug("ConversationManager", $"Could not find conversation for: {npcGuid}");
                return null;
            }

            return manager;
        }

        public static List<ConversationManager> GetAllManager()
        {
            return cache;
        }

        public static void ClearAll()
        {
            cache.Clear();

            Utils.Logger.Debug("ConversationManager", "Conversations deinitialized");
        }

        public static bool ScheduleExists(string npcName)
        {
            ConversationManager instance = cache.Find(x => x.NPC.name.Contains(npcName));

            return instance != null;
        }
    }
}
