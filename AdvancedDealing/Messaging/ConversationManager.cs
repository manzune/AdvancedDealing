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
        private static readonly List<ConversationManager> _cache = [];

        private readonly List<MessageBase> _messageList = [];

        private readonly List<MessageBase> _sendableMessages = [];

        public readonly NPC npc;

        public readonly MSGConversation conversation;

        private bool _uiPatched;

        public ConversationManager(NPC npc)
        {
            this.npc = npc;
            conversation = npc.MSGConversation;

            Utils.Logger.Debug("ConversationManager", $"Conversation created: {npc.GUID}");

            _cache.Add(this);
        }

        public void CreateSendableMessages()
        {
            foreach (MessageBase msg in _messageList)
            {
                if (!_sendableMessages.Contains(msg))
                {
                    SendableMessage sMsg = conversation.CreateSendableMessage(msg.Text);
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

            if (!_uiPatched)
            {
                npc.ConversationCanBeHidden = false;

                conversation.EnsureUIExists();
                conversation.SetEntryVisibility(true);

                _uiPatched = true;
            }
        }

        public void AddMessage(MessageBase message)
        {
            Type type = message.GetType();

            if (_messageList.Exists(a => a.GetType() == type)) return;

            message.SetReferences(npc, this, conversation);
            _messageList.Add(message);
        }

        public static ConversationManager GetManager(string npcGuid)
        {
            ConversationManager manager = _cache.Find(x => x.npc.GUID.ToString().Contains(npcGuid));

            if (manager == null)
            {
                Utils.Logger.Debug("ConversationManager", $"Could not find conversation for: {npcGuid}");
                return null;
            }

            return manager;
        }

        public static List<ConversationManager> GetAllManager()
        {
            return _cache;
        }

        public static void ClearAll()
        {
            _cache.Clear();

            Utils.Logger.Debug("ConversationManager", "Conversations deinitialized");
        }

        public static bool ScheduleExists(string npcName)
        {
            ConversationManager instance = _cache.Find(x => x.npc.name.Contains(npcName));

            return instance != null;
        }
    }
}
