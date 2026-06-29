using System.Text.Json;
using BikeMate.Helpers;
using Microsoft.Maui.Controls.Shapes;
using static BikeMate.Helpers.JsonDisplayHelper;

namespace BikeMate.Views.Shared
{

public sealed record ModuleAction(string Text, string Route);

public abstract class ConnectedModulePage : ContentPage
{
    private static readonly Color Orange = Color.FromArgb("#FF6B2C");
    private static readonly Color Dark = Color.FromArgb("#1F2933");
    private static readonly Color Muted = Color.FromArgb("#6E6E6E");
    private static readonly Color BorderColor = Color.FromArgb("#E6E6E6");
    private static readonly Color PageColor = Color.FromArgb("#F6F6F6");

    private readonly string _pageTitle;
    private readonly string _subtitle;
    private readonly string _endpoint;
    private readonly string _emptyMessage;
    private readonly IReadOnlyList<ModuleAction> _actions;
    private bool _isLoading;
    private string? _banner;
    private IReadOnlyList<ModuleCard> _cards = [];

    protected ConnectedModulePage(
        string title,
        string subtitle,
        string endpoint,
        string emptyMessage,
        params ModuleAction[] actions)
    {
        _pageTitle = title;
        _subtitle = subtitle;
        _endpoint = endpoint;
        _emptyMessage = emptyMessage;
        _actions = actions;
        Title = title;
        BackgroundColor = PageColor;
        Render("Loading live data...");
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
        Render("Refreshing live data...");
        try
        {
            using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
            using var response = await http.GetAsync(_endpoint);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _cards = [];
                _banner = string.IsNullOrWhiteSpace(error)
                    ? $"Could not load /api/{_endpoint}. Status {(int)response.StatusCode}."
                    : error;
                return;
            }

            var payload = await response.Content.ReadAsStringAsync();
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(payload) ? "{}" : payload);
            _cards = ToCards(document.RootElement).Take(40).ToArray();
            _banner = null;
        }
        catch (Exception ex)
        {
            _cards = [];
            _banner = $"Could not load /api/{_endpoint}. {ex.Message}";
        }
        finally
        {
            _isLoading = false;
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Star)
            }
        };

        root.Add(Header(), 0, 0);

        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 14, 16, 24),
            Spacing = 12
        };

        if (_actions.Count > 0)
        {
            body.Add(ActionRow());
        }

        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(StateCard(banner, Colors.White));
        }

        if (_isLoading)
        {
            body.Add(new ActivityIndicator { IsVisible = true, IsRunning = true, Color = Orange, HeightRequest = 42 });
        }

        if (_cards.Count == 0 && !_isLoading)
        {
            body.Add(StateCard(_emptyMessage, Colors.White));
        }
        else
        {
            foreach (var card in _cards)
            {
                body.Add(DataCard(card));
            }
        }

        root.Add(new ScrollView { Content = body }, 0, 1);
        Content = root;
    }

    private View Header()
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

        var text = new VerticalStackLayout { Spacing = 3 };
        text.Add(new Label
        {
            Text = _pageTitle,
            TextColor = Dark,
            FontAttributes = FontAttributes.Bold,
            FontSize = 18
        });
        text.Add(new Label
        {
            Text = _subtitle,
            TextColor = Muted,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });
        grid.Add(text, 0, 0);
        grid.Add(new Button
        {
            Text = _isLoading ? "..." : "Refresh",
            IsEnabled = !_isLoading,
            BackgroundColor = Orange,
            TextColor = Colors.White,
            CornerRadius = 8,
            HeightRequest = 40,
            Padding = new Thickness(14, 0),
            Command = new Command(async () => await LoadAsync())
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
            Command = new Command(async () => await AppNavigation.ConfirmAndSignOutAsync(this))
        }, 2, 0);

        return grid;
    }

    private View ActionRow()
    {
        var row = new HorizontalStackLayout { Spacing = 8 };
        foreach (var action in _actions)
        {
            row.Add(new Button
            {
                Text = action.Text,
                BackgroundColor = Colors.White,
                TextColor = Dark,
                BorderColor = BorderColor,
                BorderWidth = 1,
                CornerRadius = 8,
                HeightRequest = 38,
                Padding = new Thickness(12, 0),
                FontSize = 13,
                Command = new Command(async () => await Shell.Current.GoToAsync(action.Route))
            });
        }

        return new ScrollView { Orientation = ScrollOrientation.Horizontal, Content = row };
    }

    private static View DataCard(ModuleCard card)
    {
        var stack = new VerticalStackLayout { Spacing = 8 };
        stack.Add(new Label
        {
            Text = card.Title,
            TextColor = Dark,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.WordWrap
        });
        stack.Add(new Label
        {
            Text = card.Body,
            TextColor = Muted,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        });
        return new Border
        {
            Stroke = BorderColor,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(14),
            Content = stack
        };
    }

    private static View StateCard(string text, Color background)
    {
        return new Border
        {
            Stroke = BorderColor,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = background,
            Padding = new Thickness(14),
            Content = new Label
            {
                Text = text,
                TextColor = Muted,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            }
        };
    }

    private static IEnumerable<ModuleCard> ToCards(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                yield return ToCard(item);
            }

            yield break;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            var scalarLines = root.EnumerateObject()
                .Where(x => x.Value.ValueKind is not JsonValueKind.Array and not JsonValueKind.Object)
                .Select(x => $"{Humanize(x.Name)}: {Scalar(x.Value)}")
                .ToArray();
            if (scalarLines.Length > 0)
            {
                yield return new ModuleCard("Summary", string.Join("\n", scalarLines));
            }

            foreach (var property in root.EnumerateObject().Where(x => x.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object))
            {
                yield return new ModuleCard(Humanize(property.Name), Describe(property.Value));
            }
        }
    }

    private static ModuleCard ToCard(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return new ModuleCard("Record", Scalar(element));
        }

        var title = FirstScalar(element, "title", "serviceName", "shopName", "fullName", "customerName", "email", "requestId", "paymentId", "productName")
            ?? "Record";
        return new ModuleCard(title, Describe(element));
    }

    private static string Describe(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.Object => string.Join("\n", value.EnumerateObject()
                .Where(x => x.Value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
                .Take(10)
                .Select(x => $"{Humanize(x.Name)}: {Scalar(x.Value)}")),
            JsonValueKind.Array => value.GetArrayLength() == 0
                ? "No records yet."
                : string.Join("\n\n", value.EnumerateArray().Take(5).Select(x => ToCard(x).Body)),
            _ => Scalar(value)
        };
    }

    private sealed record ModuleCard(string Title, string Body);
}

}

