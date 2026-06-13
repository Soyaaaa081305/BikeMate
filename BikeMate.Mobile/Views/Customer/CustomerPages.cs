using System.Globalization;
using System.Windows.Input;
using BikeMate.Core.DTOs;
using BikeMate.Services;
using BikeMate.Views.Customer.Emergency;
using Microsoft.Maui.Controls.Shapes;

namespace BikeMate.Views.Customer;

internal static class CustomerUi
{
    public static readonly Color Orange = Color.FromArgb("#FF6B2C");
    public static readonly Color LightOrange = Color.FromArgb("#FFE1D2");
    public static readonly Color Dark = Color.FromArgb("#242424");
    public static readonly Color Muted = Color.FromArgb("#6E6E6E");
    public static readonly Color Border = Color.FromArgb("#E6E6E6");
    public static readonly Color Page = Color.FromArgb("#F6F6F6");

    public const string FontBody = "PublicSans";
    public const string FontDisplay = "Inter";
    public const string FontCaption = "PTSansCaption";
    public const string FontCaptionBold = "PTSansCaptionBold";

    public const string OnlineBikeRepairImage = "https://img.icons8.com/color/96/bicycle.png";
    public const string HomeIcon = "https://img.icons8.com/ios/50/home--v1.png";
    public const string ScheduleIcon = "https://img.icons8.com/ios/50/calendar--v1.png";
    public const string PaymentsIcon = "https://img.icons8.com/ios/50/wallet--v1.png";
    public const string MessagesIcon = "https://img.icons8.com/ios/50/speech-bubble--v1.png";

    public static string FontFor(double size, FontAttributes attributes = FontAttributes.None)
    {
        if (size <= 11)
        {
            return (attributes & FontAttributes.Bold) != 0 ? FontCaptionBold : FontCaption;
        }

        return size >= 16 || (attributes & FontAttributes.Bold) != 0 ? FontDisplay : FontBody;
    }
}

public abstract class CustomerPageBase : ContentPage
{
    protected CustomerPageBase()
    {
        Shell.SetNavBarIsVisible(this, false);
        Shell.SetTabBarIsVisible(this, false);
        BackgroundColor = Colors.White;
    }

    protected void SetScaffold(View body, string activeTab, bool showBottomBar = true)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(showBottomBar ? GridLength.Auto : new GridLength(0))
            }
        };

        root.Add(body, 0, 0);
        if (showBottomBar)
        {
            root.Add(BottomBar(activeTab), 0, 1);
        }

        Content = root;
    }

    protected static Label Label(string text, double size, Color color, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = text,
            FontSize = size,
            TextColor = color,
            FontAttributes = attributes,
            FontFamily = CustomerUi.FontFor(size, attributes),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    protected static Border Card(View content, Color? background = null, double radius = 8, Thickness? padding = null)
    {
        return new Border
        {
            BackgroundColor = background ?? Colors.White,
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            Padding = padding ?? new Thickness(14),
            Content = content
        };
    }

    protected static Button OrangeButton(string text, ICommand? command = null)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 48,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay
        };
    }

    protected static Button GhostButton(string text, ICommand? command = null)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            BorderColor = CustomerUi.Border,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 42,
            FontFamily = CustomerUi.FontBody
        };
    }

    protected static View Header(string title, bool back = true, string? right = null, ICommand? rightCommand = null)
    {
        var grid = new Grid
        {
            Padding = new Thickness(16, 18, 16, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Button
        {
            Text = back ? "Back" : string.Empty,
            Command = back ? new Command(async () => await Shell.Current.GoToAsync("..")) : null,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 12,
            WidthRequest = 56,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = new Thickness(0)
        }, 0, 0);

        grid.Add(new Label
        {
            Text = title,
            FontSize = 15,
            FontFamily = CustomerUi.FontDisplay,
            TextColor = CustomerUi.Dark,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 1, 0);

        if (!string.IsNullOrWhiteSpace(right))
        {
            var rightWidth = right.Length > 5 ? 106 : 76;
            grid.Add(new Button
            {
                Text = right,
                Command = rightCommand,
                BackgroundColor = Colors.Transparent,
                TextColor = CustomerUi.Orange,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = rightWidth,
                FontSize = 12,
                FontFamily = CustomerUi.FontDisplay
            }, 2, 0);
        }

        return grid;
    }

    protected static View Avatar(string text, double size = 48, Color? background = null)
    {
        return new Border
        {
            WidthRequest = size,
            HeightRequest = size,
            StrokeShape = new RoundRectangle { CornerRadius = size / 2 },
            BackgroundColor = background ?? Color.FromArgb("#F1F1F1"),
            Stroke = Colors.Transparent,
            Content = new Label
            {
                Text = text,
                TextColor = CustomerUi.Dark,
                FontSize = size / 3,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    protected static View Separator()
    {
        return new BoxView
        {
            HeightRequest = 1,
            BackgroundColor = CustomerUi.Border,
            HorizontalOptions = LayoutOptions.Fill
        };
    }

    protected static View Row(string left, string right = "", ICommand? command = null)
    {
        var grid = new Grid
        {
            Padding = new Thickness(0, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(Label(left, 13, CustomerUi.Dark), 0, 0);
        grid.Add(Label(right, 13, CustomerUi.Muted), 1, 0);

        if (command is not null)
        {
            grid.GestureRecognizers.Add(new TapGestureRecognizer { Command = command });
        }

        return grid;
    }

    private static View BottomBar(string activeTab)
    {
        var grid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(8, 6, 8, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };

        AddNav(grid, 0, "Home", CustomerUi.HomeIcon, "CustomerHomePage", activeTab == "Home");
        AddNav(grid, 1, "Schedule", CustomerUi.ScheduleIcon, "CustomerSchedulePage", activeTab == "Schedule");
        AddNav(grid, 2, "Payments", CustomerUi.PaymentsIcon, "CustomerPaymentsPage", activeTab == "Payments");
        AddNav(grid, 3, "Messages", CustomerUi.MessagesIcon, "CustomerMessagesPage", activeTab == "Messages");

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static void AddNav(Grid grid, int column, string text, string iconUrl, string route, bool active)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 2,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = ImageSource.FromUri(new Uri(iconUrl)), WidthRequest = 22, HeightRequest = 22, Opacity = active ? 1 : 0.70 },
                new Label
                {
                    Text = text,
                    TextColor = active ? CustomerUi.Orange : CustomerUi.Dark,
                    FontSize = 11,
                    FontAttributes = active ? FontAttributes.Bold : FontAttributes.None,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            }
        };
        stack.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync($"//{route}"))
        });

        grid.Add(stack, column, 0);
    }

    protected static string Money(decimal amount)
    {
        return string.Format(CultureInfo.GetCultureInfo("en-PH"), "PHP {0:N0}", amount);
    }

    protected static string FormatStatus(string status)
    {
        return string.IsNullOrWhiteSpace(status)
            ? "Unknown"
            : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(status.Replace("_", " "));
    }

    protected static string Initials(string? first, string? last = null)
    {
        var text = $"{first} {last}".Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return "B";
        }

        return text[..1].ToUpperInvariant();
    }
}

