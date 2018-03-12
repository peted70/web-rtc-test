using System;
using System.Runtime.InteropServices;

#if ENABLE_WINMD_SUPPORT
using Org.WebRtc;
using Windows.Media.Core;
#endif

static class Plugin
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
