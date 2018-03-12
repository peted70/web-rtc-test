namespace ConversationLibrary.Interfaces
{
    using System.Threading.Tasks;

    public delegate void SignedInDelegate();
    public delegate void DisconnectedDelegate();
    public delegate void PeerConnectedDelegate(object peerId, string name);
    public delegate void PeerDisonnectedDelegate(object peerId);
    public delegate void PeerHangupDelegate(object peerId);
    public delegate void MessageFromPeerDelegate(object peerId, string message);
    public delegate void ServerConnectionFailureDelegate();

    public interface ISignallingService
    {
        event DisconnectedDelegate OnDisconnected;
        event MessageFromPeerDelegate OnMessageFromPeer;
        event PeerConnectedDelegate OnPeerConnected;
        event PeerDisonnectedDelegate OnPeerDisconnected;
        event PeerHangupDelegate OnPeerHangup;
        event ServerConnectionFailureDelegate OnServerConnectionFailure;
        event SignedInDelegate OnSignedIn;

        Task ConnectAsync(string server, string port, string client_name);
        bool IsConnected();
        Task<bool> SendToPeerAsync(object peerId, string message);
        Task<bool> SignOutAsync();
    }
}