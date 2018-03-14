using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.Threading.Tasks;

#if ENABLE_WINMD_SUPPORT
using ConversationLibrary;
using ConversationLibrary.Interfaces;
using ConversationLibrary.Utility;
using PeerConnectionClient.Signalling;
using Org.WebRtc;
using Windows.Networking.Connectivity;
#endif

public class ControlScript : MonoBehaviour
{
    public TextureDetails TextureDetails;
    public string ServerIP = "52.174.16.92";
    public int PortNumber = 8888;
    public bool IsInitiator = true;
    public string remotePeerName = "mtaultybook";

#if ENABLE_WINMD_SUPPORT
    async void Start()
    {
        CheapContainer.Register<ISignallingService, Signaller>();
        CheapContainer.Register<IDispatcherProvider, DispatcherProvider>();
        CheapContainer.Register<ITextureDetailsProvider, TextureDetailsProvider>();

        var provider = CheapContainer.Resolve<ITextureDetailsProvider>();
        provider.Details = this.TextureDetails;

        CheapContainer.Register<IMediaManager, MediaManager>();
        CheapContainer.Register<IPeerManager, PeerManager>();
        CheapContainer.Register<IConversationManager, ConversationManager>();

        var conversationManager = CheapContainer.Resolve<IConversationManager>();
        conversationManager.IsInitiator = this.IsInitiator;

        // TODO: not really found a good way of abstracting this but I think it has to be called.
        // Does it need moving into the Media Manager and linking to the widths/heights in there?
        // I think I ramped it down to 856? 896? some such.
        WebRTC.SetPreferredVideoCaptureFormat(896, 504, 30);

        // TODO: This is here right now as it feels like a bunch of work gets marshalled
        // back (via Sync Context?) to this thread which then gums up the UI but we'd 
        // like to understand better what that work is.
        Task.Run(
            async () =>
            {
                await conversationManager.InitialiseAsync(this.HostName, this.remotePeerName);

                if (await conversationManager.ConnectToSignallingAsync(this.ServerIP, this.PortNumber,
                    "H264", 90000))
                {
                    // We're good!
                }
            }
        );
    }
    string HostName
    {
        get
        {
            var candidate =
                NetworkInformation.GetHostNames()
                .Where(n => !string.IsNullOrEmpty(n.DisplayName)).FirstOrDefault();

            // Note - only candidate below can be null, not the Displayname
            return (candidate?.DisplayName ?? "Anonymous");
        }
    }
#endif
}
