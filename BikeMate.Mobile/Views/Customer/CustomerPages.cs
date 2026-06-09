using System.Globalization;
using System.Windows.Input;
using BikeMate.Core.DTOs;
using BikeMate.Services;
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

    public const string OnlineBikeRepairImage = "https://cdn.pixabay.com/photo/2020/07/29/12/09/bicycle-5446055_1280.jpg";
    public const string HomeIcon = "https://img.icons8.com/ios/50/home--v1.png";
    public const string ScheduleIcon = "https://img.icons8.com/ios/50/calendar--v1.png";
    public const string PaymentsIcon = "https://img.icons8.com/ios/50/wallet--v1.png";
    public const string MessagesIcon = "https://img.icons8.com/ios/50/speech-bubble--v1.png";
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
            FontAttributes = FontAttributes.Bold
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
            HeightRequest = 42
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
            Text = back ? "<" : string.Empty,
            Command = back ? new Command(async () => await Shell.Current.GoToAsync("..")) : null,
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 22,
            WidthRequest = 40
        }, 0, 0);

        grid.Add(new Label
        {
            Text = title,
            FontSize = 15,
            TextColor = CustomerUi.Dark,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 1, 0);

        if (!string.IsNullOrWhiteSpace(right))
        {
            grid.Add(new Button
            {
                Text = right,
                Command = rightCommand,
                BackgroundColor = Colors.Transparent,
                TextColor = CustomerUi.Orange,
                FontAttributes = FontAttributes.Bold,
                WidthRequest = 44
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
            Command = new Command(async () => await DisplayAlertAsync("Emergency", "Call flow placeholder. Add dialer permission when ready.", "OK"))
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
        body.Add(Row("Full Name", fullName));
        body.Add(Separator());
        body.Add(Row("Account ID", _customer is null ? "" : $"CUST-{_customer.ClientId:0000}"));
        body.Add(Separator());
        body.Add(Row("Change Password", ">"));
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
        body.Add(Row("Change Mobile\nNo.", string.IsNullOrWhiteSpace(_customer?.PhoneNumber) ? "Not set >" : $"{_customer.PhoneNumber}>"));
        body.Add(Separator());
        body.Add(Row("Change Email", _customer is null ? "" : $"{_customer.Email}>"));
        body.Add(Separator());
        body.Add(Row("Default Address", _customer?.Addresses.FirstOrDefault(x => x.IsDefault)?.AddressLine ?? "Not set >"));
        body.Add(Separator());
        var motorcycle = _customer?.Motorcycles.FirstOrDefault();
        body.Add(Row("Motorcycle", motorcycle is null ? "Not set >" : $"{motorcycle.Brand} {motorcycle.Model}>"));

        body.Add(new Button
        {
            Text = "Log out",
            BackgroundColor = Color.FromArgb("#DD3838"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 48,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(() =>
            {
                SecureStorage.Default.Remove("access_token");
                SecureStorage.Default.Remove("primary_role");
                if (Application.Current?.Windows.Count > 0)
                {
                    Application.Current.Windows[0].Page = new AppShell();
                }
            })
        });

        SetScaffold(new ScrollView { Content = body }, "Home", false);
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
        header.Add(new Button { Text = "<", BackgroundColor = Colors.Transparent, TextColor = CustomerUi.Dark, FontSize = 22 }, 0, 0);
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
            Command = new Command(async () => await Shell.Current.GoToAsync($"{nameof(CustomerChatPage)}?conversationId={conversation.ConversationId}"))
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
    private readonly Entry _messageEntry = new() { Placeholder = "Message", BackgroundColor = Color.FromArgb("#E8E8E8"), FontSize = 12 };

    public CustomerChatPage()
    {
        Title = "Chat";
        Render("Loading chat...");
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
            Text = "<",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 22,
            Command = new Command(async () => await Shell.Current.GoToAsync(".."))
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
        var text = _messageEntry.Text?.Trim();
        if (_conversationId <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        try
        {
            await CustomerApiClient.SendMessageAsync(_conversationId, text);
            _messageEntry.Text = string.Empty;
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

        body.Add(OrangeButton("Payment Options", new Command(async () => await Shell.Current.GoToAsync(nameof(PaymentOptionsPage)))));

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

        var body = new VerticalStackLayout { Spacing = 0, BackgroundColor = CustomerUi.Page };
        body.Add(Header("Payments"));
        body.Add(SectionHeader("PREFERRED PAYMENT"));
        body.Add(OptionRow("Cash on Delivery", "Available"));
        body.Add(SectionHeader("WALLETS"));
        body.Add(OptionRow("GCASH", "Ready"));
        body.Add(OptionRow("PayMaya", "Ready"));
        body.Add(OptionRow("PayPal", "Link account"));
        body.Add(OptionRow("Amazon Pay", "Link account"));
        body.Add(OptionRow("Grab Pay", "Link account"));
        body.Add(SectionHeader("CREDIT/DEBIT CARDS"));
        body.Add(OptionRow("5642-XXXXXXXX-0927", "Mastercard"));
        body.Add(OptionRow("5642-XXXXXXXX-0927", "Visa"));
        body.Add(OptionRow("ADD NEW CARD", "Saved cards"));
        body.Add(new VerticalStackLayout
        {
            Padding = new Thickness(14, 16),
            Children = { OrangeButton("Continue", new Command(async () => await Shell.Current.GoToAsync(nameof(PaymentCheckoutPage)))) }
        });

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

    private static View OptionRow(string name, string note)
    {
        var grid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 12),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(Label(name, 12, CustomerUi.Dark, FontAttributes.Bold), 0, 0);
        grid.Add(Label(note, 10, CustomerUi.Orange), 1, 0);
        return grid;
    }
}

public sealed class PaymentCheckoutPage : CustomerPageBase, IQueryAttributable
{
    private int _paymentId;
    private PaymentDto? _payment;
    private ServiceRequestDto? _request;

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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var payments = await CustomerApiClient.GetPaymentHistoryAsync();
            _payment = _paymentId > 0 ? payments.FirstOrDefault(x => x.PaymentId == _paymentId) : payments.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (_payment is not null)
            {
                _paymentId = _payment.PaymentId;
                _request = await CustomerApiClient.GetRequestAsync(_payment.RequestId);
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load payment details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var amount = _payment?.Amount ?? 0m;
        var service = _request?.ServiceName ?? (_payment is null ? "No payment selected" : $"Request #{_payment.RequestId}");

        var body = new VerticalStackLayout { Spacing = 12 };
        body.Add(new Grid
        {
            BackgroundColor = CustomerUi.Orange,
            Padding = new Thickness(16, 12),
            Children =
            {
                new Label
                {
                    Text = $"Payment Details\n1 item to pay: {Money(amount)}",
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
        content.Add(Row("Repair Type", _request?.IssueDescription ?? "Service request"));
        content.Add(Row("Service Date", _request?.ScheduledAt?.ToLocalTime().ToString("MMMM d, yyyy", CultureInfo.InvariantCulture) ?? "Not scheduled"));
        content.Add(Row("Technician", _request?.MechanicName ?? "Not assigned"));
        content.Add(Separator());
        content.Add(Row("Provider", _payment?.ProviderName ?? "Pending"));
        content.Add(Row("Reference", _payment?.ReferenceNumber ?? $"BM-PAY-{_paymentId:0000}"));
        content.Add(Row("Status", _payment is null ? "" : FormatStatus(_payment.Status)));
        content.Add(Row("Total", Money(amount)));
        content.Add(OrangeButton("Continue", new Command(async () => await Shell.Current.GoToAsync($"{nameof(PaymentReceiptPage)}?paymentId={_paymentId}"))));
        body.Add(content);

        SetScaffold(new ScrollView { Content = body }, "Payments", false);
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
    private readonly Picker _motorcyclePicker = new() { Title = "Motorcycle" };
    private readonly Picker _servicePicker = new() { Title = "Service" };
    private readonly Picker _addressPicker = new() { Title = "Address" };
    private readonly Editor _issueEditor = new() { Placeholder = "Describe the issue", HeightRequest = 120, BackgroundColor = Color.FromArgb("#F1F1F1") };

    public BookServicePage()
    {
        Title = "Book Service";
        Render("Loading booking data...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _customer = await CustomerApiClient.GetCustomerAsync();
            _services = await CustomerApiClient.SearchServicesAsync();
            PopulatePickers();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to create real bookings. {ex.Message}");
        }
    }

    private void PopulatePickers()
    {
        _motorcyclePicker.Items.Clear();
        foreach (var motorcycle in _customer?.Motorcycles ?? [])
        {
            _motorcyclePicker.Items.Add($"{motorcycle.Brand} {motorcycle.Model}");
        }
        _motorcyclePicker.SelectedIndex = _motorcyclePicker.Items.Count > 0 ? 0 : -1;

        _servicePicker.Items.Clear();
        foreach (var service in _services)
        {
            _servicePicker.Items.Add($"{service.ServiceName} - {Money(service.BasePrice)}");
        }
        _servicePicker.SelectedIndex = _servicePicker.Items.Count > 0 ? 0 : -1;

        _addressPicker.Items.Clear();
        foreach (var address in _customer?.Addresses ?? [])
        {
            _addressPicker.Items.Add($"{address.Label ?? "Address"} - {address.AddressLine}");
        }
        _addressPicker.SelectedIndex = _addressPicker.Items.Count > 0 ? 0 : -1;
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(18), Spacing = 14 };
        body.Add(Header("Book Service"));
        body.Add(Label("Choose your repair details", 20, CustomerUi.Dark, FontAttributes.Bold));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(Card(Label(banner, 11, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                _motorcyclePicker,
                _servicePicker,
                _addressPicker
            }
        }));
        body.Add(_issueEditor);
        body.Add(OrangeButton("Submit booking", new Command(async () => await SubmitAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task SubmitAsync()
    {
        if (_customer is null || _servicePicker.SelectedIndex < 0 || _motorcyclePicker.SelectedIndex < 0 || _addressPicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Booking", "Profile, service, motorcycle, and address are required.", "OK");
            return;
        }

        var service = _services[_servicePicker.SelectedIndex];
        var motorcycle = _customer.Motorcycles[_motorcyclePicker.SelectedIndex];
        var address = _customer.Addresses[_addressPicker.SelectedIndex];
        var issue = string.IsNullOrWhiteSpace(_issueEditor.Text) ? service.ServiceDescription ?? service.ServiceName : _issueEditor.Text.Trim();

        try
        {
            var request = await CustomerApiClient.CreateRequestAsync(new CreateServiceRequestDto(
                service.ShopId,
                service.ShopServiceId,
                motorcycle.MotorcycleId,
                issue,
                address.AddressLine,
                address.Latitude,
                address.Longitude,
                DateTime.UtcNow.AddHours(2)));

            await Shell.Current.GoToAsync($"{nameof(BookingDetailsPage)}?requestId={request.RequestId}");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Booking failed", ex.Message, "OK");
        }
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
        body.Add(OrangeButton("Open in Google Maps"));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }
}
