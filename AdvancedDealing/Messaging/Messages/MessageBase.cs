#if IL2CPP
using Il2CppScheduleOne.Messaging;
using Il2CppScheduleOne.NPCs;
#elif MONO
using ScheduleOne.Messaging;
using ScheduleOne.NPCs;
#endif

namespace AdvancedDealing.Messaging.Messages
{
    public abstract class MessageBase
    {
        public virtual string Text => "Text";

        protected NPC npc;

        protected MSGConversation s1Conversation;

        protected ConversationManager conversation;

        public virtual bool DisableDefaultSendBehaviour => false;

        public virtual void SetReferences(NPC npc, ConversationManager conversation, MSGConversation originalConversation)
        {
            this.npc = npc;
            this.conversation = conversation;
            this.s1Conversation = originalConversation;
        }

        public virtual bool ShouldShowCheck(SendableMessage sMsg)
        {
            return true;
        }

        public virtual void OnSelected() { }

        public virtual void OnSent() { }
    }
}
