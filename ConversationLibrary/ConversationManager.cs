namespace ConversationLibrary
{
    using ConversationLibrary.Interfaces;
    using ConversationLibrary.Signalling;
    using ConversationLibrary.Utility;
    using Org.WebRtc;
    using System;
    using System.Threading.Tasks;
    using Windows.Data.Json;

    public class ConversationManager : IConversationManager
    {
        // This constructor is for when using my cheap IoC
        public ConversationManager()
        {
            this.signaller = CheapContainer.Resolve<ISignallingService>();
            this.peerManager = CheapContainer.Resolve<IPeerManager>();
            this.peerManager.OnIceCandidate += this.OnLocalIceCandidateDeterminedAsync;
            this.mediaManager = CheapContainer.Resolve<IMediaManager>();
            this.dispatcherProvider = CheapContainer.Resolve<IDispatcherProvider>();
        }
        // And this for Autofac or real IoC
        public ConversationManager(
            ISignallingService signaller,
            IPeerManager peerManager,
            IMediaManager mediaManager,
            IDispatcherProvider dispatcherProvider)
        {
            this.signaller = signaller;
            this.peerManager = peerManager;
            this.peerManager.OnIceCandidate += this.OnLocalIceCandidateDeterminedAsync;
            this.mediaManager = mediaManager;
            this.dispatcherProvider = dispatcherProvider;
        }

        public bool IsInitiator
        {
            get; set;
        }

        public async Task InitialiseAsync(string localHostName, string remotePeerName)
        {
            if (!this.initialised)
            {
                this.initialised = true;
                
                this.hostName = localHostName;
                this.remotePeerName = remotePeerName;

                // I find that if I don't do this before Initialize() then I crash.
                await WebRTC.RequestAccessForMediaCapture();

                // TODO: we need a dispatcher here.
                WebRTC.Initialize(dispatcherProvider.Dispatcher);

                await this.mediaManager.CreateAsync();

                await this.mediaManager.AddLocalStreamAsync(this.mediaManager.UserMedia);
            }
        }
        public async Task<bool> ConnectToSignallingAsync(string ipAddress, int port,
            string videoCodecName = null, int? videoClockRate = null)
        {
            TaskCompletionSource<bool> signedIn = new TaskCompletionSource<bool>();

            this.videoCodecName = videoCodecName;
            this.videoClockRate = videoClockRate;

            // Have to do this here because PeerConnected fires from the signaller
            // *before* SignedIn fires from the signaller.
            this.signaller.OnPeerConnected += this.OnSignallingPeerConnected;

            SignedInDelegate successHandler = () =>
            {
                this.signaller.OnMessageFromPeer += this.OnSignallingMessageFromPeer;
                this.signaller.OnDisconnected += this.OnSignallingDisconnected;
                this.signaller.OnPeerHangup += this.OnSignallingPeerHangup;
                signedIn.SetResult(true);
            };
            ServerConnectionFailureDelegate failureHandler = () =>
            {
                this.signaller.OnPeerConnected -= this.OnSignallingPeerConnected;
                signedIn.SetResult(false);
            };

            this.signaller.OnSignedIn += successHandler;
            this.signaller.OnServerConnectionFailure += failureHandler;

            await this.signaller.ConnectAsync(ipAddress, port.ToString(), this.hostName);

            this.signaller.OnSignedIn -= successHandler;
            this.signaller.OnServerConnectionFailure -= failureHandler;

            await signedIn.Task;

            return (signedIn.Task.Result);
        }
        public void ShutDown()
        {
            if (this.signaller.IsConnected())
            {
                // TODO: send BYE?
                this.signaller.OnPeerConnected -= this.OnSignallingPeerConnected;
                this.signaller.OnMessageFromPeer -= this.OnSignallingMessageFromPeer;
                this.signaller.OnDisconnected -= this.OnSignallingDisconnected;
                this.signaller.OnPeerHangup -= this.OnSignallingPeerHangup;
            }
            this.mediaManager.Shutdown();
            this.peerManager.Shutdown();
        }
        async void OnSignallingPeerConnected(object id, string name)
        {
            // We are simply going to jump at the first opportunity we get.
            if (this.IsInitiator && (string.Compare(name, this.remotePeerName, true) == 0))
            {
                // We have found a peer to connect to so we will connect to it.
                this.peerManager.CreateConnectionForPeerAsync((int)id);

                await this.SendOfferToRemotePeerAsync();
            }
        }
        void OnSignallingDisconnected()
        {
            this.ShutDown();
        }
        void OnSignallingPeerHangup(object peerId)
        {
            this.peerManager.Shutdown();
        }
        async void OnSignallingMessageFromPeer(object peerId, string message)
        {
            var numericalPeerId = (int)peerId;

            var jsonObject = JsonObject.Parse(message);

            switch (SignallerMessagingExtensions.GetMessageType(jsonObject))
            {
                case SignallerMessagingExtensions.MessageType.Offer:
                    await this.OnOfferMessageFromPeerAsync(numericalPeerId, jsonObject);
                    break;
                case SignallerMessagingExtensions.MessageType.Answer:
                    await this.OnAnswerMessageFromPeerAsync(numericalPeerId, jsonObject);
                    break;
                case SignallerMessagingExtensions.MessageType.Ice:
                    await this.OnIceMessageFromPeerAsync(numericalPeerId, jsonObject);
                    break;
                default:
                    break;
            }
        }
        async Task OnOfferMessageFromPeerAsync(int peerId, JsonObject message)
        {
            var sdp = SignallerMessagingExtensions.SdpFromJsonMessage(message);
            await this.AcceptRemotePeerOfferAsync(peerId, sdp);
        }
        async Task OnAnswerMessageFromPeerAsync(int peerId, JsonObject message)
        {
            var sdp = SignallerMessagingExtensions.SdpFromJsonMessage(message);
            await this.peerManager.AcceptRemoteAnswerAsync(sdp);
        }
        async Task OnIceMessageFromPeerAsync(int peerId, JsonObject message)
        {
            var candidate = SignallerMessagingExtensions.IceCandidateFromJsonMessage(message);
            await this.peerManager.AddIceCandidateAsync(candidate);
        }
        async Task SendOfferToRemotePeerAsync()
        {
            // Create the offer.
            var description = await this.peerManager.CreateAndSetLocalOfferAsync(
                this.videoCodecName, this.videoClockRate);

            var jsonMessage = description.ToJsonMessageString(
                SignallerMessagingExtensions.MessageType.Offer);

            await this.signaller.SendToPeerAsync(this.peerManager.PeerId, jsonMessage);
        }
        async Task AcceptRemotePeerOfferAsync(int peerId, string sdpDescription)
        {
            // Only if we're expecting a call.
            if (!this.IsInitiator)
            {
                var answer = await this.peerManager.AcceptRemoteOfferAsync(peerId, sdpDescription);

                // And sent it back over the network to the peer as the answer.
                await this.signaller.SendToPeerAsync(
                    this.peerManager.PeerId,
                    answer.ToJsonMessageString(SignallerMessagingExtensions.MessageType.Answer));
            }
        }
        async void OnLocalIceCandidateDeterminedAsync(RTCPeerConnectionIceEvent args)
        {
            // We send this to our connected peer immediately.
            if (this.signaller.IsConnected())
            {
                var jsonMessage = args.Candidate.ToJsonMessageString();
                await this.signaller.SendToPeerAsync(this.peerManager.PeerId, jsonMessage);
            }
        }
        IMediaManager mediaManager;
        IPeerManager peerManager;
        ISignallingService signaller;
        IDispatcherProvider dispatcherProvider;
        string hostName;
        string remotePeerName;
        bool initialised;
        string videoCodecName;
        int? videoClockRate;
    }
}
