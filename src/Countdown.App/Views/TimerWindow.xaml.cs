using System.Windows;
using System.Windows.Input;

namespace Countdown.App.Views;

public partial class TimerWindow : Window
{
    public TimerWindow()
    {
        InitializeComponent();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }
}
