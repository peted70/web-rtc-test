using System.Threading.Tasks;
using Org.WebRtc;

namespace ConversationLibrary.Interfaces
{
    public interface IMediaManager
    {
        Media Media { get; }
        MediaStream UserMedia { get; }
        Task CreateAsync(bool audioEnabled = true, bool videoEnabled = true);
        Task AddRemoteStreamAsync(MediaStream stream);
        Task AddLocalStreamAsync(MediaStream stream);
        void RemoveRemoteStream();
        void RemoveLocalStream();
        void Shutdown();
    }
}