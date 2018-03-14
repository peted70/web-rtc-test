namespace ConversationLibrary.Utility
{
    using Org.WebRtc;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    public static class SdpUtility
    {
        /// <summary>
        /// Forces the SDP to use the selected audio and video codecs.
        /// </summary>
        /// <param name="sdp">Session description.</param>
        /// <param name="audioCodec">Audio codec.</param>
        /// <param name="videoCodec">Video codec.</param>
        /// <returns>True if succeeds to force to use the selected audio/video codecs.</returns>
        public static void SelectCodecs(ref string sdp, CodecInfo audioCodec, CodecInfo videoCodec)
        {
            string audioCodecName = audioCodec?.Name ?? null;
            string videoCodecName = videoCodec?.Name ?? null;

            if (audioCodecName != null)
            {
                MaybePreferCodec(ref sdp, "audio", "receive", audioCodecName);
                MaybePreferCodec(ref sdp, "audio", "send", audioCodecName);
            }
            if (videoCodecName != null)
            {
                MaybePreferCodec(ref sdp, "video", "receive", videoCodecName);
                MaybePreferCodec(ref sdp, "video", "send", videoCodecName);
            }
        }

        /// <summary>
        /// WARNING! :-)
        /// Heavily borrowed from the original sample with some mods - the original sample also did
        /// some work to pick a specific video codec and also to move VP8 to the head of the list
        /// but I've not done that yet.
        /// 
        /// Additionally, at some point it seems that the CodecInfo structure lost its ID property
        /// which the original code was using so now it only has a NAME property. This made it
        /// harder for me to leave the original code alone and I had to modify it some more and
        /// add in the GetCodecId() method above to try and reproduce what was originally happening
        /// in the code that I took from the sample. I don't think what I've done is *perfect*
        /// though so I wouldn't be surprised if at some point this code started causing someone
        /// a problem and it needed revisiting.
        /// </summary>
        /// <param name="originalSdp"></param>
        /// <param name="audioCodecs"></param>
        /// <returns></returns>
        public static string FilterToSupportedCodecs(string originalSdp)
        {
            var filteredSdp = originalSdp;

            string[] incompatibleAudioCodecs =
                new string[] { "CN32000", "CN16000", "CN8000", "red8000", "telephone-event8000" };

            var compatibleCodecs = WebRTC.GetAudioCodecs().Where(
                codec => !incompatibleAudioCodecs.Contains(codec.Name + codec.ClockRate) &&
                         !string.IsNullOrEmpty(GetCodecId(codec)));

            Regex mfdRegex = new Regex("\r\nm=audio.*RTP.*?( .\\d*)+\r\n");
            Match mfdMatch = mfdRegex.Match(filteredSdp);

            List<string> mfdListToErase = new List<string>(); //mdf = media format descriptor

            bool audioMediaDescFound = mfdMatch.Groups.Count > 1; //Group 0 is whole match

            if (audioMediaDescFound)
            {
                for (int groupCtr = 1/*Group 0 is whole match*/; groupCtr < mfdMatch.Groups.Count; groupCtr++)
                {
                    for (int captureCtr = 0; captureCtr < mfdMatch.Groups[groupCtr].Captures.Count; captureCtr++)
                    {
                        mfdListToErase.Add(mfdMatch.Groups[groupCtr].Captures[captureCtr].Value.TrimStart());
                    }
                }
                mfdListToErase.RemoveAll(
                    entry => compatibleCodecs.Any(c => GetCodecId(c) == entry));

                // Alter audio entry
                Regex audioRegex = new Regex("\r\n(m=audio.*RTP.*?)( .\\d*)+");

                // TODO: same comment as before
                filteredSdp = audioRegex.Replace(
                    filteredSdp,
                    "\r\n$1 " + string.Join(" ", compatibleCodecs.Select(c => GetCodecId(c))));
            }

            // Remove associated rtp mapping, format parameters, feedback parameters
            Regex removeOtherMdfs = new Regex("a=(rtpmap|fmtp|rtcp-fb):(" + String.Join("|", mfdListToErase) + ") .*\r\n");

            filteredSdp = removeOtherMdfs.Replace(filteredSdp, "");

            return (filteredSdp);
        }
        static string GetCodecId(CodecInfo codecInfo)
        {
            // Taken from https://chromium.googlesource.com/external/webrtc/stable/talk/+/master/media/webrtc/webrtcvoiceengine.cc
            var codecs = new[]
            {
                  new { Name="OPUS",   Bitrate=48000,  Channels=2, Id=111   },
                  new { Name="ISAC",   Bitrate=16000,  Channels=1, Id=103   },
                  new { Name="ISAC",   Bitrate=32000,  Channels=1, Id=104   },
                  new { Name="CELT",   Bitrate=32000,  Channels=1, Id=109   },
                  new { Name="CELT",   Bitrate=32000,  Channels=2, Id=110   },
                  new { Name="G722",   Bitrate=16000,  Channels=1, Id=9     },
                  new { Name="ILBC",   Bitrate=8000,   Channels=1, Id=102   },
                  new { Name="PCMU",   Bitrate=8000,   Channels=1, Id=0     },
                  new { Name="PCMA",   Bitrate=8000,   Channels=1, Id=8     },
                  new { Name="CN",     Bitrate=48000,  Channels=1, Id=107   },
                  new { Name="CN",     Bitrate=32000,  Channels=1, Id=106   },
                  new { Name="CN",     Bitrate=16000,  Channels=1, Id=105   },
                  new { Name="CN",     Bitrate=8000,   Channels=1, Id=13    },
                  new { Name="RED",    Bitrate=8000,   Channels=1, Id=127   },
                  new { Name="TELEPHONE-EVENT", Bitrate=8000, Channels=1, Id=126 }
            };
            var entries = codecs.Where(
                (c => (c.Name == codecInfo.Name.ToUpper()) && c.Bitrate == codecInfo.ClockRate));

            // TODO: Unsure what to do in the case where we get called with a Name/Bitrate
            // and we have no entry for it. We return empty string for it right now.
            return (entries.FirstOrDefault()?.Id.ToString() ?? string.Empty);
        }

        // Sets |codec| as the default |type| codec if it's present.
        // The format of |codec| is 'NAME/RATE', e.g. 'opus/48000'.
        static bool MaybePreferCodec(ref string sdp, string type, string dir, string codec)
        {
            string str = type + " " + dir + " codec";

            string[] sdpLines = sdp.Split(new string[] { "\r\n" }, StringSplitOptions.None);

            // Search for m line.
            var mLineIndex = FindLine(sdpLines, "m=", type);
            if (mLineIndex == -1)
            {
                return false;
            }

            // If the codec is available, set it as the default in m line.
            string payload = null;
            // Iterate through rtpmap enumerations to find all matching codec entries
            for (int i = sdpLines.Length - 1; i >= 0; i--)
            {
                // Finds first match in rtpmap
                int index = FindLineInRange(sdpLines, i, 0, "a=rtpmap", codec, 1);
                if (index != -1)
                {
                    // Skip all of the entries between i and index match
                    i = index;
                    payload = GetCodecPayloadTypeFromLine(sdpLines[index]);
                    if (payload != null)
                    {
                        // Move codec to top
                        sdpLines[mLineIndex] = SetDefaultCodec(sdpLines[mLineIndex], payload);
                    }
                }
                else
                {
                    // No match means we can break the loop
                    break;
                }
            }

            sdp = string.Join("\r\n", sdpLines);
            return true;
        }

        // Find the line in sdpLines that starts with |prefix|, and, if specified,
        // contains |substr| (case-insensitive search).
        static int FindLine(string[] sdpLines, string prefix, string substr)
        {
            return FindLineInRange(sdpLines, 0, -1, prefix, substr);
        }

        // Find the line in sdpLines[startLine...endLine - 1] that starts with |prefix|
        // and, if specified, contains |substr| (case-insensitive search).
        static int FindLineInRange(string[] sdpLines, int startLine, int endLine, string prefix, string substr, int direction = 0)
        {
            if (direction != 0)
            {
                direction = 1;
            }

            if (direction == 0)
            {
                // Search beginning to end
                int realEndLine = endLine != -1 ? endLine : sdpLines.Length;
                for (int i = startLine; i < realEndLine; i++)
                {
                    if (sdpLines[i].StartsWith(prefix))
                    {
                        if (substr == "" || sdpLines[i].ToLower().Contains(substr.ToLower()))
                        {
                            return i;
                        }
                    }
                }
            }
            else
            {
                // Search end to beginning
                var realStartLine = startLine != -1 ? startLine : sdpLines.Length - 1;
                for (var j = realStartLine; j >= 0; --j)
                {
                    if (sdpLines[j].StartsWith(prefix))
                    {
                        if (substr == "" || sdpLines[j].ToLower().Contains(substr.ToLower()))
                        {
                            return j;
                        }
                    }
                }
            }
            return -1;
        }

        // Gets the codec payload type from an a=rtpmap:X line.
        static string GetCodecPayloadTypeFromLine(string sdpLine)
        {
            Regex pattern = new Regex("a=rtpmap:(\\d+) [a-zA-Z0-9-]+\\/\\d+");
            Match result = pattern.Match(sdpLine);
            return (result.Success) ? result.Groups[1].Value : null;
        }

        // Returns a new m= line with the specified codec as the first one.
        static string SetDefaultCodec(string mLine, string payload)
        {
            List<string> elements = new List<string>(mLine.Split(' '));

            // Just copy the first three parameters; codec order starts on fourth.
            List<string> newLine = elements.GetRange(0, 3);

            // Put target payload first and copy in the rest.
            newLine.Add(payload);
            elements.GetRange(3, elements.Count - 3).ForEach(
            delegate (String element)
            {
                if (!element.Equals(payload))
                {
                    newLine.Add(element);
                }
            });

            return String.Join(" ", newLine);
        }
    }
}
