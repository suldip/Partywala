using System;

namespace PartyClap.Models
{
    public class MessageConversationSummary
    {
        public string OtherUserId { get; set; }
        public string OtherUserName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastTimestamp { get; set; }
        public int UnreadCount { get; set; }
    }
}
