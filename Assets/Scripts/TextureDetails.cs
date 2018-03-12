using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TextureDetails
{
    public uint LocalTextureWidth;
    public uint LocalTextureHeight;
    public uint RemoteTextureWidth;
    public uint RemoteTextureHeight;
    public GameObject RemoteTexture;
    public GameObject LocalTexture;
}