public sealed class CustomerHomePage : CustomerPageBase
{
    private CustomerMeDto? _customer;
    private IReadOnlyList<ServiceRequestDto> _requests = [];

    public CustomerHomePage()
    {
        Title = "Home";
        Render("Loading your customer dashboard...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            _requests = await CustomerApiClient.GetMyRequestsAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load live customer data. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var content = new VerticalStackLayout { Spacing = 0, BackgroundColor = CustomerUi.Page };
        content.Add(BuildHero(_customer));
        content.Add(BuildRepairCard());
        content.Add(BuildHomeBody(_requests, banner));

        SetScaffold(new ScrollView { Content = content }, "Home");
    }

    private View BuildHero(CustomerMeDto? customer)
    {
        var grid = new Grid
        {
            Padding = new Thickness(24, 34, 24, 70),
            BackgroundColor = Color.FromArgb("#FF7D4D"),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var profile = Avatar(Initials(customer?.FirstName, customer?.LastName), 46, Colors.White);
        profile.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerProfilePage)))
        });

        grid.Add(profile, 0, 0);
        grid.Add(new Label
        {
            Text = $"Welcome, {customer?.FirstName ?? "user"}",
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            VerticalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0)
        }, 1, 0);

        grid.Add(new Button
        {
            Text = "Help",
            TextColor = Colors.White,
            BackgroundColor = Colors.Transparent,
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerHelpDeskPage)))
        }, 2, 0);

        return grid;
    }

    private static View BuildRepairCard()
    {
        var card = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        var left = new VerticalStackLayout { Spacing = 8 };
        left.Add(Label("Book your repair now", 14, Colors.White, FontAttributes.Bold));
        left.Add(Label("Anything you need?", 11, Color.FromArgb("#F1F1F1")));
        left.Add(new Button
        {
            Text = "Book now!",
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            FontSize = 11,
            HeightRequest = 32,
            CornerRadius = 14,
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(BookServicePage)))
        });

        card.Add(left, 0, 0);
        card.Add(new Image
        {
            Source = ImageSource.FromUri(new Uri(CustomerUi.OnlineBikeRepairImage)),
            HeightRequest = 74,
            WidthRequest = 74,
            Aspect = Aspect.AspectFill
        }, 1, 0);

        return new Border
        {
            Margin = new Thickness(24, -48, 24, 18),
            Padding = new Thickness(20, 18),
            Stroke = Color.FromArgb("#555555"),
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            BackgroundColor = Color.FromArgb("#5D5D5D"),
            Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.20f, Radius = 8, Offset = new Point(0, 4) },
            Content = card
        };
    }

    private View BuildHomeBody(IReadOnlyList<ServiceRequestDto> requests, string? banner)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(18, 0, 18, 20), Spacing = 18 };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Label("Homepage", 14, CustomerUi.Dark, FontAttributes.Bold));

        var scroller = new ScrollView { Orientation = ScrollOrientation.Horizontal };
        var cards = new HorizontalStackLayout { Spacing = 10 };
        cards.Add(HomeShortcut("Scheduled", CustomerUi.ScheduleIcon, new Command(async () => await Shell.Current.GoToAsync("//CustomerSchedulePage"))));
        cards.Add(HomeShortcut("Payments", CustomerUi.PaymentsIcon, new Command(async () => await Shell.Current.GoToAsync("//CustomerPaymentsPage"))));
        cards.Add(HomeShortcut("Messages", CustomerUi.MessagesIcon, new Command(async () => await Shell.Current.GoToAsync("//CustomerMessagesPage"))));
        cards.Add(HomeShortcut("Alerts", CustomerUi.HomeIcon, new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerNotificationsPage)))));
        cards.Add(HomeShortcut("Help Desk", CustomerUi.HomeIcon, new Command(async () => await Shell.Current.GoToAsync(nameof(CustomerHelpDeskPage)))));
        scroller.Content = cards;
        body.Add(scroller);

        body.Add(Label("Booked Repairs", 14, CustomerUi.Dark, FontAttributes.Bold));
        var visibleRequests = requests.OrderByDescending(x => x.CreatedAt).Take(3).ToArray();
        if (visibleRequests.Length == 0)
        {
            body.Add(Card(Label("No bookings yet. Tap Book now to create your first repair request.", 12, CustomerUi.Muted)));
        }
        else
        {
            foreach (var request in visibleRequests)
            {
                body.Add(RepairRow(request));
            }
        }

        body.Add(new Button
        {
            Text = "EMERGENCY CALL",
            BackgroundColor = Color.FromArgb("#FF626A"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 46,
            Command = new Command(async () => await Shell.Current.GoToAsync(nameof(EmergencySosPage)))
        });

        return body;
    }

    private static View HomeShortcut(string title, string iconUrl, ICommand command)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.Center,
            Children =
            {
                new Image { Source = ImageSource.FromUri(new Uri(iconUrl)), HeightRequest = 38, WidthRequest = 38, Aspect = Aspect.AspectFit },
                new Label { Text = title, FontSize = 11, TextColor = CustomerUi.Muted, HorizontalTextAlignment = TextAlignment.Center }
            }
        };

        var border = Card(stack, Colors.White, 10, new Thickness(14));
        border.WidthRequest = 108;
        border.HeightRequest = 112;
        border.GestureRecognizers.Add(new TapGestureRecognizer { Command = command });
        return border;
    }

    private static View RepairRow(ServiceRequestDto request)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Image { Source = ImageSource.FromUri(new Uri(CustomerUi.OnlineBikeRepairImage)), WidthRequest = 42, HeightRequest = 42, Aspect = Aspect.AspectFill }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(10, 0, 0, 0) };
        text.Add(Label(request.ServiceName ?? "Bike repair", 13, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(request.MechanicName ?? request.ShopName ?? "Waiting for assignment", 11, CustomerUi.Muted));
        text.Add(Label(Money(request.FinalTotal > 0 ? request.FinalTotal : request.EstimatedTotal), 11, CustomerUi.Dark));
        grid.Add(text, 1, 0);

        grid.Add(Label(FormatStatus(request.CurrentStatus), 11, CustomerUi.Orange), 2, 0);

        var border = Card(grid, Colors.White, 8, new Thickness(12));
        border.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(BookingDetailsPage)}?requestId={request.RequestId}"))
        });
        return border;
    }
}

public sealed class CustomerNotificationsPage : CustomerPageBase
{
    private IReadOnlyList<NotificationDto> _notifications = [];
    private string? _banner;
    private bool _isLoading;

