using System.Globalization;
using System.Net;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;

namespace BikeMate.Views.Mechanic;

public abstract class MechanicPageBase : ContentPage
{
    protected static readonly Color Orange = Color.FromArgb("#FF6B2C");
    protected static readonly Color Dark = Color.FromArgb("#1F2933");
    protected static readonly Color Muted = Color.FromArgb("#6E6E6E");
    protected static readonly Color BorderColor = Color.FromArgb("#E6E6E6");
    protected static readonly Color PageColor = Color.FromArgb("#F6F6F6");
    protected static readonly Color SoftOrange = Color.FromArgb("#FFF2EA");
    protected static readonly Color SoftGreen = Color.FromArgb("#EAF8EF");
    protected static readonly Color SoftBlue = Color.FromArgb("#EEF6FF");

    protected MechanicPageBase()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = PageColor;
    }

    protected void SetPage(string title, string subtitle, View content, bool isLoading, Func<Task> refresh)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        root.Add(Header(title, subtitle, isLoading, refresh), 0, 0);
        root.Add(new ScrollView { Content = content }, 0, 1);
        Content = root;
    }

    protected View Header(string title, string subtitle, bool isLoading, Func<Task> refresh)
    {
        var grid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 18, 16, 12),
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var text = new VerticalStackLayout { Spacing = 4 };
        text.Add(Text(title, 22, Dark, FontAttributes.Bold));
        text.Add(Text(subtitle, 12, Muted));
        grid.Add(text, 0, 0);
        grid.Add(new Button
        {
            Text = isLoading ? "..." : "Refresh",
            IsEnabled = !isLoading,
            BackgroundColor = Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(14, 0),
            Command = new Command(async () => await refresh())
        }, 1, 0);
        grid.Add(new Button
        {
            Text = "Log out",
            BackgroundColor = Colors.Transparent,
            TextColor = Orange,
            BorderColor = BorderColor,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(12, 0),
            FontSize = 13,
            Command = new Command(async () => await AppNavigation.SignOutAsync())
        }, 2, 0);

        return grid;
    }

    protected static Label Text(string text, double size, Color color, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = text,
            FontSize = AppTypography.SizeFor(size),
            TextColor = color,
            FontAttributes = attributes,
            FontFamily = FontFor(size, attributes),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static string FontFor(double size, FontAttributes attributes = FontAttributes.None)
    {
        return AppTypography.FontFor(size, attributes);
    }

    protected static Border Card(View content, Color? background = null, Thickness? padding = null)
    {
        return new Border
        {
            Stroke = BorderColor,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background ?? Colors.White,
            Padding = padding ?? new Thickness(14),
            Content = content
        };
    }

    protected static Button PrimaryButton(string text, Func<Task> action, bool enabled = true)
    {
        return new Button
        {
            Text = text,
            IsEnabled = enabled,
            BackgroundColor = Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 44,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await action())
        };
    }

    protected static Button SecondaryButton(string text, Func<Task> action, bool enabled = true)
    {
        return new Button
        {
            Text = text,
            IsEnabled = enabled,
            BackgroundColor = Colors.White,
            TextColor = Dark,
            BorderColor = BorderColor,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 42,
            Command = new Command(async () => await action())
        };
    }

    protected static View Banner(string? text)
    {
        return string.IsNullOrWhiteSpace(text)
            ? new BoxView { HeightRequest = 0, Opacity = 0 }
            : Card(Text(text, 12, Muted), Colors.White);
    }

    protected static View Stat(string label, string value, Color background)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(Text(value, 18, Dark, FontAttributes.Bold));
        stack.Add(Text(label, 11, Muted));
        return Card(stack, background, new Thickness(12));
    }

    protected static View JobCard(ServiceRequestDto job, Func<int, Task>? details = null, Func<int, Task>? accept = null, Func<int, Task>? reject = null)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        var title = string.IsNullOrWhiteSpace(job.ServiceName) ? $"Request #{job.RequestId}" : job.ServiceName!;
        stack.Add(Text(title, 16, Dark, FontAttributes.Bold));
        stack.Add(Text($"{job.CustomerName} - {FormatStatus(job.CurrentStatus)}", 12, Muted));
        stack.Add(Text(string.IsNullOrWhiteSpace(job.ServiceLocationAddress) ? "No address provided" : job.ServiceLocationAddress!, 12, Dark));
        stack.Add(Text(job.IssueDescription, 12, Muted));
        stack.Add(Text(JobMeta(job), 11, Orange, FontAttributes.Bold));

        var buttons = new Grid { ColumnSpacing = 8 };
        buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        buttons.Add(SecondaryButton("Details", async () =>
        {
            if (details is not null)
            {
                await details(job.RequestId);
            }
            else
            {
                await Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={job.RequestId}");
            }
        }), 0, 0);

        if (accept is not null)
        {
            buttons.Add(PrimaryButton("Accept", async () => await accept(job.RequestId)), 1, 0);
        }

        if (reject is not null)
        {
            buttons.Add(SecondaryButton("Skip", async () => await reject(job.RequestId)), 2, 0);
        }

        stack.Add(buttons);
        return Card(stack);
    }

    protected static string FormatStatus(string status)
    {
        return string.Join(" ", status.Replace("_", " ", StringComparison.Ordinal).Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(x)));
    }

    protected static string Money(decimal amount)
    {
        return string.Create(CultureInfo.InvariantCulture, $"PHP {amount:N2}");
    }

    protected static string JobMeta(ServiceRequestDto job)
    {
        var total = job.FinalTotal > 0 ? job.FinalTotal : job.EstimatedTotal;
        var distance = job.DistanceKm is null ? "Set rider area for distance" : $"{job.DistanceKm:0.##} km away";
        var schedule = job.ScheduledAt?.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture) ?? "ASAP";
        return $"{distance} - {schedule} - {Money(total)}";
    }

    protected static HtmlWebViewSource MapSource(decimal latitude, decimal longitude)
    {
        var query = $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}";
        var key = GoogleMapsEmbedKey();
        var frameUrl = string.IsNullOrWhiteSpace(key)
            ? $"https://maps.google.com/maps?q={Uri.EscapeDataString(query)}&z=15&output=embed"
            : $"https://www.google.com/maps/embed/v1/place?key={Uri.EscapeDataString(key)}&q={Uri.EscapeDataString(query)}&zoom=15";
        var encodedUrl = WebUtility.HtmlEncode(frameUrl);
        return new HtmlWebViewSource
        {
            BaseUrl = "https://www.google.com",
            Html = $$"""
<!doctype html>
<html>
<head>
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <style>
    html, body, iframe { width: 100%; height: 100%; margin: 0; padding: 0; border: 0; overflow: hidden; background: #eef1f4; }
  </style>
</head>
<body>
  <iframe src="{{encodedUrl}}" loading="lazy" referrerpolicy="no-referrer-when-downgrade" allowfullscreen></iframe>
</body>
</html>
"""
        };
    }

    private static string GoogleMapsEmbedKey()
    {
#if ANDROID
        return Android.App.Application.Context.GetString(Resource.String.google_maps_embed_key) ?? string.Empty;
#else
        return string.Empty;
#endif
    }

    protected static async Task<(decimal Latitude, decimal Longitude, decimal? Accuracy, bool Fallback)> ResolveAreaAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(12)))
                    ?? await Geolocation.Default.GetLastKnownLocationAsync();
                if (location is not null)
                {
                    return ((decimal)location.Latitude, (decimal)location.Longitude, (decimal?)location.Accuracy, false);
                }
            }
        }
        catch
        {
        }

        return (14.599512m, 120.984222m, null, true);
    }

    protected async Task UseMyAreaAsync(Func<string, Task> afterUpdate)
    {
        var area = await ResolveAreaAsync();
        await RiderApiClient.UpdateLocationAsync(null, area.Latitude, area.Longitude, area.Accuracy);
        var message = area.Fallback
            ? "GPS was unavailable, so BikeMate saved the Metro Manila test area for matching."
            : "Your current area was saved. Incoming jobs are now filtered nearby first.";
        await afterUpdate(message);
    }
}

