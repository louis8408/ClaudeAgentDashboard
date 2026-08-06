using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClaudeAgentDashboard.Application.UseCases;
using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Domain.Ports;

namespace ClaudeAgentDashboard.Presentation.Views;

/// <summary>
/// User Story 5: the single main window — a virtual "desktop" surface. Replaces
/// <c>AgentListWindow</c> (User Story 1/2/3) and hosts <see cref="AgentDetailOverlay"/>
/// in place of the old separate <c>AgentActivityDetailView</c> window (User Story 4):
/// every agent is a freely-draggable <see cref="AgentCardView"/>, clicking one opens the
/// overlay over the same window, and closing the overlay returns to the card view — no
/// second window is ever created for the detail view.
/// </summary>
public partial class DesktopWindow : Window
{
    private const double CardColumnWidth = 150;
    private const double CardRowHeight = 112;
    private const int GridColumns = 6;
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(2);

    private readonly ShowAgentCommand? _showAgentCommand;
    private readonly DismissAgentCommand? _dismissAgentCommand;
    private readonly ViewAgentActivityQuery? _viewAgentActivityQuery;
    private readonly ViewAgentTranscriptQuery? _viewAgentTranscriptQuery;
    private readonly ISettingsStore? _settingsStore;
    private readonly IHookRegistrar? _hookRegistrar;
    private readonly Uri? _hookListenerBaseAddress;
    private readonly DispatcherTimer? _refreshTimer;
    private readonly Dictionary<Guid, AgentCardView> _cards = [];
    private int _nextDefaultSlot;
    private AgentDetailOverlay? _openOverlay;

    public DesktopWindow()
        : this(null, null, null, null, null, null, null, null)
    {
    }

    public DesktopWindow(
        OpenDashboardQuery? openDashboardQuery,
        ShowAgentCommand? showAgentCommand,
        DismissAgentCommand? dismissAgentCommand,
        ViewAgentActivityQuery? viewAgentActivityQuery,
        ViewAgentTranscriptQuery? viewAgentTranscriptQuery,
        ISettingsStore? settingsStore,
        IHookRegistrar? hookRegistrar,
        Uri? hookListenerBaseAddress)
    {
        _showAgentCommand = showAgentCommand;
        _dismissAgentCommand = dismissAgentCommand;
        _viewAgentActivityQuery = viewAgentActivityQuery;
        _viewAgentTranscriptQuery = viewAgentTranscriptQuery;
        _settingsStore = settingsStore;
        _hookRegistrar = hookRegistrar;
        _hookListenerBaseAddress = hookListenerBaseAddress;

        InitializeComponent();
        ApplyBackground(settingsStore?.BackgroundImagePath);
        Render(openDashboardQuery?.Execute() ?? []);

        if (openDashboardQuery is not null)
        {
            _refreshTimer = new DispatcherTimer { Interval = RefreshInterval };
            _refreshTimer.Tick += (_, _) => Render(openDashboardQuery.Execute());
            _refreshTimer.Start();
            Closed += (_, _) =>
            {
                _refreshTimer.Stop();
                _openOverlay?.StopRefreshing();
            };
        }
    }

    private void Render(IReadOnlyCollection<AgentSession> sessions)
    {
        var seenIds = new HashSet<Guid>();

        foreach (var session in sessions)
        {
            seenIds.Add(session.Id);

            if (_cards.TryGetValue(session.Id, out var existingCard))
            {
                existingCard.Bind(session);
                continue;
            }

            var card = new AgentCardView();
            card.Bind(session);
            card.Clicked += (_, _) => OpenOverlay(session);
            card.PositionChanged += (_, position) => _settingsStore?.SetCardPosition(session.Label, position);

            var position = _settingsStore?.GetCardPosition(session.Label) ?? NextDefaultPosition();
            Canvas.SetLeft(card, position.X);
            Canvas.SetTop(card, position.Y);

            CardCanvas.Children.Add(card);
            _cards[session.Id] = card;
        }

        foreach (var staleId in _cards.Keys.Where(id => !seenIds.Contains(id)).ToList())
        {
            CardCanvas.Children.Remove(_cards[staleId]);
            _cards.Remove(staleId);
        }

        EmptyStatePanel.IsVisible = _cards.Count == 0;
    }

    private CardPosition NextDefaultPosition()
    {
        var slot = _nextDefaultSlot++;
        var column = slot % GridColumns;
        var row = slot / GridColumns;
        return new CardPosition(20 + (column * CardColumnWidth), 20 + (row * CardRowHeight));
    }

    private void OpenOverlay(AgentSession session)
    {
        _openOverlay?.StopRefreshing();

        var overlay = new AgentDetailOverlay(
            session, _showAgentCommand, _dismissAgentCommand, _viewAgentActivityQuery, _viewAgentTranscriptQuery,
            _hookRegistrar, _hookListenerBaseAddress);
        overlay.CloseRequested += (_, _) => CloseOverlay();

        _openOverlay = overlay;
        OverlayHost.Content = overlay;
        OverlayScrim.IsVisible = true;
    }

    private void CloseOverlay()
    {
        _openOverlay?.StopRefreshing();
        _openOverlay = null;
        OverlayHost.Content = null;
        OverlayScrim.IsVisible = false;
    }

    private async void OnChooseBackgroundClicked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel?.StorageProvider is null)
        {
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a background image",
            AllowMultiple = false,
            FileTypeFilter = [FilePickerFileTypes.ImageAll],
        });

        var chosenPath = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (chosenPath is null)
        {
            return;
        }

        ApplyBackground(chosenPath);
        if (_settingsStore is not null)
        {
            _settingsStore.BackgroundImagePath = chosenPath;
        }
    }

    private void ApplyBackground(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            BackgroundImage.IsVisible = false;
            BackgroundImage.Source = null;
            return;
        }

        try
        {
            BackgroundImage.Source = new Bitmap(path);
            BackgroundImage.IsVisible = true;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or ArgumentException)
        {
            // Corrupt/unreadable file since it was selected — fall back to the default
            // background (R13) rather than failing to open the dashboard.
            BackgroundImage.IsVisible = false;
            BackgroundImage.Source = null;
        }
    }
}