    public CustomerNotificationsPage()
    {
        Title = "Notifications";
        Render("Loading notifications...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        Render("Refreshing notifications...");
        try
        {
            _notifications = await CustomerApiClient.GetNotificationsAsync();
            _banner = null;
        }
        catch (Exception ex)
        {
            _banner = $"Connect the API to load notifications. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16, 8, 16, 20), Spacing = 12 };
        body.Add(Header("Notifications"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(GhostButton(_isLoading ? "Refreshing..." : "Refresh status", new Command(async () => await LoadAsync())));

        if (_notifications.Count == 0 && !_isLoading)
        {
            body.Add(Card(Label("No notifications yet. Booking, payment, chat, and emergency alerts will appear here.", 12, CustomerUi.Muted)));
        }
        else
        {
            foreach (var notification in _notifications.OrderByDescending(x => x.CreatedAt))
            {
                body.Add(NotificationRow(notification));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View NotificationRow(NotificationDto notification)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Add(new Label
        {
            Text = notification.Title,
            TextColor = CustomerUi.Dark,
            FontAttributes = notification.IsRead ? FontAttributes.None : FontAttributes.Bold,
            FontSize = 14
        });
        stack.Add(Label(notification.Message, 12, CustomerUi.Muted));
        stack.Add(Label($"{FormatStatus(notification.NotificationType ?? "notification")} - {notification.CreatedAt.ToLocalTime():MMM d, h:mm tt}", 10, CustomerUi.Orange));

        var card = Card(stack, notification.IsRead ? Colors.White : Color.FromArgb("#FFF4EF"), 8, new Thickness(14));
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () =>
            {
                if (!notification.IsRead)
                {
                    await CustomerApiClient.MarkNotificationReadAsync(notification.NotificationId);
                    await LoadAsync();
                }
            })
        });
        return card;
    }
}

public sealed class CustomerProfilePage : CustomerPageBase
{
    private CustomerMeDto? _customer;

    public CustomerProfilePage()
    {
        Title = "Account Details";
        Render("Loading account details...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load profile data. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 20),
            Spacing = 12
        };

        body.Add(Header("Account Details"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Avatar(Initials(_customer?.FirstName, _customer?.LastName), 108, Color.FromArgb("#B5B5B5")));
        body.Add(new Label
        {
            Text = "Personal Info",
            TextColor = Color.FromArgb("#276EF1"),
            FontSize = 12,
            TextDecorations = TextDecorations.Underline,
            HorizontalTextAlignment = TextAlignment.Center
        });

        var fullName = _customer is null ? "Loading..." : $"{_customer.FirstName} {_customer.LastName}";
        body.Add(Row("Full Name", $"{fullName} >", new Command(async () => await EditNameAsync())));
        body.Add(Separator());
        body.Add(Row("Account ID", _customer is null ? "" : $"CUST-{_customer.ClientId:0000}"));
        body.Add(Separator());
        body.Add(Row("Change Password", ">", new Command(async () => await DisplayAlertAsync("Change Password", "Use Forgot Password on the sign-in screen to reset your password.", "OK"))));
        body.Add(Separator());

        var quick = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        quick.Add(Label("Enable Quick\nLogin", 13, CustomerUi.Dark), 0, 0);
        quick.Add(new Switch { IsToggled = false }, 1, 0);
        body.Add(quick);

        body.Add(new BoxView { HeightRequest = 36, Opacity = 0 });
        body.Add(Row("Change Mobile\nNo.", string.IsNullOrWhiteSpace(_customer?.PhoneNumber) ? "Not set >" : $"{_customer.PhoneNumber} >", new Command(async () => await EditPhoneAsync())));
        body.Add(Separator());
        body.Add(Row("Change Email", _customer is null ? "" : $"{_customer.Email} >", new Command(async () => await EditEmailAsync())));
        body.Add(Separator());
        body.Add(Row("Default Address", $"{_customer?.Addresses.FirstOrDefault(x => x.IsDefault)?.AddressLine ?? "Not set"} >", new Command(async () => await EditAddressAsync())));
        body.Add(Separator());
        var motorcycle = _customer?.Motorcycles.FirstOrDefault();
        body.Add(Row("Motorcycle", motorcycle is null ? "Not set >" : $"{motorcycle.Brand} {motorcycle.Model} >", new Command(async () => await EditMotorcycleAsync())));

        body.Add(new Button
        {
            Text = "Log out",
            BackgroundColor = Color.FromArgb("#DD3838"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 48,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await BikeMate.Helpers.AppNavigation.SignOutAsync())
        });

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task RefreshProfileAsync()
    {
        _customer = await CustomerApiClient.GetCustomerAsync();
        Render();
    }

    private async Task SaveProfileAsync(string firstName, string lastName, string email, string? phone)
    {
        if (_customer is null)
        {
            return;
        }

        await CustomerApiClient.UpdateCustomerAsync(new UpsertCustomerProfileDto(firstName, lastName, email, phone));
        await RefreshProfileAsync();
    }

    private async Task EditNameAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var value = await DisplayPromptAsync("Full Name", "Enter your full name.", "Save", "Cancel", initialValue: $"{_customer.FirstName} {_customer.LastName}");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var parts = value.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        try
        {
            await SaveProfileAsync(parts[0], parts.Length > 1 ? parts[1] : _customer.LastName, _customer.Email, _customer.PhoneNumber);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Profile not saved", ex.Message, "OK");
        }
    }

    private async Task EditPhoneAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var value = await DisplayPromptAsync("Mobile Number", "Enter your mobile number.", "Save", "Cancel", keyboard: Keyboard.Telephone, initialValue: _customer.PhoneNumber ?? "");
        if (value is null)
        {
            return;
        }

        try
        {
            await SaveProfileAsync(_customer.FirstName, _customer.LastName, _customer.Email, string.IsNullOrWhiteSpace(value) ? null : value.Trim());
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Profile not saved", ex.Message, "OK");
        }
    }

    private async Task EditEmailAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var value = await DisplayPromptAsync("Email", "Enter your email address.", "Save", "Cancel", keyboard: Keyboard.Email, initialValue: _customer.Email);
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            await SaveProfileAsync(_customer.FirstName, _customer.LastName, value.Trim(), _customer.PhoneNumber);
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Profile not saved", ex.Message, "OK");
        }
    }

    private async Task EditAddressAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var existing = _customer.Addresses.FirstOrDefault(x => x.IsDefault) ?? _customer.Addresses.FirstOrDefault();
        var value = await DisplayPromptAsync("Default Address", "Enter your service address.", "Save", "Cancel", initialValue: existing?.AddressLine ?? "");
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            await CustomerApiClient.UpsertAddressAsync(existing, new UpsertCustomerAddressDto(
                existing?.Label ?? "Home",
                value.Trim(),
                existing?.City,
                existing?.Province,
                null,
                existing?.Latitude,
                existing?.Longitude,
                true));
            await RefreshProfileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Address not saved", ex.Message, "OK");
        }
    }

    private async Task EditMotorcycleAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var existing = _customer.Motorcycles.FirstOrDefault();
        var brand = await DisplayPromptAsync("Bike Brand", "Enter bike brand.", "Next", "Cancel", initialValue: existing?.Brand ?? "");
        if (string.IsNullOrWhiteSpace(brand))
        {
            return;
        }

        var model = await DisplayPromptAsync("Bike Model", "Enter bike model.", "Save", "Cancel", initialValue: existing?.Model ?? "");
        if (string.IsNullOrWhiteSpace(model))
        {
            return;
        }

        try
        {
            await CustomerApiClient.UpsertMotorcycleAsync(existing, new UpsertMotorcycleDto(
                brand.Trim(),
                model.Trim(),
                existing?.YearModel,
                existing?.PlateNumber,
                existing?.EngineType,
                existing?.Color,
                null));
            await RefreshProfileAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Motorcycle not saved", ex.Message, "OK");
        }
    }
}

