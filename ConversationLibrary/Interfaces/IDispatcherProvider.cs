namespace ConversationLibrary.Interfaces
{
    using System.ComponentModel;
    using Windows.UI.Core;
    using Windows.UI.Xaml.Controls;

    public interface IDispatcherProvider : INotifyPropertyChanged
    {
        CoreDispatcher Dispatcher { get; set; }
    }
}
