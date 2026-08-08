using Avalonia.Controls;
using Avalonia.Interactivity;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;
using ClaudeAgentDashboard.Presentation.Theming;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// The dashboard's settings surface: which attention reasons raise a notification, the visual
/// theme, and startup/window behavior. Every control writes straight to
/// <see cref="ISettingsStore"/> on change — no separate Save step, matching the app's existing
/// immediate-persist convention (e.g. the summary panel's collapsed state).
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly ISettingsStore? _settingsStore;
    private readonly ILoginItemRegistrar? _loginItemRegistrar;

    public SettingsWindow()
        : this(null, null)
    {
    }

    public SettingsWindow(ISettingsStore? settingsStore, ILoginItemRegistrar? loginItemRegistrar)
    {
        _settingsStore = settingsStore;
        _loginItemRegistrar = loginItemRegistrar;

        InitializeComponent();
        LoadCurrentValues();
    }

    private void LoadCurrentValues()
    {
        NotifyOnIdleCheckBox.IsChecked = _settingsStore?.NotifyOnIdle ?? true;
        NotifyOnWaitingForInputCheckBox.IsChecked = _settingsStore?.NotifyOnWaitingForInput ?? true;
        NotifyOnEndedCheckBox.IsChecked = _settingsStore?.NotifyOnEnded ?? true;

        var theme = _settingsStore?.Theme ?? AppTheme.Dark;
        DarkThemeRadioButton.IsChecked = theme == AppTheme.Dark;
        LightThemeRadioButton.IsChecked = theme == AppTheme.Light;

        LaunchAtLoginCheckBox.IsChecked = _settingsStore?.LaunchAtLoginEnabled ?? false;
        MinimizeToTrayCheckBox.IsChecked = _settingsStore?.MinimizeToTrayOnClose ?? true;
    }

    private void OnNotifyOnIdleClicked(object? sender, RoutedEventArgs e)
    {
        if (_settingsStore is not null)
        {
            _settingsStore.NotifyOnIdle = NotifyOnIdleCheckBox.IsChecked ?? true;
        }
    }

    private void OnNotifyOnWaitingForInputClicked(object? sender, RoutedEventArgs e)
    {
        if (_settingsStore is not null)
        {
            _settingsStore.NotifyOnWaitingForInput = NotifyOnWaitingForInputCheckBox.IsChecked ?? true;
        }
    }

    private void OnNotifyOnEndedClicked(object? sender, RoutedEventArgs e)
    {
        if (_settingsStore is not null)
        {
            _settingsStore.NotifyOnEnded = NotifyOnEndedCheckBox.IsChecked ?? true;
        }
    }

    private void OnThemeChanged(object? sender, RoutedEventArgs e)
    {
        var theme = LightThemeRadioButton.IsChecked == true ? AppTheme.Light : AppTheme.Dark;
        if (_settingsStore is not null)
        {
            _settingsStore.Theme = theme;
        }

        ThemeResources.Apply(theme);
    }

    private void OnLaunchAtLoginClicked(object? sender, RoutedEventArgs e)
    {
        var enabled = LaunchAtLoginCheckBox.IsChecked ?? false;
        if (_settingsStore is not null)
        {
            _settingsStore.LaunchAtLoginEnabled = enabled;
        }

        _loginItemRegistrar?.SetEnabled(enabled);
    }

    private void OnMinimizeToTrayClicked(object? sender, RoutedEventArgs e)
    {
        if (_settingsStore is not null)
        {
            _settingsStore.MinimizeToTrayOnClose = MinimizeToTrayCheckBox.IsChecked ?? true;
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
