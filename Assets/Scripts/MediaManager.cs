#if ENABLE_WINMD_SUPPORT

using ConversationLibrary.Interfaces;
using ConversationLibrary.Utility;
using Org.WebRtc;
using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.WSA;
using Windows.Media.Core;

public class MediaManager : IMediaManager
{
    // This constructor will be used by the cheap IoC container
    public MediaManager()
    {
        this.textureDetails = CheapContainer.Resolve<ITextureDetailsProvider>();
    }
    // The idea is that this constructor would be used by a real IoC container.
    public MediaManager(ITextureDetailsProvider textureDetails)
    {
        this.textureDetails = textureDetails;
    }
    public Media Media => this.media;

    public MediaStream UserMedia => this.userMedia;

    public MediaVideoTrack RemoteVideoTrack { get => remoteVideoTrack; set => remoteVideoTrack = value; }

    public async Task AddLocalStreamAsync(MediaStream stream)
    {
        //var track = stream?.GetVideoTracks()?.FirstOrDefault();

        //if (track != null)
        //{
        //    // TODO: figure out why this is I420 rather than passing the Kind property.
        //    this.CreateLocalMediaStreamSource(track, "I420", "SELF");
        //}
    }

    public async Task AddRemoteStreamAsync(MediaStream stream)
    {
        var track = stream?.GetVideoTracks()?.FirstOrDefault();

        if (track != null)
        {
            this.InvokeOnUnityMainThread(
                () => this.CreateRemoteMediaStreamSource(track, "H264", "PEER"));
        }
    }
    void InvokeOnUnityMainThread(AppCallbackItem callback)
    {
        // TODO: which thread is this meant to be on?
        UnityEngine.WSA.Application.InvokeOnAppThread(callback,false);
    }
    void InvokeOnUnityUIThread(AppCallbackItem callback)
    {
        // TODO: which thread is this meant to be on?
        UnityEngine.WSA.Application.InvokeOnUIThread(callback, false);
    }
    public async Task CreateAsync(bool audioEnabled = true, bool videoEnabled = true)
    {
        this.media = Media.CreateMedia();

        // TODO: for the moment, turning audio off as I get an access violation in
        // some piece of code that'll take some debugging.
        RTCMediaStreamConstraints constraints = new RTCMediaStreamConstraints()
        {
            audioEnabled = false,
            videoEnabled = videoEnabled
        };
        this.userMedia = await media.GetUserMedia(constraints);
    }

    public void RemoveLocalStream()
    {
        this.InvokeOnUnityMainThread(
            () => this.DestroyLocalMediaStreamSource());
    }

    public void RemoveRemoteStream()
    {
        this.InvokeOnUnityMainThread(
            () => this.DestroyRemoteMediaStreamSource());
    }

    public void Shutdown()
    {
        if (this.media != null)
        {
            if (this.localVideoTrack != null)
            {
                this.localVideoTrack.Dispose();
                this.localVideoTrack = null;
            }
            if (this.RemoteVideoTrack != null)
            {
                this.RemoteVideoTrack.Dispose();
                this.RemoteVideoTrack = null;
            }
            this.userMedia = null;
            this.media.Dispose();
            this.media = null;
        }
    }
    void CreateLocalMediaStreamSource(object track, string type, string id)
    {
        Plugin.CreateLocalMediaPlayback();
        IntPtr playbackTexture = IntPtr.Zero;
        Plugin.GetLocalPrimaryTexture(
            this.textureDetails.Details.LocalTextureWidth, 
            this.textureDetails.Details.LocalTextureHeight, 
            out playbackTexture);

        this.textureDetails.Details.LocalTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = 
            (Texture)Texture2D.CreateExternalTexture(
                (int)this.textureDetails.Details.LocalTextureWidth, 
                (int)this.textureDetails.Details.LocalTextureHeight, 
                (TextureFormat)14, false, false, playbackTexture);

#if ENABLE_WINMD_SUPPORT
        Plugin.LoadLocalMediaStreamSource(
            (MediaStreamSource)Org.WebRtc.Media.CreateMedia().CreateMediaStreamSource((MediaVideoTrack)track, type, id));
#endif
        Plugin.LocalPlay();
    }

    void DestroyLocalMediaStreamSource()
    {
        this.textureDetails.Details.LocalTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = null;
        Plugin.ReleaseLocalMediaPlayback();
    }

    void CreateRemoteMediaStreamSource(object track, string type, string id)
    {
        Plugin.CreateRemoteMediaPlayback();
        IntPtr playbackTexture = IntPtr.Zero;
        Plugin.GetRemotePrimaryTexture(
            this.textureDetails.Details.RemoteTextureWidth, 
            this.textureDetails.Details.RemoteTextureHeight, 
            out playbackTexture);

        this.textureDetails.Details.RemoteTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = 
            (Texture)Texture2D.CreateExternalTexture(
                (int)this.textureDetails.Details.RemoteTextureWidth, 
                (int)this.textureDetails.Details.RemoteTextureHeight, 
                (TextureFormat)14, false, false, playbackTexture);

#if ENABLE_WINMD_SUPPORT
        Plugin.LoadRemoteMediaStreamSource(
            (MediaStreamSource)Org.WebRtc.Media.CreateMedia().CreateMediaStreamSource((MediaVideoTrack)track, type, id));
#endif
        Plugin.RemotePlay();
    }

    void DestroyRemoteMediaStreamSource()
    {
        this.textureDetails.Details.RemoteTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = null;
        Plugin.ReleaseRemoteMediaPlayback();
    }
    Media media;
    MediaStream userMedia;
    MediaVideoTrack remoteVideoTrack;
    MediaVideoTrack localVideoTrack;
    ITextureDetailsProvider textureDetails;
}
#endif
