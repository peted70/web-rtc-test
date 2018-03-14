using System.Threading.Tasks;

namespace ConversationLibrary.Interfaces
{
    public interface IConversationManager
    {
        bool IsInitiator { get; set; }

        Task<bool> ConnectToSignallingAsync(string ipAddress, int port,
            string videoCodecName = null, int? videoClockRate = null);

        Task InitialiseAsync(string localHostName, string remotePeerName);
    }
}