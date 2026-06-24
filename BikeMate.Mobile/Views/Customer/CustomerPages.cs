using System.Globalization;
using System.Windows.Input;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using BikeMate.Views.Auth;
using BikeMate.Views.Customer.Emergency;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Customer;

internal static class CustomerUi
{
    public static readonly Color Orange = Color.FromArgb("#FF6B2C");
    public static readonly Color LightOrange = Color.FromArgb("#FFE1D2");
    public static readonly Color Dark = Color.FromArgb("#242424");
    public static readonly Color Muted = Color.FromArgb("#6E6E6E");
    public static readonly Color Border = Color.FromArgb("#E6E6E6");
    public static readonly Color Page = Color.FromArgb("#F6F6F6");

    public const double CaptionSize = AppTypography.CaptionSize;
    public const double BodySize = AppTypography.BodySize;
    public const double TitleSize = AppTypography.TitleSize;
    public const string FontBody = AppTypography.BodyFont;
    public const string FontDisplay = AppTypography.DisplayFont;
    public const string FontCaption = AppTypography.CaptionFont;
    public const string FontCaptionBold = AppTypography.CaptionBoldFont;

    public const string OnlineBikeRepairImage = "https://img.icons8.com/color/96/bicycle.png";
    public const string HomeIcon = "https://img.icons8.com/ios/50/home--v1.png";
    public const string ScheduleIcon = "https://img.icons8.com/ios/50/calendar--v1.png";
    public const string PaymentsIcon = "https://img.icons8.com/ios/50/wallet--v1.png";
    public const string MessagesIcon = "https://img.icons8.com/ios/50/speech-bubble--v1.png";

    public static string FontFor(double size, FontAttributes attributes = FontAttributes.None)
    {
        return AppTypography.FontFor(size, attributes);
    }