namespace BikeMate.Views.Admin
{

using BikeMate.Views.Shared;

public sealed class AdminDashboardPage() : ConnectedModulePage(
    "Admin Dashboard",
    "Platform analytics from backend database queries.",
    "admin/dashboard",
    "No dashboard analytics were returned yet.",
    new ModuleAction("Users", "//AdminUsersPage"),
    new ModuleAction("Requests", "//AdminRequestsPage"),
    new ModuleAction("Emergency", nameof(AdminEmergencyRequestsPage)),
    new ModuleAction("Audit", nameof(AdminAuditLogsPage)));

public sealed class AdminUsersPage() : ConnectedModulePage(
    "Users",
    "Users, roles, account status, and verification state.",
    "admin/users",
    "No users were returned yet.",
    new ModuleAction("Mechanics", nameof(AdminMechanicsVerificationPage)),
    new ModuleAction("Shops", nameof(AdminShopsVerificationPage)));

public sealed class AdminEmergencyRequestsPage() : ConnectedModulePage(
    "Emergency Monitoring",
    "Emergency service requests across the platform.",
    "admin/emergency-requests",
    "No emergency requests are active right now.");

public sealed class AdminMechanicsVerificationPage() : ConnectedModulePage(
    "Mechanic Verification",
    "Mechanic applications awaiting admin review.",
    "admin/mechanics/pending",
    "No mechanic verification requests are pending.");

public sealed class AdminShopsVerificationPage() : ConnectedModulePage(
    "Shop Verification",
    "Shop applications awaiting admin review.",
    "admin/shops/pending",
    "No shop verification requests are pending.");

public sealed class AdminRequestsPage() : ConnectedModulePage(
    "Requests",
    "All service requests with live status and assignments.",
    "admin/service-requests",
    "No service requests were returned.",
    new ModuleAction("Emergency", nameof(AdminEmergencyRequestsPage)));

public sealed class AdminPaymentsPage() : ConnectedModulePage(
    "Payments",
    "Payment records, provider status, amounts, and references.",
    "admin/payments",
    "No payments were returned.");

public sealed class AdminReportsPage() : ConnectedModulePage(
    "Reports",
    "Top services and platform reporting from backend queries.",
    "admin/reports/top-services",
    "No report rows were returned.",
    new ModuleAction("Revenue", nameof(AdminRevenueReportPage)));

public sealed class AdminRevenueReportPage() : ConnectedModulePage(
    "Revenue Report",
    "Revenue and paid payment count for the configured report range.",
    "admin/reports/revenue",
    "No revenue report data was returned.");

public sealed class AdminAuditLogsPage() : ConnectedModulePage(
    "Audit Logs",
    "Recent system actions and traceable admin changes.",
    "admin/audit-logs",
    "No audit logs were returned.");

}