public sealed class MechanicDashboardPage : MechanicPageBase
{
    private RiderDashboardDto? _dashboard;
    private bool _isLoading;
    private string? _banner;

    public MechanicDashboardPage()
    {
        Title = "Mechanic Dashboard";
        Render("Loading rider dashboard...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing rider data...");
        try
        {
            _dashboard = await RiderApiClient.GetDashboardAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _banner = $"Could not load rider dashboard. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_dashboard is null)
        {
            body.Add(Card(Text(_isLoading ? "Loading rider profile..." : "No rider data yet.", 12, Muted)));
            SetPage("Rider", "Jobs around your saved area.", body, _isLoading, async () => await LoadAsync());
            return;
        }

        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Text(_dashboard.Profile.FullName, 20, Dark, FontAttributes.Bold),
                Text($"{FormatStatus(_dashboard.AvailabilityStatus)} - {_dashboard.AverageRating:0.0} rating - {_dashboard.TotalCompletedJobs} completed jobs", 12, Muted),
                Text("Tap Use my area before testing matching so incoming jobs are filtered near your emulator/device location.", 12, Muted)
            }
        }, SoftOrange));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(Stat("Nearby jobs", _dashboard.IncomingRequests.ToString(CultureInfo.InvariantCulture), SoftBlue), 0, 0);
        stats.Add(Stat("Emergency", _dashboard.EmergencyRequests.ToString(CultureInfo.InvariantCulture), SoftOrange), 1, 0);
        stats.Add(Stat("Earnings", Money(_dashboard.TotalEarnings), SoftGreen), 2, 0);
        body.Add(stats);

        var actions = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        actions.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        actions.Add(PrimaryButton("Go online", async () => { await RiderApiClient.GoOnlineAsync(); await LoadAsync("You are online and ready for nearby jobs."); }), 0, 0);
        actions.Add(SecondaryButton("Go offline", async () => { await RiderApiClient.GoOfflineAsync(); await LoadAsync("You are offline. New jobs are hidden until you go online."); }), 1, 0);
        actions.Add(SecondaryButton("Use my area", async () => await UseMyAreaAsync(LoadAsync)), 0, 1);
        actions.Add(PrimaryButton("View jobs", async () => await Shell.Current.GoToAsync("//MechanicJobsPage")), 1, 1);
        body.Add(actions);

        body.Add(Text("Current job", 14, Dark, FontAttributes.Bold));
        body.Add(_dashboard.ActiveJob is null
            ? Card(Text("No active job. Go online, save your area, then open Jobs to accept a nearby request.", 12, Muted))
            : JobCard(_dashboard.ActiveJob));

        SetPage("Rider", "Availability, matching area, and live job summary.", body, _isLoading, async () => await LoadAsync());
    }
}