    public static double SizeFor(double size)
    {
        return AppTypography.SizeFor(size);
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
            FontSize = AppTypography.SizeFor(size),
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
            FontSize = 13,
            WidthRequest = 56,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = new Thickness(0)
        }, 0, 0);

        grid.Add(new Label
        {
            Text = title,
            FontSize = 13,
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
                FontSize = 13,
                FontFamily = CustomerUi.FontDisplay
            }, 2, 0);
        }
        else
        {
            grid.Add(new BoxView
            {
                WidthRequest = 56,
                Opacity = 0,
                InputTransparent = true
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
                FontSize = CustomerUi.TitleSize,
                FontFamily = CustomerUi.FontDisplay,
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
            FontSize = 13,
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

        var bookedRepairsTitle = Label("Booked Repairs", 14, CustomerUi.Dark, FontAttributes.Bold);
        bookedRepairsTitle.HorizontalOptions = LayoutOptions.Fill;
        bookedRepairsTitle.HorizontalTextAlignment = TextAlignment.Center;
        body.Add(bookedRepairsTitle);
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
            FontSize = 13
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
    private Entry? _firstNameEntry;
    private Entry? _middleNameEntry;
    private Entry? _lastNameEntry;
    private Entry? _emailEntry;
    private Entry? _phoneEntry;
    private Picker? _sexPicker;
    private DatePicker? _birthdayPicker;
    private Switch? _birthdaySwitch;
    private Entry? _provinceEntry;
    private Entry? _cityEntry;
    private Entry? _barangayEntry;
    private Editor? _addressEditor;
    private Entry? _bikeBrandEntry;
    private Entry? _bikeModelEntry;
    private Entry? _bikeYearEntry;
    private Entry? _bikePlateEntry;
    private Entry? _bikeEngineEntry;
    private Entry? _bikeColorEntry;
    private bool _isLoading;
    private bool _isSaving;

    public CustomerProfilePage()
    {
        Title = "Account Details";
        Render("Loading account details...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            Render();
        }
        catch (ApiSessionExpiredException)
        {
        }
        catch (Exception ex)
        {
            Render($"Account details could not be loaded. Check your connection and try again. {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void Render(string? banner = null)
    {
        var customer = _customer;
        var defaultAddress = customer?.Addresses.FirstOrDefault(x => x.IsDefault) ?? customer?.Addresses.FirstOrDefault();
        var motorcycle = customer?.Motorcycles.FirstOrDefault();
        PrepareFields(customer, defaultAddress, motorcycle);

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 20),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };

        body.Add(Header("Account Details", true, _isSaving ? "Saving" : "Save", new Command(async () => await SaveProfileAsync())));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(ProfileHeaderCard(customer));
        body.Add(PersonalDetailsCard());
        body.Add(ContactDetailsCard(customer));
        body.Add(AddressCard());
        body.Add(MotorcycleCard());
        body.Add(AccountSecurityCard(customer));
        body.Add(DangerZoneCard());
        body.Add(OrangeButton(_isSaving ? "Saving account details..." : "Save account details", new Command(async () => await SaveProfileAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task RefreshProfileAsync()
    {
        _customer = await CustomerApiClient.GetCustomerAsync();
        Render();
    }

    private void PrepareFields(CustomerMeDto? customer, CustomerAddressDto? address, MotorcycleDto? motorcycle)
    {
        _firstNameEntry = Field(customer?.FirstName, "First name");
        _middleNameEntry = Field(customer?.MiddleName, "Middle name");
        _lastNameEntry = Field(customer?.LastName, "Last name");
        _emailEntry = Field(customer?.Email, "Email address", Keyboard.Email);
        _phoneEntry = Field(customer?.PhoneNumber, "+63 mobile number", Keyboard.Telephone);
        _phoneEntry.MaxLength = 13;

        _sexPicker = new Picker
        {
            Title = "Select sex",
            TextColor = CustomerUi.Dark,
            TitleColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };
        _sexPicker.Items.Add("Female");
        _sexPicker.Items.Add("Male");
        _sexPicker.Items.Add("Prefer not to say");
        if (!string.IsNullOrWhiteSpace(customer?.Sex))
        {
            var index = _sexPicker.Items.IndexOf(customer.Sex);
            _sexPicker.SelectedIndex = index >= 0 ? index : -1;
        }

        _birthdaySwitch = new Switch { IsToggled = customer?.Birthdate is not null, OnColor = CustomerUi.Orange };
        _birthdayPicker = new DatePicker
        {
            Date = customer?.Birthdate?.Date ?? DateTime.Today.AddYears(-18),
            MinimumDate = DateTime.Today.AddYears(-100),
            MaximumDate = DateTime.Today.AddYears(-16),
            TextColor = CustomerUi.Dark,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };

        _provinceEntry = Field(address?.Province, "Province");
        _cityEntry = Field(address?.City, "City or municipality");
        _barangayEntry = Field(address?.Barangay, "Barangay");
        _addressEditor = EditorField(address?.AddressLine, "House number, street, subdivision, landmark");

        _bikeBrandEntry = Field(motorcycle?.Brand, "Brand");
        _bikeModelEntry = Field(motorcycle?.Model, "Model");
        _bikeYearEntry = Field(motorcycle?.YearModel?.ToString(CultureInfo.InvariantCulture), "Year model", Keyboard.Numeric);
        _bikePlateEntry = Field(motorcycle?.PlateNumber, "Plate number");
        _bikeEngineEntry = Field(motorcycle?.EngineType, "Engine type");
        _bikeColorEntry = Field(motorcycle?.Color, "Color");
    }

    private View ProfileHeaderCard(CustomerMeDto? customer)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 14
        };

        grid.Add(ProfilePhoto(customer), 0, 0);

        var details = new VerticalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        details.Add(Label(customer is null ? "Loading account" : FullName(customer), 18, CustomerUi.Dark, FontAttributes.Bold));
        details.Add(Label(customer is null ? "Fetching your BikeMate profile." : $"CUST-{customer.ClientId:0000}", 11, CustomerUi.Muted));
        var badges = new HorizontalStackLayout { Spacing = 6 };
        badges.Add(Badge(FormatStatus(customer?.AccountStatus ?? "loading"), CustomerUi.LightOrange, CustomerUi.Orange));
        badges.Add(Badge(customer?.EmailVerified == true ? "Email verified" : "Email pending", Color.FromArgb("#EEF1F4"), CustomerUi.Dark));
        details.Add(badges);

        var actions = new Grid { ColumnSpacing = 8 };
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        actions.Add(GhostButton("Change photo", new Command(async () => await ChangeProfilePhotoAsync())), 0, 0);
        actions.Add(GhostButton(customer?.ValidIdImageUrl is null ? "Upload ID" : "Replace ID", new Command(async () => await ReplaceValidIdAsync())), 1, 0);
        details.Add(actions);

        grid.Add(details, 1, 0);
        return Card(grid, Colors.White, 8, new Thickness(14));
    }

    private View PersonalDetailsCard()
    {
        var stack = Section("Personal details", "Shown to shops and mechanics assigned to your bookings.");
        stack.Add(TwoColumnInputs(("First name", _firstNameEntry!), ("Last name", _lastNameEntry!)));
        stack.Add(InputBlock("Middle name", _middleNameEntry!));
        stack.Add(InputBlock("Sex", _sexPicker!));
        var birthday = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        birthday.Add(InputBlock("Birthdate", _birthdayPicker!), 0, 0);
        birthday.Add(new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.End,
            Children =
            {
                Label("Use", 11, CustomerUi.Muted),
                _birthdaySwitch!
            }
        }, 1, 0);
        stack.Add(birthday);
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View ContactDetailsCard(CustomerMeDto? customer)
    {
        var stack = Section("Contact details", "Keep this current so support and repair partners can reach you.");
        stack.Add(InputBlock("Email address", _emailEntry!));
        stack.Add(BadgeRow("Email status", customer?.EmailVerified == true ? "Verified" : "Verification pending"));
        stack.Add(InputBlock("Mobile number", _phoneEntry!));
        stack.Add(BadgeRow("Phone status", customer?.PhoneVerified == true ? "Verified" : "Not verified"));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View AddressCard()
    {
        var stack = Section("Primary service address", "Used as your default pickup or repair location.");
        stack.Add(TwoColumnInputs(("Province", _provinceEntry!), ("City", _cityEntry!)));
        stack.Add(InputBlock("Barangay", _barangayEntry!));
        stack.Add(InputBlock("Complete address", _addressEditor!));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View MotorcycleCard()
    {
        var stack = Section("Motorcycle details", "These details help shops prepare parts and tools before arrival.");
        stack.Add(TwoColumnInputs(("Brand", _bikeBrandEntry!), ("Model", _bikeModelEntry!)));
        stack.Add(TwoColumnInputs(("Year", _bikeYearEntry!), ("Plate", _bikePlateEntry!)));
        stack.Add(TwoColumnInputs(("Engine", _bikeEngineEntry!), ("Color", _bikeColorEntry!)));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View AccountSecurityCard(CustomerMeDto? customer)
    {
        var stack = Section("Security and account", "Manage access and account health.");
        stack.Add(DetailLine("Account ID", customer is null ? "Loading" : $"CUST-{customer.ClientId:0000}"));
        stack.Add(DetailLine("Member since", customer is null ? "Loading" : customer.CreatedAt.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture)));
        stack.Add(DetailLine("Valid ID", string.IsNullOrWhiteSpace(customer?.ValidIdImageUrl) ? "Not uploaded" : "Uploaded"));
        stack.Add(GhostButton("Change password", new Command(async () => await Shell.Current.GoToAsync(nameof(PasswordResetPage)))));
        stack.Add(GhostButton("Log out", new Command(async () => await AppNavigation.SignOutAsync())));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View DangerZoneCard()
    {
        var stack = Section("Account deletion", "Deleting your account disables sign-in but keeps booking and payment records required for support and compliance.");
        stack.Add(new Button
        {
            Text = "Delete account",
            BackgroundColor = Color.FromArgb("#FFF0F0"),
            TextColor = Color.FromArgb("#B42318"),
            BorderColor = Color.FromArgb("#F5B5B5"),
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 46,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            Command = new Command(async () => await DeleteAccountAsync())
        });
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private async Task SaveProfileAsync()
    {
        if (_customer is null || _isSaving)
        {
            return;
        }

        var firstName = Clean(_firstNameEntry?.Text);
        var middleName = Clean(_middleNameEntry?.Text);
        var lastName = Clean(_lastNameEntry?.Text);
        var email = Clean(_emailEntry?.Text);
        var phoneNumber = Clean(_phoneEntry?.Text);
        var sex = _sexPicker?.SelectedItem?.ToString();
        var birthdate = _birthdaySwitch?.IsToggled == true ? _birthdayPicker?.Date?.Date : null;
        var province = Clean(_provinceEntry?.Text);
        var city = Clean(_cityEntry?.Text);
        var barangay = Clean(_barangayEntry?.Text);
        var addressLine = Clean(_addressEditor?.Text);
        var bikeBrand = Clean(_bikeBrandEntry?.Text);
        var bikeModel = Clean(_bikeModelEntry?.Text);
        var yearText = Clean(_bikeYearEntry?.Text);
        var bikePlate = Clean(_bikePlateEntry?.Text);
        var bikeEngine = Clean(_bikeEngineEntry?.Text);
        var bikeColor = Clean(_bikeColorEntry?.Text);

        if (firstName is null || lastName is null || email is null)
        {
            await DisplayAlertAsync("Missing details", "First name, last name, and email are required.", "OK");
            return;
        }

        if (addressLine is null)
        {
            await DisplayAlertAsync("Missing address", "Please add your complete service address.", "OK");
            return;
        }

        var hasBikeDetails = bikeBrand is not null || bikeModel is not null || yearText is not null ||
                             bikePlate is not null || bikeEngine is not null || bikeColor is not null;
        if (hasBikeDetails && (bikeBrand is null || bikeModel is null))
        {
            await DisplayAlertAsync("Motorcycle details", "Bike brand and model are required when saving motorcycle details.", "OK");
            return;
        }

        int? yearModel = null;
        if (yearText is not null)
        {
            if (!int.TryParse(yearText, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedYear) ||
                parsedYear < 1950 ||
                parsedYear > DateTime.Today.Year + 1)
            {
                await DisplayAlertAsync("Motorcycle year", "Enter a valid motorcycle year model.", "OK");
                return;
            }

            yearModel = parsedYear;
        }

        _isSaving = true;
        Render("Saving your account details...");
        try
        {
            await CustomerApiClient.UpdateCustomerAsync(new UpsertCustomerProfileDto(
                firstName,
                lastName,
                email,
                phoneNumber,
                middleName,
                sex,
                birthdate));

            var existingAddress = _customer.Addresses.FirstOrDefault(x => x.IsDefault) ?? _customer.Addresses.FirstOrDefault();
            await CustomerApiClient.UpsertAddressAsync(existingAddress, new UpsertCustomerAddressDto(
                existingAddress?.Label ?? "Home",
                addressLine,
                barangay,
                city,
                province,
                existingAddress?.PostalCode,
                existingAddress?.Latitude,
                existingAddress?.Longitude,
                true));

            if (hasBikeDetails)
            {
                var existingMotorcycle = _customer.Motorcycles.FirstOrDefault();
                await CustomerApiClient.UpsertMotorcycleAsync(existingMotorcycle, new UpsertMotorcycleDto(
                    bikeBrand ?? string.Empty,
                    bikeModel ?? string.Empty,
                    yearModel,
                    bikePlate,
                    bikeEngine,
                    bikeColor,
                    existingMotorcycle?.MotorcycleImageUrl));
            }

            _customer = await CustomerApiClient.GetCustomerAsync();
            _isSaving = false;
            Render("Account details saved.");
        }
        catch (Exception ex)
        {
            _isSaving = false;
            Render($"Account details were not saved. {ex.Message}");
        }
    }

    private async Task ChangeProfilePhotoAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var file = await PickImageAsync("Choose profile photo");
        if (file is null)
        {
            return;
        }

        try
        {
            Render("Uploading profile photo...");
            var upload = await CustomerApiClient.UploadFileAsync(file, "profile");
            await CustomerApiClient.UpdateCustomerProfileImageAsync(upload.Url);
            await RefreshProfileAsync();
        }
        catch (Exception ex)
        {
            Render($"Profile photo was not updated. {ex.Message}");
        }
    }

    private async Task ReplaceValidIdAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var file = await PickImageAsync("Upload valid ID");
        if (file is null)
        {
            return;
        }

        try
        {
            Render("Uploading valid ID...");
            var upload = await CustomerApiClient.UploadFileAsync(file, "customer-id");
            await CustomerApiClient.UpdateCustomerValidIdAsync(upload.Url);
            await RefreshProfileAsync();
        }
        catch (Exception ex)
        {
            Render($"Valid ID was not updated. {ex.Message}");
        }
    }

    private async Task DeleteAccountAsync()
    {
        if (_customer is null)
        {
            return;
        }

        var confirm = await DisplayAlertAsync(
            "Delete account",
            "This will disable your BikeMate account and sign you out. Booking, payment, and support records may be retained for service history and compliance.",
            "Continue",
            "Cancel");
        if (!confirm)
        {
            return;
        }

        var typed = await DisplayPromptAsync("Final confirmation", "Type DELETE to confirm account deletion.", "Delete", "Cancel");
        if (!string.Equals(typed, "DELETE", StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            await CustomerApiClient.DeleteCustomerAccountAsync();
            await AppNavigation.SignOutAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Account not deleted", ex.Message, "OK");
        }
    }

    private static async Task<FileResult?> PickImageAsync(string title)
    {
        return await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title,
            FileTypes = FilePickerFileType.Images
        });
    }

    private static string FullName(CustomerMeDto customer)
    {
        return string.Join(" ", new[] { customer.FirstName, customer.MiddleName, customer.LastName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static View ProfilePhoto(CustomerMeDto? customer)
    {
        if (!string.IsNullOrWhiteSpace(customer?.ProfileImageUrl))
        {
            return new Border
            {
                WidthRequest = 94,
                HeightRequest = 94,
                StrokeShape = new RoundRectangle { CornerRadius = 47 },
                Stroke = CustomerUi.Border,
                StrokeThickness = 1,
                BackgroundColor = Color.FromArgb("#F2F2F2"),
                Content = new Image
                {
                    Source = ImageSourceFor(customer.ProfileImageUrl),
                    Aspect = Aspect.AspectFill
                }
            };
        }

        return Avatar(Initials(customer?.FirstName, customer?.LastName), 94, CustomerUi.LightOrange);
    }

    private static ImageSource ImageSourceFor(string value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            ? ImageSource.FromUri(uri)
            : ImageSource.FromFile(value);
    }

    private static VerticalStackLayout Section(string title, string subtitle)
    {
        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                Label(title, 14, CustomerUi.Dark, FontAttributes.Bold),
                Label(subtitle, 11, CustomerUi.Muted)
            }
        };
    }

    private static View TwoColumnInputs((string Label, View Input) left, (string Label, View Input) right)
    {
        var grid = new Grid { ColumnSpacing = 10 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Add(InputBlock(left.Label, left.Input), 0, 0);
        grid.Add(InputBlock(right.Label, right.Input), 1, 0);
        return grid;
    }

    private static View InputBlock(string label, View input)
    {
        var stack = new VerticalStackLayout { Spacing = 5 };
        stack.Add(Label(label, 11, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(new Border
        {
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#FAFAFA"),
            Padding = new Thickness(10, 2),
            Content = input
        });
        return stack;
    }

    private static Entry Field(string? value, string placeholder, Keyboard? keyboard = null)
    {
        return new Entry
        {
            Text = value ?? string.Empty,
            Placeholder = placeholder,
            Keyboard = keyboard ?? Keyboard.Text,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody
        };
    }

    private static Editor EditorField(string? value, string placeholder)
    {
        return new Editor
        {
            Text = value ?? string.Empty,
            Placeholder = placeholder,
            HeightRequest = 78,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontSize = CustomerUi.BodySize,
            FontFamily = CustomerUi.FontBody,
            AutoSize = EditorAutoSizeOption.TextChanges
        };
    }

    private static View BadgeRow(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(Label(label, 11, CustomerUi.Muted), 0, 0);
        grid.Add(Badge(value, Color.FromArgb("#EEF1F4"), CustomerUi.Dark), 1, 0);
        return grid;
    }

    private static View DetailLine(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(Label(label, 12, CustomerUi.Dark), 0, 0);
        grid.Add(Label(value, 12, CustomerUi.Muted, FontAttributes.Bold), 1, 0);
        return grid;
    }

    private static View Badge(string text, Color background, Color textColor)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = background,
            Padding = new Thickness(9, 4),
            Content = new Label
            {
                Text = text,
                FontSize = CustomerUi.CaptionSize,
                FontFamily = CustomerUi.FontCaptionBold,
                TextColor = textColor,
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static string? Clean(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
        var searchEntry = new Entry { Placeholder = "Search Help", FontSize = 13, Margin = new Thickness(8, -10, 0, -10) };
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
            FontSize = 13,
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
    private bool _showHistory;

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

        body.Add(Tabs());

        var visibleRequests = _requests
            .Where(request => IsHistoryRequest(request) == _showHistory)
            .OrderByDescending(request => request.CreatedAt)
            .ToArray();
        if (visibleRequests.Length == 0)
        {
            body.Add(Card(Label(
                _showHistory
                    ? "No completed or cancelled repairs yet."
                    : "No upcoming or active repairs right now.",
                12,
                CustomerUi.Muted)));
        }
        else
        {
            foreach (var request in visibleRequests)
            {
                body.Add(ScheduleCard(request));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Schedule");
    }

    private View Tabs()
    {
        var grid = new Grid
        {
            HeightRequest = 46,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 0
        };

        grid.Add(TabButton("Upcoming", !_showHistory, () =>
        {
            _showHistory = false;
            Render();
        }), 0, 0);
        grid.Add(TabButton("History", _showHistory, () =>
        {
            _showHistory = true;
            Render();
        }), 1, 0);

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static Button TabButton(string text, bool selected, Action select)
    {
        return new Button
        {
            Text = text,
            Command = new Command(select),
            BackgroundColor = selected ? CustomerUi.Orange : Colors.White,
            TextColor = selected ? Colors.White : CustomerUi.Dark,
            CornerRadius = 7,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            FontSize = CustomerUi.BodySize,
            Padding = new Thickness(12, 0)
        };
    }

    private static bool IsHistoryRequest(ServiceRequestDto request)
    {
        return request.CurrentStatus.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
               request.CurrentStatus.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
               request.CurrentStatus.Equals("rejected", StringComparison.OrdinalIgnoreCase);
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
    private string _filter = "all";
    private string _searchText = string.Empty;
    private bool _isLoading;

    public CustomerMessagesPage()
    {
        Title = "Messages";
        Render("Loading conversations...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _conversations = await CustomerApiClient.GetConversationsAsync();
            Render();
        }
        catch (ApiSessionExpiredException)
        {
        }
        catch (Exception ex)
        {
            Render($"Messages could not be loaded. Check your connection and try again. {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 8, 16, 18),
            Spacing = 12,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Messages", false));
        var subtitle = Label("Booking conversations with your repair shop and assigned mechanic.", 11, CustomerUi.Muted);
        subtitle.HorizontalTextAlignment = TextAlignment.Center;
        body.Add(subtitle);

        var search = new Entry
        {
            Text = _searchText,
            Placeholder = "Search messages or booking ID",
            ReturnType = ReturnType.Search,
            BackgroundColor = Colors.White,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontFamily = CustomerUi.FontBody,
            FontSize = CustomerUi.BodySize,
            HeightRequest = 48
        };
        search.Completed += (_, _) =>
        {
            _searchText = search.Text?.Trim() ?? string.Empty;
            Render();
        };
        body.Add(Card(search, Colors.White, 8, new Thickness(10, 0)));
        body.Add(FilterTabs());

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        var visibleConversations = _conversations
            .Where(MatchesFilter)
            .Where(MatchesSearch)
            .ToArray();
        if (visibleConversations.Length == 0)
        {
            body.Add(Card(Label(
                string.IsNullOrWhiteSpace(_searchText)
                    ? "No booking conversations are available in this view yet."
                    : "No conversations match your search.",
                12,
                CustomerUi.Muted)));
        }
        else
        {
            foreach (var conversation in visibleConversations)
            {
                body.Add(MessageRow(conversation));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Messages");
    }

    private View FilterTabs()
    {
        var grid = new Grid
        {
            HeightRequest = 44,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(FilterButton("All", "all"), 0, 0);
        grid.Add(FilterButton("Shops", "shop"), 1, 0);
        grid.Add(FilterButton("Mechanics", "mechanic"), 2, 0);
        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private Button FilterButton(string label, string value)
    {
        var selected = _filter == value;
        return new Button
        {
            Text = label,
            BackgroundColor = selected ? CustomerUi.Orange : Colors.White,
            TextColor = selected ? Colors.White : CustomerUi.Dark,
            FontFamily = CustomerUi.FontDisplay,
            FontAttributes = FontAttributes.Bold,
            FontSize = CustomerUi.BodySize,
            CornerRadius = 7,
            Command = new Command(() =>
            {
                _filter = value;
                Render();
            })
        };
    }

    private bool MatchesFilter(ConversationSummaryDto conversation)
    {
        return _filter switch
        {
            "shop" => IsShopConversation(conversation),
            "mechanic" => IsMechanicConversation(conversation),
            _ => true
        };
    }

    private bool MatchesSearch(ConversationSummaryDto conversation)
    {
        if (string.IsNullOrWhiteSpace(_searchText))
        {
            return true;
        }

        return conversation.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ||
               (conversation.Subtitle?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (conversation.LastMessageText?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
               (conversation.RequestId?.ToString(CultureInfo.InvariantCulture).Contains(_searchText, StringComparison.OrdinalIgnoreCase) ?? false);
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

        var avatar = ConversationAvatar(conversation);
        grid.Add(avatar, 0, 0);

        var text = new VerticalStackLayout { Spacing = 5, Margin = new Thickness(10, 0, 8, 0) };
        var titleRow = new HorizontalStackLayout { Spacing = 6 };
        titleRow.Add(Label(conversation.Title, 13, CustomerUi.Dark, FontAttributes.Bold));
        titleRow.Add(TypeBadge(conversation));
        text.Add(titleRow);
        var bookingLine = Label(
            conversation.RequestId is null
                ? conversation.Subtitle ?? "BikeMate conversation"
                : $"BM-{conversation.RequestId:000000} | {FormatStatus(conversation.BookingStatus ?? "pending")}",
            10,
            CustomerUi.Muted);
        text.Add(bookingLine);
        var preview = Label(Preview(conversation.LastMessageText ?? conversation.Subtitle ?? "Open conversation"), 11, CustomerUi.Dark);
        preview.LineBreakMode = LineBreakMode.TailTruncation;
        preview.MaxLines = 2;
        text.Add(preview);
        grid.Add(text, 1, 0);

        var meta = new VerticalStackLayout { Spacing = 8, HorizontalOptions = LayoutOptions.End };
        meta.Add(Label(FriendlyTime(conversation.LastMessageAt), 9, CustomerUi.Muted));
        if (conversation.UnreadCount > 0)
        {
            meta.Add(new Border
            {
                BackgroundColor = CustomerUi.Orange,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 10 },
                HeightRequest = 20,
                MinimumWidthRequest = 20,
                Padding = new Thickness(6, 1),
                HorizontalOptions = LayoutOptions.End,
                Content = new Label
                {
                    Text = conversation.UnreadCount > 99 ? "99+" : conversation.UnreadCount.ToString(CultureInfo.InvariantCulture),
                    TextColor = Colors.White,
                    FontSize = CustomerUi.CaptionSize,
                    FontFamily = CustomerUi.FontCaptionBold,
                    HorizontalTextAlignment = TextAlignment.Center
                }
            });
        }
        grid.Add(meta, 2, 0);

        var row = Card(grid, Colors.White, 8, new Thickness(12));
        row.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(async () => await Shell.Current.Navigation.PushAsync(new CustomerChatPage(conversation.ConversationId)))
        });
        return row;
    }

    private static View ConversationAvatar(ConversationSummaryDto conversation)
    {
        if (!string.IsNullOrWhiteSpace(conversation.OtherProfileImageUrl))
        {
            return new Border
            {
                WidthRequest = 50,
                HeightRequest = 50,
                Stroke = CustomerUi.Border,
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = 25 },
                Content = new Image
                {
                    Source = conversation.OtherProfileImageUrl,
                    Aspect = Aspect.AspectFill
                }
            };
        }

        return Avatar(Initials(conversation.Title), 50, IsShopConversation(conversation) ? CustomerUi.LightOrange : Color.FromArgb("#EEF1F4"));
    }

    private static View TypeBadge(ConversationSummaryDto conversation)
    {
        var isShop = IsShopConversation(conversation);
        return new Border
        {
            BackgroundColor = isShop ? CustomerUi.LightOrange : Color.FromArgb("#EEF1F4"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(7, 2),
            Content = Label(isShop ? "SHOP" : "MECHANIC", 9, isShop ? CustomerUi.Orange : CustomerUi.Dark, FontAttributes.Bold)
        };
    }

    private static bool IsShopConversation(ConversationSummaryDto conversation)
    {
        return conversation.ConversationType == "booking_shop";
    }

    private static bool IsMechanicConversation(ConversationSummaryDto conversation)
    {
        return conversation.ConversationType is "booking_mechanic" or "emergency_request" or "service_request";
    }

    private static string Preview(string text)
    {
        return string.Join(" ", text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)).Trim();
    }

    private static string FriendlyTime(DateTime? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        var local = value.Value.ToLocalTime();
        return local.Date == DateTime.Today
            ? local.ToString("h:mm tt", CultureInfo.InvariantCulture)
            : local.Year == DateTime.Today.Year
                ? local.ToString("MMM d", CultureInfo.InvariantCulture)
                : local.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
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
    private bool _isSending;

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
            await CustomerApiClient.MarkConversationReadAsync(_conversationId);
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
            Padding = new Thickness(14, 10),
            BackgroundColor = Colors.White,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };

        grid.Add(new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 13,
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
        who.Add(Label(_conversation?.Title ?? "Conversation", 13, CustomerUi.Dark, FontAttributes.Bold));
        who.Add(Label(PartnerCaption(), 10, CustomerUi.Muted));
        grid.Add(who, 2, 0);

        grid.Add(HeaderIcon("Info", BookingInfo()), 3, 0);

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static Button HeaderIcon(string text, string title)
    {
        return new Button
        {
            Text = text,
            FontSize = CustomerUi.CaptionSize,
            FontFamily = CustomerUi.FontCaptionBold,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Orange,
            WidthRequest = text.Length > 2 ? 58 : 38,
            Command = new Command(async () => await Shell.Current.DisplayAlertAsync("Contact", title, "OK"))
        };
    }

    private View BuildMessages(string? banner)
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 12, 16, 20),
            Spacing = 10,
            BackgroundColor = CustomerUi.Page
        };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        if (_conversation?.RequestId is not null)
        {
            body.Add(BookingContextCard());
        }

        if (_messages.Count == 0)
        {
            body.Add(Card(Label("No messages yet. Send a message to begin the conversation.", 12, CustomerUi.Muted)));
            return body;
        }

        DateTime? currentDate = null;
        foreach (var message in _messages.OrderBy(x => x.CreatedAt))
        {
            var localDate = message.CreatedAt.ToLocalTime().Date;
            if (currentDate != localDate)
            {
                currentDate = localDate;
                body.Add(DateDivider(localDate));
            }

            body.Add(Bubble(message, message.SenderUserId == _customer?.UserId));
        }

        return body;
    }

    private static View Bubble(MessageDto message, bool mine)
    {
        var stack = new VerticalStackLayout { Spacing = 6 };
        if (!mine && IsAutomatedMessage(message.MessageText))
        {
            stack.Add(Label("AUTOMATED BOOKING UPDATE", 9, CustomerUi.Orange, FontAttributes.Bold));
        }

        if (!string.IsNullOrWhiteSpace(message.MessageText))
        {
            stack.Add(Label(message.MessageText, 12, mine ? Colors.White : CustomerUi.Dark));
        }

        if (!string.IsNullOrWhiteSpace(message.AttachmentUrl))
        {
            stack.Add(AttachmentPreview(message.AttachmentUrl, mine));
        }

        var time = Label(
            message.CreatedAt.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture),
            9,
            mine ? Color.FromArgb("#FFF0E9") : CustomerUi.Muted);
        time.HorizontalTextAlignment = mine ? TextAlignment.End : TextAlignment.Start;
        stack.Add(time);

        var bubble = Card(stack, mine ? CustomerUi.Orange : Colors.White, 8, new Thickness(12, 9));
        bubble.HorizontalOptions = mine ? LayoutOptions.End : LayoutOptions.Start;
        bubble.MaximumWidthRequest = 300;
        return bubble;
    }

    private static View AttachmentPreview(string attachmentUrl, bool mine)
    {
        var textColor = mine ? Colors.White : CustomerUi.Dark;
        var fileName = Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var uri)
            ? System.IO.Path.GetFileName(uri.LocalPath)
            : System.IO.Path.GetFileName(attachmentUrl);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "Attachment";
        }

        View preview = IsImageUrl(attachmentUrl)
            ? new Image
            {
                Source = attachmentUrl,
                HeightRequest = 150,
                WidthRequest = 220,
                Aspect = Aspect.AspectFill,
                BackgroundColor = Color.FromArgb("#F1F1F1")
            }
            : new HorizontalStackLayout
            {
                Spacing = 8,
                Children =
                {
                    Label("File", 11, textColor, FontAttributes.Bold),
                    Label(fileName, 11, textColor)
                }
            };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) =>
        {
            if (Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var openUri))
            {
                await Launcher.Default.OpenAsync(openUri);
            }
        };
        preview.GestureRecognizers.Add(tap);
        return preview;
    }

    private static bool IsImageUrl(string attachmentUrl)
    {
        var extension = System.IO.Path.GetExtension(Uri.TryCreate(attachmentUrl, UriKind.Absolute, out var uri) ? uri.LocalPath : attachmentUrl);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    private View BuildComposer()
    {
        _messageEntry = new Entry
        {
            Text = _draftMessage,
            Placeholder = "Write a message",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            FontFamily = CustomerUi.FontBody,
            FontSize = CustomerUi.BodySize
        };
        _messageEntry.TextChanged += (_, e) => _draftMessage = e.NewTextValue ?? string.Empty;

        var stack = new VerticalStackLayout
        {
            Padding = new Thickness(14, 8, 14, 12),
            Spacing = 8,
            BackgroundColor = Colors.White
        };
        var attachments = new HorizontalStackLayout
        {
            Spacing = 8,
            Children =
            {
                CompactAction("Attach file", new Command(async () => await PickAndSendAttachmentAsync(false))),
                CompactAction("Add photo", new Command(async () => await PickAndSendAttachmentAsync(true)))
            }
        };
        stack.Add(attachments);

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        grid.Add(new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Color.FromArgb("#F8F8F8"),
            Padding = new Thickness(10, 0),
            Content = _messageEntry
        }, 0, 0);
        grid.Add(new Button
        {
            Text = _isSending ? "Sending" : "Send",
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            WidthRequest = 76,
            FontFamily = CustomerUi.FontDisplay,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await SendAsync())
        }, 1, 0);
        stack.Add(grid);
        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            BackgroundColor = Colors.White,
            Content = stack
        };
    }

    private View BookingContextCard()
    {
        var stack = new VerticalStackLayout { Spacing = 7 };
        var top = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        top.Add(Label($"Booking BM-{_conversation!.RequestId:000000}", 12, CustomerUi.Dark, FontAttributes.Bold), 0, 0);
        top.Add(new Border
        {
            BackgroundColor = CustomerUi.LightOrange,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(8, 3),
            Content = Label(FormatStatus(_conversation.BookingStatus ?? "pending"), 9, CustomerUi.Orange, FontAttributes.Bold)
        }, 1, 0);
        stack.Add(top);
        stack.Add(Label(_conversation.Subtitle ?? "Booking conversation", 10, CustomerUi.Muted));
        if (_conversation.ScheduledAt is not null)
        {
            stack.Add(Label(
                $"Scheduled {_conversation.ScheduledAt.Value.ToLocalTime():MMM d, yyyy 'at' h:mm tt}",
                10,
                CustomerUi.Dark));
        }

        var openBooking = GhostButton("View booking details", new Command(async () =>
            await Shell.Current.GoToAsync($"{nameof(BookingDetailsPage)}?requestId={_conversation.RequestId}")));
        openBooking.HeightRequest = 38;
        stack.Add(openBooking);
        return Card(stack, Colors.White, 8, new Thickness(12));
    }

    private static View DateDivider(DateTime date)
    {
        var text = date == DateTime.Today
            ? "Today"
            : date == DateTime.Today.AddDays(-1)
                ? "Yesterday"
                : date.ToString("MMM d, yyyy", CultureInfo.InvariantCulture);
        var label = Label(text, 9, CustomerUi.Muted, FontAttributes.Bold);
        label.HorizontalTextAlignment = TextAlignment.Center;
        label.HorizontalOptions = LayoutOptions.Fill;
        return label;
    }

    private static Button CompactAction(string text, ICommand command)
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
            HeightRequest = 34,
            Padding = new Thickness(12, 0),
            FontFamily = CustomerUi.FontBody,
            FontSize = CustomerUi.CaptionSize
        };
    }

    private string PartnerCaption()
    {
        var partner = _conversation?.ConversationType == "booking_shop" ? "Repair shop" : "Assigned mechanic";
        return _conversation?.RequestId is null
            ? partner
            : $"{partner} | BM-{_conversation.RequestId:000000}";
    }

    private string BookingInfo()
    {
        if (_conversation?.RequestId is null)
        {
            return _conversation?.Title ?? "BikeMate conversation";
        }

        return
            $"Booking BM-{_conversation.RequestId:000000}\n" +
            $"Partner: {_conversation.Title}\n" +
            $"Status: {FormatStatus(_conversation.BookingStatus ?? "pending")}\n" +
            $"{_conversation.Subtitle}";
    }

    private static bool IsAutomatedMessage(string text)
    {
        return text.StartsWith("Booking BM-", StringComparison.Ordinal) ||
               (text.StartsWith("Hi ", StringComparison.Ordinal) &&
                text.Contains("assigned to booking BM-", StringComparison.Ordinal));
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
            _isSending = true;
            Render();
            await CustomerApiClient.SendMessageAsync(_conversationId, text);
            _draftMessage = string.Empty;
            if (_messageEntry is not null)
            {
                _messageEntry.Text = string.Empty;
            }
            _isSending = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Message not sent", ex.Message, "OK");
        }
        finally
        {
            _isSending = false;
        }
    }

    private async Task PickAndSendAttachmentAsync(bool imageOnly)
    {
        if (_conversationId <= 0 || _isSending)
        {
            return;
        }

        try
        {
            var file = await FilePicker.Default.PickAsync(imageOnly
                ? PickOptions.Images
                : new PickOptions
                {
                    PickerTitle = "Select an attachment",
                    FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        [DevicePlatform.Android] = new[]
                        {
                            "image/jpeg",
                            "image/png",
                            "image/webp",
                            "application/pdf",
                            "text/plain",
                            "application/msword",
                            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        }
                    })
                });
            if (file is null)
            {
                return;
            }

            _isSending = true;
            Render("Uploading attachment...");
            var uploaded = await CustomerApiClient.UploadFileAsync(file);
            var text = string.IsNullOrWhiteSpace(_draftMessage)
                ? uploaded.FileName
                : _draftMessage.Trim();
            await CustomerApiClient.SendMessageAsync(_conversationId, text, uploaded.Url);
            _draftMessage = string.Empty;
            if (_messageEntry is not null)
            {
                _messageEntry.Text = string.Empty;
            }

            _isSending = false;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Attachment not sent", ex.Message, "OK");
        }
        finally
        {
            _isSending = false;
        }
    }
}

public sealed class CustomerPaymentsPage : CustomerPageBase
{
    private IReadOnlyList<PaymentDto> _payments = [];
    private IReadOnlyList<ServiceRequestDto> _requests = [];
    private bool _showHistory;

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
        body.Add(Header("Payments", false));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(PaymentTabs());

        var visiblePayments = _payments
            .Where(payment => IsHistoricalPayment(payment) == _showHistory)
            .OrderByDescending(payment => payment.PaidAt ?? payment.CreatedAt)
            .ToArray();
        if (visiblePayments.Length == 0)
        {
            body.Add(Card(Label(
                _showHistory
                    ? "No completed, cancelled, failed, or refunded payments yet."
                    : "No payments are currently awaiting completion.",
                12,
                CustomerUi.Muted)));
        }
        else
        {
            foreach (var payment in visiblePayments)
            {
                var request = _requests.FirstOrDefault(x => x.RequestId == payment.RequestId);
                body.Add(PaymentRow(payment, request));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Payments");
    }

    private View PaymentTabs()
    {
        var grid = new Grid
        {
            HeightRequest = 46,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 0
        };

        grid.Add(PaymentTabButton("Ongoing", !_showHistory, () =>
        {
            _showHistory = false;
            Render();
        }), 0, 0);
        grid.Add(PaymentTabButton("History", _showHistory, () =>
        {
            _showHistory = true;
            Render();
        }), 1, 0);

        return new Border
        {
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    private static Button PaymentTabButton(string text, bool selected, Action select)
    {
        return new Button
        {
            Text = text,
            Command = new Command(select),
            BackgroundColor = selected ? CustomerUi.Orange : Colors.White,
            TextColor = selected ? Colors.White : CustomerUi.Dark,
            CornerRadius = 7,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay,
            FontSize = CustomerUi.BodySize,
            Padding = new Thickness(12, 0)
        };
    }

    private static bool IsHistoricalPayment(PaymentDto payment)
    {
        return payment.Status.Equals("paid", StringComparison.OrdinalIgnoreCase) ||
               payment.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
               payment.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase) ||
               payment.Status.Equals("refunded", StringComparison.OrdinalIgnoreCase);
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
        var selectedService = BookingDraft.SelectedService();
        var hasPricedBooking = BookingDraft.RequestId > 0 &&
            BookingDraft.SelectedShopId is not null &&
            BookingDraft.SelectedShopServiceId is not null;
        var body = new VerticalStackLayout { Padding = new Thickness(16), Spacing = 14, BackgroundColor = CustomerUi.Page };
        body.Add(Header("Payments"));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label("Secure payment", 16, CustomerUi.Dark, FontAttributes.Bold),
                Label(hasPricedBooking
                    ? $"Checkout is based on the selected shop service: {selectedService?.ServiceName ?? "Selected service"} at {Money(selectedService?.BasePrice ?? 0m)}."
                    : "Choose a repair shop and service first so checkout uses the correct shop price.",
                    12,
                    hasPricedBooking ? CustomerUi.Dark : CustomerUi.Muted),
                Label("BikeMate opens PayMongo hosted checkout for card, GCash, PayMaya, and other supported payment methods. Payment status is verified from the backend after you return.", 12, CustomerUi.Muted),
                Label("No payment keys or card details are stored in the mobile app.", 12, CustomerUi.Muted)
            }
        }, Colors.White, 8, new Thickness(14)));

        body.Add(OrangeButton(hasPricedBooking ? "Continue to secure payment" : "Choose repair shop", new Command(async () =>
        {
            if (!hasPricedBooking)
            {
                await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
                return;
            }

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
            FontSize = 11,
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
                if (!string.Equals(_payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        _payment = await CustomerApiClient.RefreshPaymentAsync(_payment.PaymentId);
                        if (string.Equals(_payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
                        {
                            _request = await CustomerApiClient.GetRequestAsync(_payment.RequestId);
                            banner = AppendBanner(banner, "Payment confirmed by BikeMate.");
                        }
                    }
                    catch (Exception refreshError)
                    {
                        banner = AppendBanner(banner, $"BikeMate could not verify PayMongo yet. {refreshError.Message}");
                    }
                }
            }

            Render(banner);
        }
        catch (Exception ex)
        {
            var message = $"Connect the API to load payment details. {ex.Message}";
            Render(string.IsNullOrWhiteSpace(banner) ? message : $"{banner}\n{message}");
        }
    }

    private static string? AppendBanner(string? current, string message)
    {
        return string.IsNullOrWhiteSpace(current) ? message : $"{current}\n{message}";
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
                "Payment confirmed by BikeMate.",
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
        return string.Equals(_payment?.Status, "paid", StringComparison.OrdinalIgnoreCase);
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
        body.Add(new Label { Text = $"For {name}\nPaid on {paidDate}", FontSize = 13, TextColor = CustomerUi.Muted, HorizontalTextAlignment = TextAlignment.Center });
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
        body.Add(BookingVisuals.PickerRow("Region / Main Area", BookingDraft.RegionOptions, BookingDraft.Region, selected =>
        {
            BookingDraft.SetRegion(selected);
            Render();
            return Task.CompletedTask;
        }));
        body.Add(BookingVisuals.PickerRow("Your Location", BookingDraft.LocationOptions, BookingDraft.LocationName, async selected =>
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
        }));

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
        body.Add(OrangeButton("Track Mechanic", new Command(async () => await OpenTrackMechanicAsync())));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private async Task OpenTrackMechanicAsync()
    {
        if (_requestId <= 0)
        {
            return;
        }

        if (!await CustomerPaymentGate.EnsurePaidOrRedirectAsync(this, _requestId))
        {
            return;
        }

        await Shell.Current.GoToAsync($"{nameof(TrackMechanicPage)}?requestId={_requestId}");
    }
}

public sealed class TrackMechanicPage : CustomerPageBase, IQueryAttributable
{
    private int _requestId;
    private ServiceRequestDto? _request;
    private LiveLocationDto? _location;
    private PaymentDto? _payment;
    private IDispatcherTimer? _timer;
    private bool _isLoading;

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
            BookingDraft.RequestId = requestId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        StartLiveRefresh();
        await LoadAsync();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        base.OnDisappearing();
    }

    private void StartLiveRefresh()
    {
        if (_timer is null)
        {
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(8);
            _timer.Tick += async (_, _) => await LoadAsync(true);
        }

        if (!_timer.IsRunning)
        {
            _timer.Start();
        }
    }

    private async Task LoadAsync(bool silent = false)
    {
        if (_isLoading)
        {
            return;
        }

        _isLoading = true;
        try
        {
            _request = _requestId > 0
                ? await CustomerApiClient.GetRequestAsync(_requestId)
                : (await CustomerApiClient.GetMyRequestsAsync()).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            _requestId = _request?.RequestId ?? _requestId;
            BookingDraft.RequestId = _requestId;
            if (_requestId > 0 && !await CustomerPaymentGate.EnsurePaidOrRedirectAsync(this, _requestId))
            {
                return;
            }

            if (_requestId > 0)
            {
                _payment = await CustomerApiClient.GetLatestPaymentForRequestAsync(_requestId);
                _location = await CustomerApiClient.GetLatestRequestLocationAsync(_requestId);
            }

            Render();
        }
        catch (Exception ex)
        {
            if (!silent)
            {
                Render($"Connect the API to load tracking data. {ex.Message}");
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void Render(string? banner = null)
    {
        var request = _request;
        var status = request?.CurrentStatus ?? "pending";
        var trackingStatus = string.Equals(status, "paid", StringComparison.OrdinalIgnoreCase) &&
                             !string.IsNullOrWhiteSpace(request?.MechanicName)
            ? "accepted"
            : status;
        var destinationLat = request?.ServiceLatitude ?? BookingDraft.Latitude;
        var destinationLng = request?.ServiceLongitude ?? BookingDraft.Longitude;
        var destination = request?.ServiceLocationAddress ?? BookingDraft.ConfirmationAddress();
        var mechanicLat = _location?.Latitude ?? destinationLat ?? 14.599512m;
        var mechanicLng = _location?.Longitude ?? destinationLng ?? 120.984222m;
        var hasLiveLocation = _location is not null;
        var route = BuildRouteSummary(mechanicLat, mechanicLng, destinationLat, destinationLng, destination, hasLiveLocation);
        var amount = request is null ? 0m : request.FinalTotal > 0 ? request.FinalTotal : request.EstimatedTotal;

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 0, 14, 16),
            Spacing = 12,
            BackgroundColor = CustomerUi.Page
        };
        body.Add(Header("Track Mechanic", true, "Refresh", new Command(async () => await LoadAsync())));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(TrackingMapCard(mechanicLat, mechanicLng, destinationLat, destinationLng, hasLiveLocation, route));
        body.Add(StatusCard(trackingStatus, request, hasLiveLocation));
        body.Add(RouteStats(route));
        body.Add(SectionCard("Repair details",
        [
            DetailRow("Service", request?.ServiceName ?? "Bike repair"),
            DetailRow("Concern", request?.IssueDescription ?? "Repair request"),
            DetailRow("Shop", request?.ShopName ?? "Waiting for shop assignment"),
            DetailRow("Mechanic", request?.MechanicName ?? "Waiting for mechanic assignment")
        ]));
        body.Add(SectionCard("Schedule and payment",
        [
            DetailRow("Booking ID", _requestId > 0 ? $"BM-{_requestId:000000}" : "Pending"),
            DetailRow("Date", request?.ScheduledAt?.ToLocalTime().ToString("MMM d, yyyy", CultureInfo.InvariantCulture) ?? BookingDraft.ScheduledAt.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)),
            DetailRow("Time", request?.ScheduledAt?.ToLocalTime().ToString("h:mm tt", CultureInfo.InvariantCulture) ?? BookingDraft.ScheduledAt.ToString("h:mm tt", CultureInfo.InvariantCulture)),
            DetailRow("Payment", _payment is null ? "Checking" : FormatStatus(_payment.Status)),
            DetailRow("Total", amount > 0 ? Money(amount) : "To be finalized")
        ]));
        body.Add(Timeline(trackingStatus));
        body.Add(ActionCard(trackingStatus, destination));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private static View TrackingMapCard(
        decimal mechanicLatitude,
        decimal mechanicLongitude,
        decimal? destinationLatitude,
        decimal? destinationLongitude,
        bool hasLiveLocation,
        (string Origin, string Destination, string Distance, string Time) route)
    {
        var map = new Grid { HeightRequest = 280, BackgroundColor = Color.FromArgb("#EEF1F4") };
        var source = hasLiveLocation && destinationLatitude is not null && destinationLongitude is not null
            ? BookingVisuals.GoogleDirectionsSource(mechanicLatitude, mechanicLongitude, destinationLatitude.Value, destinationLongitude.Value)
            : BookingVisuals.GoogleMapSource(destinationLatitude ?? mechanicLatitude, destinationLongitude ?? mechanicLongitude);
        map.Add(new WebView
        {
            Source = source,
            HeightRequest = 280
        });

        var badge = new VerticalStackLayout
        {
            Spacing = 2,
            Padding = new Thickness(12, 8),
            BackgroundColor = Color.FromRgba(255, 255, 255, 0.92),
            Children =
            {
                Label(hasLiveLocation ? "Live mechanic route" : "Waiting for live mechanic location", 12, CustomerUi.Dark, FontAttributes.Bold),
                Label($"{route.Distance} • {route.Time}", 10, hasLiveLocation ? CustomerUi.Orange : CustomerUi.Muted)
            }
        };
        map.Add(new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Content = badge,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(12)
        });

        return Card(map, Colors.White, 8, new Thickness(0));
    }

    private static View StatusCard(string status, ServiceRequestDto? request, bool hasLiveLocation)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 12
        };
        grid.Add(Avatar(Initials(request?.MechanicName ?? "BM"), 48, CustomerUi.LightOrange), 0, 0);

        var text = new VerticalStackLayout { Spacing = 3 };
        text.Add(Label(request?.MechanicName ?? "Mechanic not assigned yet", 14, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(StatusMessage(status, hasLiveLocation), 11, CustomerUi.Muted));
        grid.Add(text, 1, 0);
        grid.Add(Pill(FormatStatus(status), StatusColor(status), Colors.White), 2, 0);
        return Card(grid, Colors.White, 8, new Thickness(14));
    }

    private static View RouteStats((string Origin, string Destination, string Distance, string Time) route)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(RouteLine("From", route.Origin));
        stack.Add(RouteLine("To", route.Destination));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(Stat("Distance", route.Distance), 0, 0);
        stats.Add(Stat("ETA", route.Time), 1, 0);
        stack.Add(stats);

        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private static View SectionCard(string title, IReadOnlyList<View> rows)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        stack.Add(Label(title, 13, CustomerUi.Dark, FontAttributes.Bold));
        foreach (var row in rows)
        {
            stack.Add(row);
        }

        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private static View DetailRow(string label, string value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        grid.Add(Label(label, 11, CustomerUi.Muted), 0, 0);
        var valueLabel = Label(value, 11, CustomerUi.Dark, FontAttributes.Bold);
        valueLabel.HorizontalTextAlignment = TextAlignment.End;
        grid.Add(valueLabel, 1, 0);
        return grid;
    }

    private static View Timeline(string status)
    {
        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label("Progress", 13, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(TimelineRow("Payment confirmed", "Tracking is available after secure payment.", IsAtLeast(status, "paid")));
        stack.Add(TimelineRow("Mechanic assigned", "A shop/mechanic accepted the booking.", IsAtLeast(status, "accepted")));
        stack.Add(TimelineRow("Mechanic en route", "Live route updates appear when location is shared.", IsAtLeast(status, "en_route")));
        stack.Add(TimelineRow("Arrived", "Mechanic reached the service location.", IsAtLeast(status, "arrived")));
        stack.Add(TimelineRow("Repair in progress", "The repair work has started.", IsAtLeast(status, "in_progress")));
        stack.Add(TimelineRow("Completed", "Review your repair and keep the receipt.", IsAtLeast(status, "completed")));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View ActionCard(string status, string destination)
    {
        var stack = new VerticalStackLayout { Spacing = 10 };
        if (IsAtLeast(status, "completed"))
        {
            stack.Add(Label("Repair completed", 13, CustomerUi.Dark, FontAttributes.Bold));
            stack.Add(Label("Please review your repair experience. Your feedback helps BikeMate improve shop and mechanic quality.", 11, CustomerUi.Muted));
            stack.Add(OrangeButton("Review repair", new Command(async () =>
            {
                BookingDraft.RequestId = _requestId;
                await Shell.Current.GoToAsync(nameof(BookingRatingPage));
            })));
        }
        else if (string.Equals(status, "cancelled", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(status, "rejected", StringComparison.OrdinalIgnoreCase))
        {
            stack.Add(Label("Booking ended", 13, CustomerUi.Dark, FontAttributes.Bold));
            stack.Add(Label("This request is no longer active. You can return home and book another service.", 11, CustomerUi.Muted));
            stack.Add(OrangeButton("Return home", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));
        }
        else
        {
            var buttons = new Grid { ColumnSpacing = 8 };
            buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            buttons.Add(OrangeButton("Refresh status", new Command(async () => await LoadAsync())), 0, 0);
            buttons.Add(GhostButton("Open map", new Command(async () => await BookingVisuals.OpenGoogleMapsAsync(destination))), 1, 0);
            stack.Add(buttons);
            stack.Add(Label("The page refreshes automatically while open when the mechanic app shares live location.", 10, CustomerUi.Muted));
        }

        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private static View RouteLine(string label, string value)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(Label(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(Label(value, 12, CustomerUi.Dark));
        return stack;
    }

    private static View Stat(string label, string value)
    {
        return new Border
        {
            BackgroundColor = Color.FromArgb("#F7F7F7"),
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(12, 8),
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    Label(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold),
                    Label(value, 15, CustomerUi.Dark, FontAttributes.Bold)
                }
            }
        };
    }

    private static View TimelineRow(string title, string subtitle, bool done)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };
        grid.Add(new Border
        {
            WidthRequest = 18,
            HeightRequest = 18,
            Stroke = done ? CustomerUi.Orange : CustomerUi.Border,
            BackgroundColor = done ? CustomerUi.Orange : Colors.White,
            StrokeShape = new RoundRectangle { CornerRadius = 9 },
            Content = new Label
            {
                Text = done ? "✓" : "",
                TextColor = Colors.White,
                FontSize = 10,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2 };
        text.Add(Label(title, 11, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(Label(subtitle, 9, CustomerUi.Muted));
        grid.Add(text, 1, 0);
        return grid;
    }

    private static View Pill(string text, Color background, Color textColor)
    {
        return new Border
        {
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            BackgroundColor = background,
            Padding = new Thickness(10, 5),
            Content = new Label
            {
                Text = text,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                TextColor = textColor,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            }
        };
    }

    private static (string Origin, string Destination, string Distance, string Time) BuildRouteSummary(
        decimal mechanicLatitude,
        decimal mechanicLongitude,
        decimal? destinationLatitude,
        decimal? destinationLongitude,
        string destination,
        bool hasLiveLocation)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            destination = "Service location";
        }

        if (!hasLiveLocation || destinationLatitude is null || destinationLongitude is null)
        {
            return ("Mechanic live location", destination, "--", "--");
        }

        var km = DistanceKm(mechanicLatitude, mechanicLongitude, destinationLatitude.Value, destinationLongitude.Value);
        var minutes = Math.Max(1, (int)Math.Ceiling((double)km / 18d * 60d));
        return ("Mechanic live location", destination, $"{km:0.##} km", $"{minutes} min");
    }

    private static decimal DistanceKm(decimal latitudeA, decimal longitudeA, decimal latitudeB, decimal longitudeB)
    {
        const double earthRadiusKm = 6371d;
        var lat1 = ToRadians((double)latitudeA);
        var lat2 = ToRadians((double)latitudeB);
        var deltaLat = ToRadians((double)(latitudeB - latitudeA));
        var deltaLng = ToRadians((double)(longitudeB - longitudeA));
        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round((decimal)(earthRadiusKm * c), 2);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180d;
    }

    private static bool IsAtLeast(string status, string target)
    {
        return StatusRank(status) >= StatusRank(target);
    }

    private static int StatusRank(string status)
    {
        return status switch
        {
            "payment_pending" => 1,
            "paid" => 2,
            "pending" => 2,
            "accepted" => 3,
            "en_route" => 4,
            "arrived" => 5,
            "in_progress" => 6,
            "completed" => 7,
            "cancelled" => 7,
            "rejected" => 7,
            _ => 0
        };
    }

    private static Color StatusColor(string status)
    {
        return status switch
        {
            "completed" => Color.FromArgb("#1D7D46"),
            "cancelled" or "rejected" => Color.FromArgb("#9F2A2A"),
            "en_route" or "arrived" or "in_progress" => CustomerUi.Orange,
            _ => Color.FromArgb("#6E6E6E")
        };
    }

    private static string StatusMessage(string status, bool hasLiveLocation)
    {
        return status switch
        {
            "payment_pending" => "Waiting for secure payment before tracking starts.",
            "paid" or "pending" => "Waiting for shop or mechanic confirmation.",
            "accepted" => hasLiveLocation ? "Mechanic accepted and location is active." : "Mechanic accepted. Waiting for live location.",
            "en_route" => hasLiveLocation ? "Mechanic is on the way to you." : "Mechanic is on the way. Waiting for location update.",
            "arrived" => "Mechanic has arrived at the service location.",
            "in_progress" => "Repair is currently in progress.",
            "completed" => "Repair is complete and ready for review.",
            "cancelled" => "This booking was cancelled.",
            "rejected" => "This booking was rejected.",
            _ => "Tracking details are being updated."
        };
    }

    private static string Initials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "BM";
        }

        return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }
}

internal static class CustomerPaymentGate
{
    public static async Task<bool> EnsurePaidOrRedirectAsync(Page page, int requestId)
    {
        var payment = await CustomerApiClient.GetLatestPaymentForRequestAsync(requestId);
        if (payment is not null && !string.Equals(payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                payment = await CustomerApiClient.RefreshPaymentAsync(payment.PaymentId);
            }
            catch
            {
                // The payment screen will show the detailed refresh error and the secure checkout link.
            }
        }

        if (payment is not null && string.Equals(payment.Status, "paid", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        await page.DisplayAlertAsync("Payment required", "Complete secure payment before tracking your mechanic.", "OK");
        await Shell.Current.GoToAsync($"{nameof(PaymentCheckoutPage)}?requestId={requestId}");
        return false;
    }
}
