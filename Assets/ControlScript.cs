using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Internal;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_WINMD_SUPPORT
using Org.WebRtc;
using Windows.Media.Core;
#endif

public class ControlScript : MonoBehaviour
{
    public uint LocalTextureWidth;
    public uint LocalTextureHeight;
    public uint RemoteTextureWidth;
    public uint RemoteTextureHeight;
    public RawImage LocalVideoImage;
    public RawImage RemoteVideoImage;

    public void CreateLocalMediaStreamSource(object track, string type, string id)
    {
        ControlScript.Plugin.CreateLocalMediaPlayback();
        IntPtr playbackTexture = IntPtr.Zero;
        ControlScript.Plugin.GetLocalPrimaryTexture(this.LocalTextureWidth, this.LocalTextureHeight, out playbackTexture);
        LocalVideoImage.texture = (Texture)Texture2D.CreateExternalTexture((int)this.LocalTextureWidth, (int)this.LocalTextureHeight, (TextureFormat)14, false, false, playbackTexture);
#if ENABLE_WINMD_SUPPORT
       ControlScript.Plugin.LoadLocalMediaStreamSource((MediaStreamSource)Org.WebRtc.Media.CreateMedia().CreateMediaStreamSource((MediaVideoTrack)track, type, id));
#endif
        ControlScript.Plugin.LocalPlay();
    }

    public void DestroyLocalMediaStreamSource()
    {
        this.LocalVideoImage.texture = null;
        ControlScript.Plugin.ReleaseLocalMediaPlayback();
    }

    public void CreateRemoteMediaStreamSource(object track, string type, string id)
    {
        ControlScript.Plugin.CreateRemoteMediaPlayback();
        IntPtr playbackTexture = IntPtr.Zero;
        ControlScript.Plugin.GetRemotePrimaryTexture(this.RemoteTextureWidth, this.RemoteTextureHeight, out playbackTexture);
        this.RemoteVideoImage.texture = (Texture)Texture2D.CreateExternalTexture((int)this.RemoteTextureWidth, (int)this.RemoteTextureHeight, (TextureFormat)14, false, false, playbackTexture);
#if ENABLE_WINMD_SUPPORT
        ControlScript.Plugin.LoadRemoteMediaStreamSource((MediaStreamSource)Org.WebRtc.Media.CreateMedia().CreateMediaStreamSource((MediaVideoTrack)track, type, id));
#endif
        ControlScript.Plugin.RemotePlay();
    }

    public void DestroyRemoteMediaStreamSource()
    {
        this.RemoteVideoImage.texture = null;
        ControlScript.Plugin.ReleaseRemoteMediaPlayback();
    }

    private static class Plugin
    {
        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void CreateLocalMediaPlayback();

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void CreateRemoteMediaPlayback();

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void ReleaseLocalMediaPlayback();

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void ReleaseRemoteMediaPlayback();

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void GetLocalPrimaryTexture(uint width, uint height, out IntPtr playbackTexture);

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void GetRemotePrimaryTexture(uint width, uint height, out IntPtr playbackTexture);

#if ENABLE_WINMD_SUPPORT
        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void LoadLocalMediaStreamSource(MediaStreamSource IMediaSourceHandler);

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void LoadRemoteMediaStreamSource(MediaStreamSource IMediaSourceHandler);
#endif

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void LocalPlay();

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void RemotePlay();

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void LocalPause();

        [DllImport("MediaEngineUWP", CallingConvention = CallingConvention.StdCall)]
        internal static extern void RemotePause();
    }
}