public sealed class MechanicJobsPage : MechanicPageBase
{
    private IReadOnlyList<ServiceRequestDto> _jobs = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicJobsPage()
    {
        Title = "Jobs";
        Render("Loading nearby jobs...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Checking jobs near your saved area...");
        try
        {
            _jobs = await RiderApiClient.GetIncomingAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _jobs = [];
            _banner = $"Could not load nearby jobs. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Text("Nearby matching", 16, Dark, FontAttributes.Bold),
                Text("Incoming requests use your saved rider location and show jobs within about 8 km first.", 12, Muted)
            }
        }, SoftBlue));

        var actionRow = new Grid { ColumnSpacing = 8 };
        actionRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actionRow.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actionRow.Add(SecondaryButton("Use my area", async () => await UseMyAreaAsync(LoadAsync)), 0, 0);
        actionRow.Add(PrimaryButton("Current job", async () => await Shell.Current.GoToAsync(nameof(MechanicJobDetailsPage))), 1, 0);
        body.Add(actionRow);

        if (_isLoading)
        {
            body.Add(new ActivityIndicator { IsRunning = true, Color = Orange, HeightRequest = 42 });
        }

        if (_jobs.Count == 0 && !_isLoading)
        {
            body.Add(Card(Text("No nearby requests right now. Create a customer booking near your saved area, then refresh this page.", 12, Muted)));
        }
        else
        {
            foreach (var job in _jobs)
            {
                body.Add(JobCard(
                    job,
                    requestId => Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}"),
                    AcceptAsync,
                    RejectAsync));
            }
        }

        SetPage("Jobs", "Incoming requests near your rider area.", body, _isLoading, async () => await LoadAsync());
    }

    private async Task AcceptAsync(int requestId)
    {
        await RiderApiClient.AcceptAsync(requestId);
        await Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}");
    }

    private async Task RejectAsync(int requestId)
    {
        await RiderApiClient.RejectAsync(requestId);
        await LoadAsync($"Request #{requestId} was skipped.");
    }
}