public sealed class CustomerHelpDeskPage : CustomerPageBase
{
    public CustomerHelpDeskPage()
    {
        Title = "Help Desk";

        var body = new VerticalStackLayout { Padding = new Thickness(16, 8, 16, 20), Spacing = 14 };
        body.Add(Header("Help Desk"));
        body.Add(Label("What is BikeMate?", 21, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(Label("BikeMate is an on-demand bike repair service that sends expert mechanics straight to your doorstep. Book repairs, message trusted shops, track jobs, and settle payments quickly and reliably.", 13, CustomerUi.Dark));

        var searchGrid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        var searchLabel = Label("Search", 13, CustomerUi.Dark);
        var searchEntry = new Entry { Placeholder = "Search Help", FontSize = 12, Margin = new Thickness(8, -10, 0, -10) };
        Grid.SetColumn(searchEntry, 1);
        searchGrid.Children.Add(searchLabel);
        searchGrid.Children.Add(searchEntry);
        body.Add(Card(searchGrid, Colors.White, 18, new Thickness(12, 4)));

        body.Add(Label("FAQ", 13, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(FaqRow("How do I book a repair?", "Simply open the app, describe your issue, select a time, and confirm your booking."));
        body.Add(FaqRow("How much do repair costs?", "The estimate appears before confirmation. Final costs are shown in Payment Details."));
        body.Add(FaqRow("Is there a call-out or diagnostic fee?", "Some emergency bookings may include a call-out fee."));

        body.Add(new Label
        {
            Text = "Still stuck? Help us a mail away",
            FontAttributes = FontAttributes.Bold,
            TextColor = CustomerUi.Dark,
            FontSize = 15,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 18, 0, 0)
        });
        body.Add(OrangeButton("Send a message", new Command(async () => await Shell.Current.GoToAsync("//CustomerMessagesPage"))));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View FaqRow(string question, string answer)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        row.Add(Label(question, 13, CustomerUi.Dark), 0, 0);
        row.Add(Label("+", 18, CustomerUi.Muted), 1, 0);
        stack.Add(row);
        stack.Add(Label(answer, 11, CustomerUi.Muted));
        stack.Add(Separator());
        return stack;
    }
}

public sealed class CustomerSchedulePage : CustomerPageBase
{
    private IReadOnlyList<ServiceRequestDto> _requests = [];

    public CustomerSchedulePage()
    {
        Title = "Schedule";
        Render("Loading booked repairs...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _requests = await CustomerApiClient.GetMyRequestsAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load booked repairs. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 14 };
        body.Add(Header("Booked Repairs", false));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Tabs("Upcoming", "History"));
        if (_requests.Count == 0)
        {
            body.Add(Card(Label("No booked repairs yet.", 12, CustomerUi.Muted)));
        }
        else
        {
            foreach (var request in _requests.OrderByDescending(x => x.CreatedAt))
            {
                body.Add(ScheduleCard(request));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Schedule");
    }

    private static View Tabs(string active, string other)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(new Button { Text = active, BackgroundColor = CustomerUi.Orange, TextColor = Colors.White, CornerRadius = 0 }, 0, 0);
        grid.Add(new Button { Text = other, BackgroundColor = Colors.White, TextColor = CustomerUi.Dark, CornerRadius = 0 }, 1, 0);
        return grid;
    }

    private static View ScheduleCard(ServiceRequestDto request)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new Image { Source = ImageSource.FromUri(new Uri(CustomerUi.OnlineBikeRepairImage)), WidthRequest = 54, HeightRequest = 54, Aspect = Aspect.AspectFill }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(10, 0, 0, 0) };
        text.Add(Label(request.ServiceName ?? "Bike repair", 13, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(request.MechanicName ?? request.ShopName ?? "Waiting for assignment", 11, CustomerUi.Muted));
        text.Add(Label(request.ScheduledAt?.ToLocalTime().ToString("MMM d, h:mm tt", CultureInfo.InvariantCulture) ?? "No schedule yet", 11, CustomerUi.Muted));
        text.Add(Label(Money(request.FinalTotal > 0 ? request.FinalTotal : request.EstimatedTotal), 11, CustomerUi.Dark));
        grid.Add(text, 1, 0);
        grid.Add(Label(FormatStatus(request.CurrentStatus), 11, CustomerUi.Orange), 2, 0);

        var card = Card(grid, Colors.White, 8, new Thickness(12));
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(BookingDetailsPage)}?requestId={request.RequestId}"))
        });
        return card;
    }
}

public sealed class CustomerMessagesPage : CustomerPageBase
{
    private IReadOnlyList<ConversationSummaryDto> _conversations = [];

    public CustomerMessagesPage()
    {
        Title = "Messages";
        Render("Loading conversations...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _conversations = await CustomerApiClient.GetConversationsAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load messages. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16, 10, 16, 10), Spacing = 10 };
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 12,
            WidthRequest = 56,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = new Thickness(0),
            Command = new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))
        }, 0, 0);
        header.Add(Label("Messages", 28, CustomerUi.Orange, FontAttributes.Bold), 1, 0);
        header.Add(new Entry { Placeholder = "Search...", FontSize = 11, WidthRequest = 110, BackgroundColor = Color.FromArgb("#EFEFEF") }, 2, 0);
        body.Add(header);

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        if (_conversations.Count == 0)
        {
            body.Add(Card(Label("No conversations yet. Messages appear here after a booking has a mechanic or shop conversation.", 12, CustomerUi.Muted)));
        }
        else
        {
            foreach (var conversation in _conversations)
            {
                body.Add(MessageRow(conversation));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Messages");
    }

    private static View MessageRow(ConversationSummaryDto conversation)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(Avatar(Initials(conversation.Title), 44, Colors.White), 0, 0);

        var text = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(10, 0, 0, 0) };
        text.Add(Label(conversation.Title, 12, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(conversation.LastMessageText ?? conversation.Subtitle ?? "Open conversation", 12, CustomerUi.Dark));
        grid.Add(text, 1, 0);
        grid.Add(Label(conversation.LastMessageAt?.ToLocalTime().ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "", 9, CustomerUi.Dark), 2, 0);

        var row = Card(grid, conversation.LastMessageText is null ? Color.FromArgb("#EFEFF4") : CustomerUi.LightOrange, 0, new Thickness(10));
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.Navigation.PushAsync(new CustomerChatPage(conversation.ConversationId)))
        });
        return row;
    }
}

public sealed class CustomerChatPage : CustomerPageBase, IQueryAttributable
{
    private int _conversationId;
    private CustomerMeDto? _customer;
    private ConversationSummaryDto? _conversation;
    private IReadOnlyList<MessageDto> _messages = [];
    private Entry? _messageEntry;
    private string _draftMessage = string.Empty;

    public CustomerChatPage()
    {
        Title = "Chat";
        Render("Loading chat...");
    }

    public CustomerChatPage(int conversationId)
        : this()
    {
        _conversationId = conversationId;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("conversationId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var conversationId))
        {
            _conversationId = conversationId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_conversationId <= 0)
        {
            Render("Open a conversation from Messages first.");
            return;
        }

        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            var conversations = await CustomerApiClient.GetConversationsAsync();
            _conversation = conversations.FirstOrDefault(x => x.ConversationId == _conversationId);
            _messages = await CustomerApiClient.GetMessagesAsync(_conversationId);
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load this chat. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            }
        };

