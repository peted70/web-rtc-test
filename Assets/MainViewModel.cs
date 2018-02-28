using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using PeerConnectionClient.Model;
using System;
using System.Linq;
#if ENABLE_WINMD_SUPPORT
using Windows.UI.Core;
using Org.WebRtc;
using PeerConnectionClient.Signalling;
#endif

public class MainViewModel : MonoBehaviour
{
    public string ServerIP = "52.174.16.92";
    public int PortNumber = 8888;

#if ENABLE_WINMD_SUPPORT
    private MediaVideoTrack _peerVideoTrack;
    public CodecInfo SelectedVideoCodec;
    public CodecInfo SelectedAudioCodec;
    private List<CodecInfo> AudioCodecs;
    private List<CodecInfo> VideoCodecs;
    private MediaVideoTrack _selfVideoTrack;

    internal Peer SelectedPeer { get; private set; }
#endif

    // Use this for initialization
    void Start()
    {
#if ENABLE_WINMD_SUPPORT 
        WebRTC.Initialize(Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher);
        Conductor.Instance.Signaller.OnPeerConnected += (peerId, peerName) =>
        {
            SelectedPeer = new Peer { Id = peerId, Name = peerName };
        };
        ConnectCommandExecute(null);

        Conductor.Instance.OnAddRemoteStream += Conductor_OnAddRemoteStream;
        Conductor.Instance.OnRemoveRemoteStream += Conductor_OnRemoveRemoteStream;
        Conductor.Instance.OnAddLocalStream += Conductor_OnAddLocalStream;
        Conductor.Instance.OnConnectionHealthStats += Conductor_OnPeerConnectionHealthStats;

        // Prepare to list supported audio codecs
        AudioCodecs = new List<CodecInfo>();
        var audioCodecList = WebRTC.GetAudioCodecs();

        // These are features added to existing codecs, they can't decode/encode real audio data so ignore them
        string[] incompatibleAudioCodecs = new string[] { "CN32000", "CN16000", "CN8000", "red8000", "telephone-event8000" };

        // Prepare to list supported video codecs
        VideoCodecs = new List<CodecInfo>();

        // Order the video codecs so that the stable VP8 is in front.
        var videoCodecList = WebRTC.GetVideoCodecs().OrderBy(codec =>
        {
            switch (codec.Name)
            {
                case "VP8": return 1;
                case "VP9": return 2;
                case "H264": return 3;
                default: return 99;
            }
        });

        // Load the supported audio/video information into the Settings controls
        foreach (var audioCodec in audioCodecList)
        {
            if (!incompatibleAudioCodecs.Contains(audioCodec.Name + audioCodec.ClockRate))
            {
                AudioCodecs.Add(audioCodec);
            }
        }

        if (AudioCodecs.Count > 0)
        {
            //if (settings.Values["SelectedAudioCodecName"] != null)
            //{
                string name = "";// Convert.ToString(settings.Values["SelectedAudioCodecName"]);
                foreach (var audioCodec in AudioCodecs)
                {
                    string audioCodecName = audioCodec.Name;
                    if (audioCodecName == name)
                    {
                        SelectedAudioCodec = audioCodec;
                        break;
                    }
                }
            //}
            if (SelectedAudioCodec == null)
            {
                SelectedAudioCodec = AudioCodecs.First();
            }
        }

        foreach (var videoCodec in videoCodecList)
        {
            VideoCodecs.Add(videoCodec);
        }

        if (VideoCodecs.Count > 0)
        {
            //if (settings.Values["SelectedVideoCodecName"] != null)
            //{
            string name = "";// Convert.ToString(settings.Values["SelectedVideoCodecName"]);
            foreach (var videoCodec in VideoCodecs)
            {
                string videoCodecName = videoCodec.Name;
                if (videoCodecName == name)
                {
                    SelectedVideoCodec = videoCodec;
                    break;
                }
            }
            //}
            if (SelectedVideoCodec == null)
            {
                SelectedVideoCodec = VideoCodecs.First();
            }
        }

        Conductor.Instance.OnPeerConnectionCreated += () =>
        {
            RunOnUiThread(() =>
            {
                IsReadyToConnect = false;
                IsConnectedToPeer = true;
                IsReadyToDisconnect = false;
            });
        };

        // Connection between the current user and a peer is closed event handler
        Conductor.Instance.OnPeerConnectionClosed += () =>
        {
            RunOnUiThread(() =>
            {
                IsConnectedToPeer = false;
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    UnityEngine.GameObject go = UnityEngine.GameObject.Find("Control");
                    go.GetComponent<ControlScript>().DestroyLocalMediaStreamSource();
                    go.GetComponent<ControlScript>().DestroyRemoteMediaStreamSource();
                }, false);

                _peerVideoTrack = null;
                _selfVideoTrack = null;
                GC.Collect(); // Ensure all references are truly dropped.

                IsMicrophoneEnabled = true;
                IsCameraEnabled = true;
                SelfVideoFps = PeerVideoFps = "";
            });
        };
#endif
    }

