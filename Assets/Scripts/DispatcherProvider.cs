
#if ENABLE_WINMD_SUPPORT
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ConversationLibrary.Interfaces;
using Windows.UI.Core;
using System.ComponentModel;

public class DispatcherProvider : IDispatcherProvider
{
    public CoreDispatcher Dispatcher
    {
        get => Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
        set => throw new NotImplementedException();
    }
    public event PropertyChangedEventHandler PropertyChanged;
}
#endif