        root.Add(BuildHeader(), 0, 0);
        root.Add(new ScrollView { Content = BuildMessages(banner) }, 0, 1);
        root.Add(BuildComposer(), 0, 2);

        SetScaffold(root, "Messages", false);
    }

    private View BuildHeader()
    {
        var grid = new Grid
        {
            Padding = new Thickness(14, 8),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 12,
            WidthRequest = 56,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = new Thickness(0),
            Command = new Command(async () =>
            {
                if (Shell.Current.Navigation.NavigationStack.Count > 1)
                {
                    await Shell.Current.Navigation.PopAsync();
                }
                else
                {
                    await Shell.Current.GoToAsync("..");
                }
            })
        }, 0, 0);
        grid.Add(Avatar(Initials(_conversation?.Title), 42, CustomerUi.LightOrange), 1, 0);

        var who = new VerticalStackLayout { Spacing = 1, Margin = new Thickness(8, 0, 0, 0) };
        who.Add(Label(_conversation?.Title ?? "Conversation", 12, CustomerUi.Dark, FontAttributes.Bold));
        who.Add(Label(_conversation?.Subtitle ?? "BikeMate chat", 10, CustomerUi.Muted));
        grid.Add(who, 2, 0);

        grid.Add(HeaderIcon("i", _conversation?.Title ?? "Conversation"), 3, 0);
        grid.Add(HeaderIcon("Video", _conversation?.Title ?? "Conversation"), 4, 0);
        grid.Add(HeaderIcon("Call", _conversation?.Title ?? "Conversation"), 5, 0);

        return grid;
    }

    private static Button HeaderIcon(string text, string title)
    {
        return new Button
        {
            Text = text,
            FontSize = 11,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Orange,
            WidthRequest = text.Length > 2 ? 58 : 38,
            Command = new Command(async () => await Shell.Current.DisplayAlertAsync("Contact", title, "OK"))
        };
    }

    private View BuildMessages(string? banner)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 12 };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        if (_messages.Count == 0)
        {
            body.Add(Card(Label("No messages yet.", 12, CustomerUi.Muted)));
            return body;
        }

        foreach (var message in _messages.OrderBy(x => x.CreatedAt))
        {
            body.Add(new Label
            {
                Text = message.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy, h:mmtt", CultureInfo.InvariantCulture),
                FontSize = 10,
                TextColor = CustomerUi.Muted,
                HorizontalTextAlignment = TextAlignment.Center
            });
            body.Add(Bubble(message.MessageText, message.SenderUserId == _customer?.UserId));
        }

        return body;
    }

    private static View Bubble(string text, bool mine)
    {
        var bubble = Card(Label(text, 12, mine ? Colors.White : CustomerUi.Dark), mine ? CustomerUi.Orange : Color.FromArgb("#D9D9D9"), 8, new Thickness(12, 8));
        bubble.HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start;
        bubble.MaximumWidthRequest = 280;
        return bubble;
    }

    private View BuildComposer()
    {
        _messageEntry = new Entry
        {
            Text = _draftMessage,
            Placeholder = "Message",
            BackgroundColor = Color.FromArgb("#E8E8E8"),
            FontSize = 12
        };
        _messageEntry.TextChanged += (_, e) => _draftMessage = e.NewTextValue ?? string.Empty;

        var stack = new VerticalStackLayout { Padding = new Thickness(14, 4, 14, 10), Spacing = 6 };
        var attachments = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                GhostButton("FILE"),
                GhostButton("PHOTOS")
            }
        };
        stack.Add(attachments);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new Button { Text = "+", BackgroundColor = Colors.Transparent, TextColor = CustomerUi.Muted, FontSize = 22 }, 0, 0);
        grid.Add(_messageEntry, 1, 0);
        grid.Add(new Button
        {
            Text = "Send",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Orange,
            Command = new Command(async () => await SendAsync())
        }, 2, 0);
        stack.Add(grid);
        return stack;
    }

    private async Task SendAsync()
    {
        var text = (_messageEntry?.Text ?? _draftMessage).Trim();
        if (_conversationId <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            await CustomerApiClient.SendMessageAsync(_conversationId, text);
            _draftMessage = string.Empty;
            if (_messageEntry is not null)
            {
                _messageEntry.Text = string.Empty;
            }
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Message not sent", ex.Message, "OK");
        }
    }
}

public sealed class CustomerPaymentsPage : CustomerPageBase
{
    private IReadOnlyList<PaymentDto> _payments = [];
    private IReadOnlyList<ServiceRequestDto> _requests = [];

    public CustomerPaymentsPage()
    {
        Title = "Payments";
        Render("Loading payment history...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _requests = await CustomerApiClient.GetMyRequestsAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load payment data. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14), Spacing = 12 };
        body.Add(Header("Completed", false, "...", new Command(async () => await Shell.Current.GoToAsync(nameof(PaymentOptionsPage)))));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(PaymentTabs("Ongoing", "History"));
        if (_payments.Count == 0)
        {
            body.Add(Card(Label("No payments yet. Completed or checkout payments will appear here.", 12, CustomerUi.Muted)));
        }
        else
        {
            foreach (var payment in _payments.OrderByDescending(x => x.CreatedAt))
            {
                var request = _requests.FirstOrDefault(x => x.RequestId == payment.RequestId);
                body.Add(PaymentRow(payment, request));
            }
        }

        body.Add(OrangeButton("Secure Checkout", new Command(async () => await Shell.Current.GoToAsync(nameof(PaymentOptionsPage)))));

        SetScaffold(new ScrollView { Content = body }, "Payments");
    }

    private static View PaymentTabs(string active, string other)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(new Button { Text = active, BackgroundColor = CustomerUi.Orange, TextColor = CustomerUi.Dark, CornerRadius = 0 }, 0, 0);
        grid.Add(new Button { Text = other, BackgroundColor = Colors.White, TextColor = CustomerUi.Dark, CornerRadius = 0 }, 1, 0);
        return grid;
    }

    private static View PaymentRow(PaymentDto payment, ServiceRequestDto? request)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(new Image { Source = ImageSource.FromUri(new Uri(CustomerUi.OnlineBikeRepairImage)), WidthRequest = 64, HeightRequest = 64, Aspect = Aspect.AspectFill }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 6, Margin = new Thickness(8, 0, 0, 0) };
        text.Add(Label(request?.ServiceName ?? $"Request #{payment.RequestId}", 11, CustomerUi.Dark));
        text.Add(Label($"Total 1 Item: {Money(payment.Amount)}", 10, CustomerUi.Dark));
        text.Add(Label("View full specifications", 10, CustomerUi.Dark));
        grid.Add(text, 1, 0);
        grid.Add(Label(FormatStatus(payment.Status), 10, CustomerUi.Orange), 2, 0);

        var row = Card(grid, Colors.White, 0, new Thickness(8));
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(PaymentCheckoutPage)}?paymentId={payment.PaymentId}"))
        });
        return row;
    }
}

public sealed class PaymentOptionsPage : CustomerPageBase
{
    public PaymentOptionsPage()
    {
        Title = "Payments";
        Render();
    }