#if ENABLE_WINMD_SUPPORT
    private String _peerVideoFps;

    /// <summary>
    /// Frame rate per second for the peer's video.
    /// </summary>
    public String PeerVideoFps
    {
        get { return _peerVideoFps; }
        set { if (_peerVideoFps == value) return; _peerVideoFps = value; }
    }

    private String _selfVideoFps;

    /// <summary>
    /// Frame rate per second for the self video.
    /// </summary>
    public String SelfVideoFps
    {
        get { return _selfVideoFps; }
        set { if (_selfVideoFps == value) return; _selfVideoFps = value; }
    }


    private bool _isMicrophoneEnabled = true;

    /// <summary>
    /// Indicator if the microphone is enabled.
    /// </summary>
    public bool IsMicrophoneEnabled
    {
        get { return _isMicrophoneEnabled; }
        set { if (_isMicrophoneEnabled == value) return; _isMicrophoneEnabled = value; }
    }

    private bool _isCameraEnabled = true;

    /// <summary>
    /// Indicator if the camera is enabled.
    /// </summary>
    public bool IsCameraEnabled
    {
        get { return _isCameraEnabled; }
        set { if (_isCameraEnabled == value) return; _isCameraEnabled = value; }
    }

    private bool _cameraEnabled = true;
    private bool IsReadyToConnect;
    private bool IsConnectedToPeer;
    private bool IsReadyToDisconnect;
    private bool _microphoneIsOn;
    private bool VideoLoopbackEnabled;

    /// <summary>
    /// Camera on/off toggle button.
    /// Disabled/Enabled local stream if the camera is off/on.
    /// </summary>
    public bool CameraEnabled
    {
        get { return _cameraEnabled; }
        set
        {
            if (_cameraEnabled == value)
                return;

            _cameraEnabled = value;

            if (IsConnectedToPeer)
            {
                if (_cameraEnabled)
                {
                    Conductor.Instance.EnableLocalVideoStream();
                }
                else
                {
                    Conductor.Instance.DisableLocalVideoStream();
                }
            }
        }
    }

    private void Conductor_OnPeerConnectionHealthStats(RTCPeerConnectionHealthStats evt)
    {
    }

    private void Conductor_OnAddLocalStream(MediaStreamEvent evt)
    {
        _selfVideoTrack = evt.Stream.GetVideoTracks().FirstOrDefault();
        if (_selfVideoTrack != null)
        {
            RunOnUiThread(() =>
            {
                if (_cameraEnabled)
                {
                    Conductor.Instance.EnableLocalVideoStream();
                }
                else
                {
                    Conductor.Instance.DisableLocalVideoStream();
                }

                if (_microphoneIsOn)
                {
                    Conductor.Instance.UnmuteMicrophone();
                }
                else
                {
                    Conductor.Instance.MuteMicrophone();
                }
            });
            if (VideoLoopbackEnabled)
            {
                UnityEngine.WSA.Application.InvokeOnAppThread(() =>
                {
                    GameObject go = GameObject.Find("Control");
                    go.GetComponent<ControlScript>().CreateLocalMediaStreamSource(_selfVideoTrack, "I420", "SELF");

                }, false);
            }
        }
    }

    private void Conductor_OnRemoveRemoteStream(MediaStreamEvent evt)
    {
    }

    private void Conductor_OnAddRemoteStream(MediaStreamEvent evt)
    {
        _peerVideoTrack = evt.Stream.GetVideoTracks().FirstOrDefault();
        if (_peerVideoTrack != null)
        {
            UnityEngine.WSA.Application.InvokeOnAppThread(() =>
            {
                // do stuff with _peerVideoTrack.
                GameObject go = UnityEngine.GameObject.Find("Control");
                if (SelectedVideoCodec.Name == "H264")
                {
                    go.GetComponent<ControlScript>().CreateRemoteMediaStreamSource(_peerVideoTrack, "H264", "PEER");
                }
                else
                {
                    go.GetComponent<ControlScript>().CreateRemoteMediaStreamSource(_peerVideoTrack, "I420", "PEER");
                }

            }, false);
        }
    }
#endif

    private void ConnectCommandExecute(object obj)
    {
#if ENABLE_WINMD_SUPPORT
        new Task(() =>
        {
            Conductor.Instance.StartLogin(ServerIP, PortNumber.ToString());
        }).Start();
#endif
    }


    private void ConnectToPeerCommandExecute(object obj)
    {
#if ENABLE_WINMD_SUPPORT
        new Task(() => { Conductor.Instance.ConnectToPeer(SelectedPeer); }).Start();
#endif
    }

    /// <summary>
    /// Schedules the provided callback on the UI thread from a worker thread, and
    //  returns the results asynchronously.
    /// </summary>
    /// <param name="fn">The function to execute</param>
    protected void RunOnUiThread(Action fn)
    {
#if ENABLE_WINMD_SUPPORT
        var asyncOp = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler(fn));
#endif
    }

    bool tryConnectToPeer = false;

    // Update is called once per frame
    void Update()
    {
#if ENABLE_WINMD_SUPPORT
        if (!tryConnectToPeer && !IsConnectedToPeer)
        {
            ConnectToPeerCommandExecute(null);
            tryConnectToPeer = true;
        }
#endif
    }
}
