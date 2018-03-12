namespace ConversationLibrary
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using ConversationLibrary.Interfaces;
    using ConversationLibrary.Utility;
    using Org.WebRtc;

    public class PeerManager : IPeerManager
    {
        public event RTCPeerConnectionIceEventDelegate OnIceCandidate;

        // Intention is that this constructor is used when using our cheap IoC.
        public PeerManager()
        {
            this.mediaManager = CheapContainer.Resolve<IMediaManager>();
        }
        // And this one for real IoC (autofac in my case)
        public PeerManager(IMediaManager mediaManager)
        {
            this.mediaManager = mediaManager;
        }
        public object PeerId => this.currentPeerId.Value;

        public void CreateConnectionForPeerAsync(object peerId)
        {
            this.currentPeerId = (int)peerId;

            if (this.peerConnection == null)
            {
                this.peerConnection = new RTCPeerConnection(
                    new RTCConfiguration()
                    {
                        BundlePolicy = RTCBundlePolicy.Balanced,
                        IceTransportPolicy = RTCIceTransportPolicy.All
                    }
                );
                this.peerConnection.AddStream(this.mediaManager.UserMedia);
                this.peerConnection.OnAddStream += OnPeerAddsRemoteStreamAsync;
                this.peerConnection.OnIceCandidate += OnLocalIceCandidateDetermined;
            }
        }
        public async Task<RTCSessionDescription> AcceptRemoteOfferAsync(object peerId, string sdpDescription)
        {
            this.CreateConnectionForPeerAsync(peerId);

            // Take the description from the UI and set it as our Remote Description
            // of type 'offer'
            await this.peerConnection.SetRemoteDescription(
                new RTCSessionDescription(RTCSdpType.Offer, sdpDescription));

            // And create our answer
            var answer = await this.peerConnection.CreateAnswer();

            // And set that as our local description
            await this.peerConnection.SetLocalDescription(answer);

            return (answer);
        }
        public async Task<RTCSessionDescription> CreateAndSetLocalOfferAsync()
        {
            // Create the offer.
            var description = await this.peerConnection.CreateOffer();

            // We filter some pieces out of the SDP based on what I think
            // aren't supported Codecs. I largely took it from the original sample
            // when things didn't work for me without it.
            var filteredDescriptionSdp = SdpUtility.FilterToSupportedCodecs(description.Sdp);

            description.Sdp = filteredDescriptionSdp;

            // Set that filtered offer description as our local description.
            await this.peerConnection.SetLocalDescription(description);

            return (description);
        }
        public async Task AcceptRemoteAnswerAsync(string sdpAnswer)
        {
            await this.peerConnection.SetRemoteDescription(new RTCSessionDescription(RTCSdpType.Answer, sdpAnswer));
        }
        public async Task AddIceCandidateAsync(RTCIceCandidate iceCandidate)
        {
            await this.peerConnection.AddIceCandidate(iceCandidate);
        }
        void OnLocalIceCandidateDetermined(RTCPeerConnectionIceEvent iceCandidate)
        {
            this.OnIceCandidate?.Invoke(iceCandidate);
        }
        async void OnPeerAddsRemoteStreamAsync(MediaStreamEvent args)
        {
            var stream = args?.Stream;

            if (stream != null)
            {
                await this.mediaManager.AddRemoteStreamAsync(stream);
            }
        }
        public void Shutdown()
        {
            if (this.peerConnection != null)
            {
                this.mediaManager.RemoveRemoteStream();
                this.peerConnection.OnIceCandidate -= this.OnLocalIceCandidateDetermined;
                this.peerConnection.OnAddStream -= this.OnPeerAddsRemoteStreamAsync;
                this.peerConnection.Close();
                this.peerConnection = null;
                this.currentPeerId = null;
            }
        }
        IMediaManager mediaManager;
        RTCPeerConnection peerConnection;
        int? currentPeerId;
    }
}
