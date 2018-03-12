namespace ConversationLibrary.Signalling
{
    using Org.WebRtc;
    using Windows.Data.Json;

    static class SignallerMessagingExtensions
    {
        public enum MessageType
        {
            Offer,
            Answer,
            Ice,
            Unknown
        }
        public static MessageType GetMessageType(JsonObject message)
        {
            var type = MessageType.Unknown;

            // if the message contains a message type then we know what it is because this application
            // must be on the other end sending it.
            IJsonValue value;

            if (message.TryGetValue(messageType, out value))
            {
                type = (MessageType)value.GetNumber();
            }
            else
            {
                // The sample app must be on the other end so we have to try harder.
                if (message.ContainsKey(kSessionDescriptionTypeName))
                {
                    // this is SDP - could be an offer or an answer (or a pranswer which I'm ignoring).
                    var sdpType = message.GetNamedString(kSessionDescriptionTypeName);
                    type = sdpType == "offer" ? MessageType.Offer : MessageType.Answer;
                }
                else if (message.ContainsKey(kCandidateSdpMidName))
                {
                    type = MessageType.Ice;
                }
            }
            return (type);
        }
        public static RTCIceCandidate IceCandidateFromJsonMessage(JsonObject jsonObject)
        {
            var candidate = new RTCIceCandidate(
                jsonObject.GetNamedString(kCandidateSdpName),
                jsonObject.GetNamedString(kCandidateSdpMidName),
                (ushort)jsonObject.GetNamedNumber(kCandidateSdpMlineIndexName));

            return (candidate);
        }
        public static string ToJsonMessageString(this RTCIceCandidate candidate)
        {
            var json = new JsonObject()
            {
                { messageType,  JsonValue.CreateNumberValue((int)MessageType.Ice) },
                { kCandidateSdpMidName, JsonValue.CreateStringValue(candidate.SdpMid) },
                { kCandidateSdpMlineIndexName, JsonValue.CreateNumberValue(candidate.SdpMLineIndex)},
                { kCandidateSdpName, JsonValue.CreateStringValue(candidate.Candidate)}
            };
            return (json.Stringify());
        }
        public static string ToJsonMessageString(this RTCSessionDescription description,
            MessageType jsonMessageType)
        {
            var json = new JsonObject()
            {
                { messageType, JsonValue.CreateNumberValue((int)jsonMessageType) },
                { kSessionDescriptionTypeName, JsonValue.CreateStringValue(description.Type.GetValueOrDefault().ToString().ToLower()) },
                { kSessionDescriptionSdpName, JsonValue.CreateStringValue(description.Sdp) }
            };
            return (json.Stringify());
        }
        public static string SdpFromJsonMessage(JsonObject jsonObject)
        {
            return (jsonObject.GetNamedString(kSessionDescriptionSdpName));
        }
        // My addition to the protocol that's already part of the sample - I add a message type although
        // I think it can be inferred anyway so my app will always add this whereas the sample app won't
        // have it.
        private static readonly string messageType = "messageType";

        // SDP negotiation attributes
        private static readonly string kCandidateSdpMidName = "sdpMid";
        private static readonly string kCandidateSdpMlineIndexName = "sdpMLineIndex";
        private static readonly string kCandidateSdpName = "candidate";
        private static readonly string kSessionDescriptionTypeName = "type";
        private static readonly string kSessionDescriptionSdpName = "sdp";

    }
}