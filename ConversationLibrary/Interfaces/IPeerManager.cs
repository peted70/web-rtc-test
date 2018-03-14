using System.Threading.Tasks;
using Org.WebRtc;

namespace ConversationLibrary.Interfaces
{
    public interface IPeerManager
    {
        object PeerId { get; }
        event RTCPeerConnectionIceEventDelegate OnIceCandidate;
        void CreateConnectionForPeerAsync(object peerId);
        Task<RTCSessionDescription> CreateAndSetLocalOfferAsync(
            string videoCodecName, int? videoClockRate);

        Task AddIceCandidateAsync(RTCIceCandidate iceCandidate);
        Task<RTCSessionDescription> AcceptRemoteOfferAsync(object peerId, string sdpDescription);
        Task AcceptRemoteAnswerAsync(string sdpAnswer);
        void Shutdown();
    }
}