public sealed class MechanicEmergencyRequestsPage : MechanicPageBase
{
    private IReadOnlyList<ServiceRequestDto> _jobs = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicEmergencyRequestsPage()
    {
        Title = "Emergency";
        Render("Loading emergency requests...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Checking emergency jobs near you...");
        try
        {
            _jobs = await RiderApiClient.GetEmergencyAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _jobs = [];
            _banner = $"Could not load emergency requests. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        body.Add(Card(Text("Emergency jobs use a wider nearby radius so responders can find high-priority roadside requests.", 12, Muted), SoftOrange));
        body.Add(SecondaryButton("Use my area", async () => await UseMyAreaAsync(LoadAsync)));

        if (_jobs.Count == 0 && !_isLoading)
        {
            body.Add(Card(Text("No emergency requests near your saved area.", 12, Muted)));
        }
        else
        {
            foreach (var job in _jobs)
            {
                body.Add(JobCard(job, requestId => Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}"), AcceptAsync));
            }
        }

        SetPage("Emergency", "Urgent nearby jobs for responders.", body, _isLoading, async () => await LoadAsync());
    }

    private async Task AcceptAsync(int requestId)
    {
        await RiderApiClient.AcceptAsync(requestId);
        await Shell.Current.GoToAsync($"{nameof(MechanicJobDetailsPage)}?requestId={requestId}");
    }
}

public sealed class MechanicJobDetailsPage : MechanicPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _job;
    private bool _isLoading;
    private string? _banner;

    public MechanicJobDetailsPage()
    {
        Title = "Current Job";
        Render("Loading job...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing job details...");
        try
        {
            _job = _requestId > 0
                ? await RiderApiClient.GetJobAsync(_requestId)
                : await RiderApiClient.GetCurrentJobAsync();
            _requestId = _job?.RequestId ?? _requestId;
            _banner = banner;
        }
        catch (Exception ex)
        {
            _job = null;
            _banner = $"Could not load job details. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_job is null)
        {
            body.Add(Card(Text(_isLoading ? "Loading current job..." : "No active job is assigned yet.", 12, Muted)));
            body.Add(PrimaryButton("Find nearby jobs", async () => await Shell.Current.GoToAsync("//MechanicJobsPage")));
            SetPage("Current Job", "Job details, status, and location.", body, _isLoading, async () => await LoadAsync());
            return;
        }

        body.Add(JobCard(_job));
        body.Add(PrimaryButton("Open job map", async () => await Shell.Current.GoToAsync($"{nameof(MechanicMapPage)}?requestId={_job.RequestId}")));

        var actions = new Grid { ColumnSpacing = 8, RowSpacing = 8 };
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        actions.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        actions.Add(SecondaryButton("On the way", async () => await UpdateStatusAsync("en_route")), 0, 0);
        actions.Add(SecondaryButton("Arrived", async () => await UpdateStatusAsync("arrived")), 1, 0);
        actions.Add(SecondaryButton("Start service", async () => await UpdateStatusAsync("in_progress")), 0, 1);
        actions.Add(PrimaryButton("Complete", async () => await UpdateStatusAsync("completed")), 1, 1);
        body.Add(actions);

        SetPage("Current Job", "Move the customer through real request statuses.", body, _isLoading, async () => await LoadAsync());
    }

    private async Task UpdateStatusAsync(string status)
    {
        if (_job is null)
        {
            return;
        }

        _job = await RiderApiClient.UpdateStatusAsync(_job.RequestId, status, $"Rider marked {status}.");
        await LoadAsync($"Request #{_job.RequestId} is now {FormatStatus(_job.CurrentStatus)}.");
    }
}

public sealed class MechanicMapPage : MechanicPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _job;
    private bool _isLoading;
    private string? _banner;

    public MechanicMapPage()
    {
        Title = "Job Location";
        Render("Loading map...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("requestId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing map...");
        try
        {
            _job = _requestId > 0
                ? await RiderApiClient.GetJobAsync(_requestId)
                : await RiderApiClient.GetCurrentJobAsync();
            _requestId = _job?.RequestId ?? _requestId;
            _banner = banner;
        }
        catch (Exception ex)
        {
            _job = null;
            _banner = $"Could not load job location. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_job?.ServiceLatitude is null || _job.ServiceLongitude is null)
        {
            body.Add(Card(Text("This job has no saved coordinates yet. Ask the customer to use current location during booking.", 12, Muted)));
            SetPage("Job Location", "Live map for the selected request.", body, _isLoading, async () => await LoadAsync());
            return;
        }

        body.Add(Card(new Grid
        {
            HeightRequest = 360,
            Children =
            {
                new WebView { Source = MapSource(_job.ServiceLatitude.Value, _job.ServiceLongitude.Value), HeightRequest = 360 }
            }
        }, Colors.White, new Thickness(0)));
        body.Add(Card(Text(_job.ServiceLocationAddress ?? "Customer location", 12, Dark)));
        body.Add(PrimaryButton("Open in Google Maps", async () =>
        {
            var query = Uri.EscapeDataString($"{_job.ServiceLatitude.Value.ToString(CultureInfo.InvariantCulture)},{_job.ServiceLongitude.Value.ToString(CultureInfo.InvariantCulture)}");
            await Launcher.Default.OpenAsync(new Uri($"https://www.google.com/maps/search/?api=1&query={query}"));
        }));

        SetPage("Job Location", "Customer request location from live booking data.", body, _isLoading, async () => await LoadAsync());
    }
}

public sealed class MechanicMessagesPage : MechanicPageBase
{
    private IReadOnlyList<ConversationSummaryDto> _conversations = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicMessagesPage()
    {
        Title = "Messages";
        Render("Loading conversations...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing messages...");
        try
        {
            _conversations = await RiderApiClient.GetConversationsAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _conversations = [];
            _banner = $"Could not load messages. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));

        if (_conversations.Count == 0 && !_isLoading)
        {
            body.Add(Card(Text("No conversations yet. Customer chats will appear after a request is connected.", 12, Muted)));
        }
        else
        {
            foreach (var conversation in _conversations.OrderByDescending(x => x.LastMessageAt ?? DateTime.MinValue))
            {
                var stack = new VerticalStackLayout { Spacing = 6 };
                stack.Add(Text(conversation.Title, 15, Dark, FontAttributes.Bold));
                stack.Add(Text(conversation.Subtitle ?? conversation.LastMessageText ?? "No recent message", 12, Muted));
                stack.Add(Text((conversation.LastMessageAt?.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture) ?? "New conversation"), 11, Orange));
                body.Add(Card(stack));
            }
        }

        SetPage("Messages", "Job and customer conversations.", body, _isLoading, async () => await LoadAsync());
    }
}

public sealed class MechanicHistoryPage : MechanicPageBase
{
    private IReadOnlyList<ServiceRequestDto> _jobs = [];
    private bool _isLoading;
    private string? _banner;

    public MechanicHistoryPage()
    {
        Title = "History";
        Render("Loading history...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing completed jobs...");
        try
        {
            _jobs = await RiderApiClient.GetHistoryAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _jobs = [];
            _banner = $"Could not load history. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        if (_jobs.Count == 0 && !_isLoading)
        {
            body.Add(Card(Text("Completed, cancelled, and rejected jobs will appear here.", 12, Muted)));
        }
        else
        {
            foreach (var job in _jobs)
            {
                body.Add(JobCard(job));
            }
        }

        SetPage("History", "Past rider jobs and outcomes.", body, _isLoading, async () => await LoadAsync());
    }
}

public sealed class MechanicProfilePage : MechanicPageBase
{
    private RiderDashboardDto? _dashboard;
    private bool _isLoading;
    private string? _banner;

    public MechanicProfilePage()
    {
        Title = "Profile";
        Render("Loading profile...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        _isLoading = true;
        Render(banner ?? "Refreshing profile...");
        try
        {
            _dashboard = await RiderApiClient.GetDashboardAsync();
            _banner = banner;
        }
        catch (Exception ex)
        {
            _dashboard = null;
            _banner = $"Could not load profile. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        body.Add(Banner(banner));
        if (_dashboard is null)
        {
            body.Add(Card(Text("No profile data yet.", 12, Muted)));
            SetPage("Profile", "Rider verification and performance.", body, _isLoading, async () => await LoadAsync());
            return;
        }

        var profile = _dashboard.Profile;
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Text(profile.FullName, 20, Dark, FontAttributes.Bold),
                Text(profile.IsVerified ? "Verified rider" : "Verification pending", 12, Orange, FontAttributes.Bold),
                Text(profile.Bio ?? "No bio yet.", 12, Muted)
            }
        }, SoftOrange));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(Stat("Rating", profile.AverageRating.ToString("0.0", CultureInfo.InvariantCulture), SoftBlue), 0, 0);
        stats.Add(Stat("Completed", profile.TotalCompletedJobs.ToString(CultureInfo.InvariantCulture), SoftGreen), 1, 0);
        body.Add(stats);
        body.Add(Card(Text($"Experience: {(profile.YearsExperience?.ToString(CultureInfo.InvariantCulture) ?? "0")} year(s)\nStatus: {FormatStatus(profile.AvailabilityStatus)}", 12, Dark)));
        body.Add(SecondaryButton("Use my area", async () => await UseMyAreaAsync(LoadAsync)));

        SetPage("Profile", "Rider verification, rating, and matching area.", body, _isLoading, async () => await LoadAsync());
    }
}
