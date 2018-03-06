using System;
using System.Runtime.InteropServices;
using UnityEngine;
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
    public GameObject RemoteTexture;
    public GameObject LocalTexture;

    public void CreateLocalMediaStreamSource(object track, string type, string id)
    {
        Plugin.CreateLocalMediaPlayback();
        IntPtr playbackTexture = IntPtr.Zero;
        Plugin.GetLocalPrimaryTexture(LocalTextureWidth, LocalTextureHeight, out playbackTexture);
        LocalTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = Texture2D.CreateExternalTexture((int)LocalTextureWidth, (int)LocalTextureHeight, (TextureFormat)14, false, false, playbackTexture);
#if ENABLE_WINMD_SUPPORT
        Plugin.LoadLocalMediaStreamSource((MediaStreamSource)Media.CreateMedia().CreateMediaStreamSource((MediaVideoTrack)track, type, id));
#endif
        Plugin.LocalPlay();
    }

    public void DestroyLocalMediaStreamSource()
    {
        Plugin.ReleaseLocalMediaPlayback();
    }

    public void CreateRemoteMediaStreamSource(object track, string type, string id)
    {
        Plugin.CreateRemoteMediaPlayback();
        IntPtr playbackTexture = IntPtr.Zero;
        Plugin.GetRemotePrimaryTexture(RemoteTextureWidth, RemoteTextureHeight, out playbackTexture);
        RemoteTexture.GetComponent<Renderer>().sharedMaterial.mainTexture = Texture2D.CreateExternalTexture((int)RemoteTextureWidth, (int)RemoteTextureHeight, (TextureFormat)14, false, false, playbackTexture);
#if ENABLE_WINMD_SUPPORT
        Plugin.LoadRemoteMediaStreamSource((MediaStreamSource)Media.CreateMedia().CreateMediaStreamSource((MediaVideoTrack)track, type, id));
#endif
        Plugin.RemotePlay();
    }

    public void DestroyRemoteMediaStreamSource()
    {
        Plugin.ReleaseRemoteMediaPlayback();
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
