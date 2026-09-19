using System.Windows;
using System.Windows.Input;
using Countdown.App.Services;
using Countdown.Core;

namespace Countdown.App.Views;

public partial class CustomDurationDialog : Window
{
    public CustomDurationDialog()
    {
        InitializeComponent();
        WindowIconService.Apply(this);
        Loaded += (_, _) =>
        {
            InputBox.Focus();
            UpdateState();
        };
        // Deterministic Enter handling: the dialog is launched from a context menu,
        // where default-button routing can be flaky; also avoids the IME swallowing
        // the first Enter (IME is disabled on this box, but be explicit).
        InputBox.KeyDown += OnInputKeyDown;
    }

    /// <summary>Shows the dialog; returns the raw input, or null when cancelled/invalid.</summary>
    public static string? Prompt(Window? owner)
    {
        var dialog = new CustomDurationDialog();
        if (owner is not null)
        {
            dialog.Owner = owner;
        }

        return dialog.ShowDialog() == true ? dialog.InputBox.Text : null;
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            TryAccept();
        }
    }

    private void OnInputChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) => UpdateState();

    private void UpdateState()
    {
        if (OkButton is null)
        {
            return;
        }

        var valid = DurationParser.TryParse(InputBox?.Text, out _);
        OkButton.IsEnabled = valid;
        HintText.Text = valid
            ? "纯数字按分钟；可带 m / s 后缀，回车开始"
            : "格式无效，例如 25、5m、90s、1m30s";
        HintText.Foreground = valid
            ? System.Windows.Media.Brushes.DarkGray
            : System.Windows.Media.Brushes.LightCoral;
    }

    private void TryAccept()
    {
        if (DurationParser.TryParse(InputBox.Text, out _))
        {
            DialogResult = true;
        }
    }

    private void OnOk(object sender, RoutedEventArgs e) => TryAccept();
}
