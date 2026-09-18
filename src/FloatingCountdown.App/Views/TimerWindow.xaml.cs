using System.Windows;
using System.Windows.Input;

namespace FloatingCountdown.App.Views;

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
