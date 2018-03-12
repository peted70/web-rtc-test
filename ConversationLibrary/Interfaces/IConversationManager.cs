using System.Threading.Tasks;

namespace ConversationLibrary.Interfaces
{
    public interface IConversationManager
    {
        bool IsInitiator { get; set; }

        Task<bool> ConnectToSignallingAsync(string ipAddress, int port);

        Task InitialiseAsync(string localHostName);
    }
}