    private void Render()
    {
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 14, BackgroundColor = CustomerUi.Page };
        body.Add(Header("Payments"));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label("Secure payment", 16, CustomerUi.Dark, FontAttributes.Bold),
                Label("BikeMate opens PayMongo hosted checkout for card, GCash, PayMaya, and other supported payment methods. Payment status is verified from the backend after you return.", 12, CustomerUi.Muted),
                Label("No payment keys or card details are stored in the mobile app.", 12, CustomerUi.Muted)
            }
        }, Colors.White, 8, new Thickness(14)));

        body.Add(OrangeButton("Continue to secure payment", new Command(async () =>
        {
            var route = BookingDraft.RequestId > 0
                ? $"{nameof(PaymentCheckoutPage)}?requestId={BookingDraft.RequestId}"
                : nameof(PaymentCheckoutPage);
            await Shell.Current.GoToAsync(route);
        })));
        body.Add(GhostButton("Return home", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }

    private static View SectionHeader(string text)
    {
        return new Label
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#DCDCDC"),
            TextColor = CustomerUi.Muted,
            FontSize = 10,
            FontAttributes = FontAttributes.Bold,
            Padding = new Thickness(16, 10)
        };
    }

    private View OptionRow(string name, string note)
    {
        var selected = string.Equals(BookingDraft.PaymentMethod, name, StringComparison.OrdinalIgnoreCase);
        var grid = new Grid
        {
            BackgroundColor = selected ? Color.FromArgb("#FFF2EC") : Colors.White,
            Padding = new Thickness(16, 12),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(Label(selected ? "[x]" : "[ ]", 11, CustomerUi.Orange, FontAttributes.Bold), 0, 0);
        grid.Add(Label(name, 12, CustomerUi.Dark, FontAttributes.Bold), 1, 0);
        grid.Add(Label(note, 10, CustomerUi.Orange), 2, 0);
        grid.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.PaymentMethod = name;
                Render();
            })
        });
        return grid;
    }
}

public sealed class PaymentCheckoutPage : CustomerPageBase, IQueryAttributable
{
    private int _paymentId;
    private int _requestId;
    private PaymentDto? _payment;
    private ServiceRequestDto? _request;
    private bool _fromBookingFlow;
    private bool _isOpeningCheckout;
    private bool _returnedFromPaymentSuccess;

    public PaymentCheckoutPage()
    {
        Title = "Payment Details";
        Render("Loading payment details...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("paymentId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var paymentId))
        {
            _paymentId = paymentId;
        }

        if (query.TryGetValue("requestId", out var requestValue) &&
            int.TryParse(Uri.UnescapeDataString(requestValue?.ToString() ?? ""), out var requestId))
        {
            _requestId = requestId;
            _fromBookingFlow = true;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var paymentReturn = PaymentReturnService.ConsumeReturn();
        if (paymentReturn is not null)
        {
            if (paymentReturn.PaymentId > 0)
            {
                _paymentId = paymentReturn.PaymentId;
            }

            if (paymentReturn.RequestId > 0)
            {
                _requestId = paymentReturn.RequestId;
            }

            _fromBookingFlow = _fromBookingFlow || paymentReturn.FromBookingFlow;
            _returnedFromPaymentSuccess = string.Equals(paymentReturn.Status, "success", StringComparison.OrdinalIgnoreCase);
            await LoadAsync(PaymentReturnService.FormatBanner(paymentReturn));
            return;
        }

        await LoadAsync();
    }

    private async Task LoadAsync(string? banner = null)
    {
        try
        {
            var payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _requestId = _requestId > 0 ? _requestId : BookingDraft.RequestId;
            _fromBookingFlow = _fromBookingFlow || BookingDraft.RequestId > 0;

            if (_paymentId > 0)
            {
                _payment = payments.FirstOrDefault(x => x.PaymentId == _paymentId);
            }
            else if (_requestId > 0)
            {
                _payment = payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.RequestId == _requestId);
                _request = await CustomerApiClient.GetRequestAsync(_requestId);
                if (_payment is null)
                {
                    var amount = _request.FinalTotal > 0 ? _request.FinalTotal : _request.EstimatedTotal;
                    _payment = await CustomerApiClient.CreateCheckoutAsync(new CreateCheckoutSessionDto(_requestId, amount));
                }
            }
            else
            {
                _payment = payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            }

            if (_payment is not null)
            {
                _paymentId = _payment.PaymentId;
                _requestId = _payment.RequestId;
                BookingDraft.PaymentId = _payment.PaymentId;
                _request ??= await CustomerApiClient.GetRequestAsync(_payment.RequestId);
            }

            Render(banner);
        }
        catch (Exception ex)
        {
            var message = $"Connect the API to load payment details. {ex.Message}";
            Render(string.IsNullOrWhiteSpace(banner) ? message : $"{banner}\n{message}");
        }
    }

    private void Render(string? banner = null)
    {
        var selectedService = BookingDraft.SelectedService();
        var amount = _payment?.Amount
            ?? (_request is null ? selectedService?.BasePrice ?? 0m : _request.FinalTotal > 0 ? _request.FinalTotal : _request.EstimatedTotal);
        var service = _request?.ServiceName
            ?? selectedService?.ServiceName
            ?? (_payment is null ? "No payment selected" : $"Request #{_payment.RequestId}");
        var issue = _request?.IssueDescription ?? $"{BookingDraft.ProblemCategory}: {BookingDraft.OtherDetails}";
        var location = _request?.ServiceLocationAddress ?? BookingDraft.ConfirmationAddress();
        var schedule = _request?.ScheduledAt?.ToLocalTime() ?? BookingDraft.ScheduledAt;
        var bookingStatus = _request is null ? "pending" : FormatStatus(_request.CurrentStatus);
        var isPaid = IsPaidForDisplay();
        var paymentStatus = isPaid ? "Paid" : _payment is null ? "Unpaid" : FormatStatus(_payment.Status);

        var body = new VerticalStackLayout { Spacing = 12 };
        body.Add(new Grid
        {
            BackgroundColor = CustomerUi.Orange,
            Padding = new Thickness(16, 12),
            Children =
            {
                new Label
                {
                    Text = $"Payment Details\n{paymentStatus} - total: {Money(amount)}",
                    TextColor = Colors.White,
                    HorizontalTextAlignment = TextAlignment.Center,
                    FontAttributes = FontAttributes.Bold,
                    FontSize = 13
                }
            }
        });

        var content = new VerticalStackLayout { Padding = new Thickness(18), Spacing = 14 };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            content.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        content.Add(new Image { Source = ImageSource.FromUri(new Uri(CustomerUi.OnlineBikeRepairImage)), HeightRequest = 90, HorizontalOptions = LayoutOptions.Center });
        content.Add(Label("Service paid:", 12, CustomerUi.Dark));
        content.Add(Label(service, 12, CustomerUi.Dark));
        content.Add(Label("Repair Summary", 13, CustomerUi.Dark, FontAttributes.Bold));
        content.Add(Row("Item", service));
        content.Add(Row("Repair Type", issue));
        content.Add(Row("Service Type", BookingDraft.ServiceType));
        content.Add(Row("Bike", $"{BookingDraft.Brand} {BookingDraft.Model}"));
        content.Add(Row("Location", location));
        content.Add(Row("Service Date", schedule.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture)));
        content.Add(Row("Service Time", schedule.ToString("h:mm tt", CultureInfo.InvariantCulture)));
        content.Add(Row("Technician", _request?.MechanicName ?? "Not assigned"));
        content.Add(Row("Booking Status", bookingStatus));
        content.Add(Separator());
        content.Add(Row("Payment Method", "PayMongo secure checkout"));
        content.Add(Row("Provider", _payment?.ProviderName ?? "paymongo"));
        content.Add(Row("Reference", _payment?.ReferenceNumber ?? $"BM-PAY-{_paymentId:0000}"));
        content.Add(Row("Payment Status", paymentStatus));
        content.Add(Row("Total", Money(amount)));
        if (isPaid)
        {
            content.Add(Card(Label(
                string.Equals(_payment?.Status, "paid", StringComparison.OrdinalIgnoreCase)
                    ? "Payment confirmed by BikeMate."
                    : "Payment completed in PayMongo. BikeMate will keep verifying the backend record.",
                11,
                CustomerUi.Muted),
                Colors.White,
                8,
                new Thickness(12)));
        }

        if (!isPaid &&
            Uri.TryCreate(_payment?.CheckoutUrl, UriKind.Absolute, out var checkoutUri))
        {
            content.Add(OrangeButton(_isOpeningCheckout ? "Opening secure payment..." : "Continue to secure payment", new Command(async () => await OpenCheckoutAsync(checkoutUri))));
        }

        content.Add(GhostButton("Refresh payment status", new Command(async () => await LoadAsync())));
        if (isPaid && _paymentId > 0)
        {
            content.Add(GhostButton("View receipt", new Command(async () => await Shell.Current.GoToAsync($"{nameof(PaymentReceiptPage)}?paymentId={_paymentId}"))));
        }

        content.Add(OrangeButton("Return home", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));
        body.Add(content);

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }

    private async Task OpenCheckoutAsync(Uri checkoutUri)
    {
        if (_isOpeningCheckout)
        {
            return;
        }

        _isOpeningCheckout = true;
        var openedCheckout = false;
        Render();
        try
        {
            PaymentReturnService.RememberCheckoutContext(_paymentId, _requestId, _fromBookingFlow);

            if (!await Launcher.Default.CanOpenAsync(checkoutUri))
            {
                await DisplayAlertAsync("Payment unavailable", "Android could not open the secure PayMongo checkout link. Enable a browser, then try again.", "OK");
                return;
            }

            var opened = await Launcher.Default.OpenAsync(checkoutUri);
            if (!opened)
            {
                await DisplayAlertAsync("Payment unavailable", "Android did not open the PayMongo checkout page. Please try again.", "OK");
                return;
            }

            openedCheckout = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PayMongo checkout open failed: {ex}");
            await DisplayAlertAsync("Payment unavailable", "Could not open secure PayMongo checkout. Please try again or contact support.", "OK");
        }
        finally
        {
            _isOpeningCheckout = false;
            if (openedCheckout)
            {
                Render("PayMongo checkout opened. BikeMate will refresh this page when you return.");
            }
            else
            {
                await LoadAsync();
            }
        }
    }

    private bool IsPaidForDisplay()
    {
        return string.Equals(_payment?.Status, "paid", StringComparison.OrdinalIgnoreCase) || _returnedFromPaymentSuccess;
    }
}