namespace BikeMate.Views.ShopAdmin
{

using BikeMate.Views.Shared;

public sealed class ShopDashboardPage() : ConnectedModulePage(
    "Shop Dashboard",
    "Bookings, services, inventory, staff, ratings, and earnings.",
    "shop/dashboard",
    "No shop dashboard data was returned yet.",
    new ModuleAction("Services", "//ShopServicesPage"),
    new ModuleAction("Products", "//ShopProductsPage"),
    new ModuleAction("Bookings", "//ShopBookingsPage"),
    new ModuleAction("Mechanics", nameof(ShopMechanicsPage)));

public sealed class ShopProfilePage() : ConnectedModulePage(
    "Shop Profile",
    "Business profile, address, contact, and verification state.",
    "shop/profile",
    "No shop profile data was returned yet.");

public sealed class ShopServicesPage() : ConnectedModulePage(
    "Services",
    "Live shop services, categories, pricing, duration, and active state.",
    "shop/services",
    "No services have been added yet.",
    new ModuleAction("Add or Edit", nameof(ShopServiceEditPage)));

public sealed class ShopServiceEditPage() : ConnectedModulePage(
    "Service Editor",
    "Existing service records prepared for add, edit, disable, and image management API calls.",
    "shop/services",
    "No service records are available to edit yet.");

public sealed class ShopProductsPage() : ConnectedModulePage(
    "Products",
    "Inventory, stock quantity, pricing, and active state.",
    "shop/inventory",
    "No inventory products have been added yet.",
    new ModuleAction("Add or Edit", nameof(ShopProductEditPage)));

public sealed class ShopProductEditPage() : ConnectedModulePage(
    "Product Editor",
    "Existing product records prepared for add, edit, disable, and image management API calls.",
    "shop/inventory",
    "No product records are available to edit yet.");

public sealed class ShopBookingsPage() : ConnectedModulePage(
    "Bookings",
    "Incoming, active, and completed bookings related to this shop.",
    "shop/bookings",
    "No shop bookings were returned.");

public sealed class ShopMechanicsPage() : ConnectedModulePage(
    "Mechanics",
    "Mechanics assigned to this shop and their availability/performance.",
    "shop/mechanics",
    "No mechanics are assigned to this shop yet.");

public sealed class ShopEarningsPage() : ConnectedModulePage(
    "Earnings",
    "Paid bookings and PayMongo payment records for this shop.",
    "shop/payments",
    "No shop payment records were returned.");

public sealed class ShopSchedulePage() : ConnectedModulePage(
    "Schedule",
    "Shop bookings displayed as the first connected schedule view.",
    "shop/bookings",
    "No scheduled shop bookings were returned.");
}