public sealed class PaymentReceiptPage : CustomerPageBase, IQueryAttributable
{
    private int _paymentId;
    private PaymentDto? _payment;
    private CustomerMeDto? _customer;

    public PaymentReceiptPage()
    {
        Title = "Payment Receipt";
        Render("Loading receipt...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("paymentId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var paymentId))
        {
            _paymentId = paymentId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            var payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _payment = payments.FirstOrDefault(x => x.PaymentId == _paymentId) ?? payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (_payment is not null)
            {
                _paymentId = _payment.PaymentId;
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load receipt data. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var name = _customer is null ? "Customer" : $"{_customer.FirstName} {_customer.LastName}";
        var amount = _payment?.Amount ?? 0m;
        var paidDate = (_payment?.PaidAt ?? _payment?.CreatedAt)?.ToLocalTime().ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) ?? "";

        var body = new VerticalStackLayout { Padding = new Thickness(20, 10, 20, 20), Spacing = 14 };
        body.Add(Header(""));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(new Image { Source = "bikemate_logo.png", HeightRequest = 150, HorizontalOptions = LayoutOptions.Center });
        body.Add(new Label { Text = $"Invoice # {_paymentId}", FontSize = 13, TextColor = CustomerUi.Dark, HorizontalTextAlignment = TextAlignment.Center });
        body.Add(new Label { Text = $"For {name}\nPaid on {paidDate}", FontSize = 12, TextColor = CustomerUi.Muted, HorizontalTextAlignment = TextAlignment.Center });
        body.Add(Label($"Hi {name},", 12, CustomerUi.Dark));
        body.Add(Label($"Here is your payment receipt for Invoice #{_paymentId}, for {Money(amount)}.", 12, CustomerUi.Dark));
        body.Add(Label("You can always view your receipt inside BikeMate.", 12, CustomerUi.Dark));
        body.Add(Label("If you have any questions, please let us know.\n\nThanks\nBikeMate", 12, CustomerUi.Dark));
        body.Add(Separator());
        body.Add(new Label { Text = $"Payment amount: {Money(amount)}", FontSize = 13, TextColor = CustomerUi.Dark, HorizontalTextAlignment = TextAlignment.Center });
        body.Add(OrangeButton("View Invoice", new Command(async () => await Shell.Current.GoToAsync($"{nameof(PaymentInvoicePage)}?paymentId={_paymentId}"))));

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }
}

public sealed class PaymentInvoicePage : CustomerPageBase, IQueryAttributable
{
    private int _paymentId;
    private PaymentDto? _payment;
    private CustomerMeDto? _customer;

    public PaymentInvoicePage()
    {
        Title = "Payment Invoice";
        Render("Loading invoice...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("paymentId", out var value) &&
            int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var paymentId))
        {
            _paymentId = paymentId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            var payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _payment = payments.FirstOrDefault(x => x.PaymentId == _paymentId) ?? payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (_payment is not null)
            {
                _paymentId = _payment.PaymentId;
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load invoice data. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var name = _customer is null ? "Customer" : $"{_customer.FirstName} {_customer.LastName}";
        var amount = _payment?.Amount ?? 0m;
        var created = (_payment?.CreatedAt ?? DateTime.UtcNow).ToLocalTime();

        var body = new VerticalStackLayout { Padding = new Thickness(18, 8, 18, 20), Spacing = 12 };
        body.Add(Header(""));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Label("GCASH", 28, Color.FromArgb("#136FE8"), FontAttributes.Bold));
        body.Add(new Label
        {
            Text = $"BikeMate        {created:dd MMMM yyyy HH:mm:ss}",
            TextColor = Colors.White,
            BackgroundColor = Colors.Black,
            Padding = new Thickness(8),
            FontSize = 11
        });
        body.Add(Label("INTERNAL PAY NOW", 15, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(Row("Reference Code", _payment?.ReferenceNumber ?? $"BM-PAY-{_paymentId:0000}"));
        body.Add(Row("Transfer to", "BikeMate"));
        body.Add(Row("Account Number", "XXXXXXXXXX019"));
        body.Add(Row("Account Name", "BikeMate"));
        body.Add(Row("Transfer from", name));
        body.Add(Row("Amount", Money(amount)));
        body.Add(Row("Transfer Date", created.ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)));
        body.Add(Row("Purpose", $"Request #{_payment?.RequestId ?? 0} payment"));
        body.Add(new BoxView { HeightRequest = 80, Opacity = 0 });
        body.Add(Label("This is a computer generated receipt no signature required", 10, CustomerUi.Dark));
        body.Add(OrangeButton("Done", new Command(async () => await Shell.Current.GoToAsync("//CustomerPaymentsPage"))));

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
    }
}

public sealed class BookServicePage : CustomerPageBase
{
    private CustomerMeDto? _customer;
    private IReadOnlyList<ShopServiceDto> _services = [];
    private Editor? _addressEditor;

    public BookServicePage()
    {
        Title = "Book Now";
        BookingDraft.Reset();
        Render("Loading booking data...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            _services = await CustomerApiClient.SearchServicesAsync();
            BookingDraft.ApplyCustomer(_customer);
            BookingDraft.Services = _services;
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to create real bookings. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14, 0, 14, 16), Spacing = 8 };
        body.Add(BookingVisuals.FlowHeader("Book Now!"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(Label(banner, 11, CustomerUi.Muted), 6, new Thickness(10)));
        }

        body.Add(BookingVisuals.MapPanel(210, true));
        body.Add(BookingVisuals.FieldRow("Region / Main Area", BookingDraft.Region, new Command(async () => await SelectRegionAsync())));
        body.Add(BookingVisuals.FieldRow("Your Location", BookingDraft.LocationName, new Command(async () => await SelectLocationAsync())));

        _addressEditor = new Editor
        {
            Text = BookingDraft.AddressLine,
            Placeholder = "Address *",
            HeightRequest = 78,
            FontSize = 11,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Orange,
            BackgroundColor = Colors.Transparent
        };
        _addressEditor.TextChanged += (_, e) => BookingDraft.AddressLine = e.NewTextValue ?? string.Empty;
        body.Add(BookingVisuals.WhiteCard(_addressEditor, 4, new Thickness(8)));
        body.Add(new BoxView { HeightRequest = 12, Opacity = 0 });
        body.Add(BookingVisuals.PrimaryButton("Continue", new Command(async () => await ContinueAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    public void RefreshLocationUi()
    {
        Render();
    }

    private async Task SelectRegionAsync()
    {
        var selected = await BookingOptionSheet.ShowAsync(
            "Select Region / Main Area",
            ["Baguio", "Cagayan de Oro", "Cebu", "Mega Manila", "Naga and Legazpi", "Central Luzon"],
            BookingDraft.Region);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            BookingDraft.Region = selected;
            Render();
        }
    }

    private async Task SelectLocationAsync()
    {
        var selected = await BookingOptionSheet.ShowAsync(
            "Select Location",
            ["Makati Ave, Mega Manila", "San Pedro, Laguna", "Pasig City", "Quezon City", "Use current location"],
            BookingDraft.LocationName);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            if (selected == "Use current location")
            {
                await BookingVisuals.UpdateCurrentLocationAsync(this);
            }
            else
            {
                BookingDraft.SetManualLocation(selected);
            }
            Render();
        }
    }

    private async Task ContinueAsync()
    {
        BookingDraft.Customer = _customer ?? BookingDraft.Customer;
        BookingDraft.Services = _services.Count == 0 ? BookingDraft.Services : _services;
        BookingDraft.AddressLine = _addressEditor?.Text?.Trim() ?? BookingDraft.AddressLine;

        if (string.IsNullOrWhiteSpace(BookingDraft.AddressLine))
        {
            await DisplayAlertAsync("Location required", "Please enter the service address.", "OK");
            return;
        }

        if (BookingDraft.Customer is null)
        {
            await DisplayAlertAsync("Profile required", "Connect the API or login as a customer before booking.", "OK");
            return;
        }

        await Shell.Current.GoToAsync(nameof(BookingFillUpPage));
    }
}

public sealed class BookingDetailsPage : CustomerPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _request;

    public BookingDetailsPage()
    {
        Title = "Booking Details";
        Render("Loading booking details...");
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
        try
        {
            if (_requestId > 0)
            {
                _request = await CustomerApiClient.GetRequestAsync(_requestId);
            }
            else
            {
                _request = (await CustomerApiClient.GetMyRequestsAsync()).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                _requestId = _request?.RequestId ?? 0;
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load booking details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(18), Spacing = 14 };
        body.Add(Header("Booking Details"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label(_request?.ServiceName ?? "Bike repair", 18, CustomerUi.Dark, FontAttributes.Bold),
                Label($"Status: {(_request is null ? "" : FormatStatus(_request.CurrentStatus))}", 13, CustomerUi.Orange, FontAttributes.Bold),
                Label($"Mechanic: {_request?.MechanicName ?? "Not assigned"}", 13, CustomerUi.Dark),
                Label($"Shop: {_request?.ShopName ?? "Not assigned"}", 13, CustomerUi.Dark),
                Label($"Location: {_request?.ServiceLocationAddress ?? "No address"}", 13, CustomerUi.Dark),
                Label($"Estimated total: {Money(_request?.EstimatedTotal ?? 0m)}", 13, CustomerUi.Dark)
            }
        }));
        body.Add(OrangeButton("Track Mechanic", new Command(async () => await Shell.Current.GoToAsync($"{nameof(TrackMechanicPage)}?requestId={_requestId}"))));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }
}

public sealed class TrackMechanicPage : CustomerPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _request;

    public TrackMechanicPage()
    {
        Title = "Track Mechanic";
        Render("Loading tracking details...");
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
        try
        {
            _request = _requestId > 0
                ? await CustomerApiClient.GetRequestAsync(_requestId)
                : (await CustomerApiClient.GetMyRequestsAsync()).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load tracking data. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(18), Spacing = 14 };
        body.Add(Header("Track Mechanic"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label($"{_request?.MechanicName ?? "Mechanic"} status", 18, CustomerUi.Dark, FontAttributes.Bold),
                Label($"Current status: {(_request is null ? "" : FormatStatus(_request.CurrentStatus))}", 13, CustomerUi.Dark),
                Label($"Service: {_request?.ServiceName ?? "Bike repair"}", 13, CustomerUi.Dark),
                Label($"Location: {_request?.ServiceLocationAddress ?? "No address"}", 13, CustomerUi.Dark),
                Label("Live map coordinates can be attached here from the locations endpoint.", 12, CustomerUi.Muted)
            }
        }));
        body.Add(OrangeButton("Open in Google Maps", new Command(async () =>
        {
            await BookingVisuals.OpenGoogleMapsAsync(_request?.ServiceLocationAddress ?? BookingDraft.ConfirmationAddress());
        })));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }
}
