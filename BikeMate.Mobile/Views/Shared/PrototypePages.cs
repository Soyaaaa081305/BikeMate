namespace BikeMate.Views.Shared
{
    using System.Net.Http.Json;
    using System.Text.Json;
    using BikeMate.Helpers;
    using Microsoft.Maui.Controls.Shapes;
    using Microsoft.Maui.Layouts;

    public abstract class PrototypePage : ContentPage
    {
        private readonly string[] _fallbackCards;
        private readonly string? _endpoint;
        private readonly VerticalStackLayout _content = new() { Padding = new Thickness(20), Spacing = 14 };
        private readonly Label _status = new()
        {
            FontSize = 12,
            TextColor = Color.FromArgb("#6E6E6E"),
            LineBreakMode = LineBreakMode.WordWrap
        };
        private bool _isLoading;

        protected PrototypePage(string title, string subtitle, IEnumerable<string> highlights, IEnumerable<string> cards, string? endpoint = null)
        {
            Title = title;
            _fallbackCards = cards.ToArray();
            _endpoint = endpoint;
            BackgroundColor = Color.FromArgb("#F7F7F7");
            Content = BuildContent(title, subtitle, highlights);
            RenderCards(_fallbackCards, endpoint is null ? "Prototype content" : "Loading live module data...");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            if (!string.IsNullOrWhiteSpace(_endpoint))
            {
                await LoadAsync();
            }
        }

        private View BuildContent(string title, string subtitle, IEnumerable<string> highlights)
        {
            var root = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(GridLength.Auto),
                    new RowDefinition(GridLength.Star)
                }
            };

            var header = new Grid
            {
                Padding = new Thickness(20, 18, 20, 12),
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Auto)
                },
                BackgroundColor = Colors.White
            };

            var titleStack = new VerticalStackLayout { Spacing = 4 };
            titleStack.Add(new Label
            {
                Text = title,
                FontSize = 24,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1F2933")
            });
            titleStack.Add(new Label
            {
                Text = subtitle,
                FontSize = 13,
                TextColor = Color.FromArgb("#6E6E6E")
            });

            header.Add(titleStack, 0, 0);
            header.Add(new Button
            {
                Text = "Refresh",
                BackgroundColor = Color.FromArgb("#FF6B2C"),
                TextColor = Colors.White,
                CornerRadius = 14,
                HeightRequest = 34,
                FontSize = 11,
                Padding = new Thickness(12, 0),
                Command = new Command(async () => await LoadAsync())
            }, 1, 0);
            header.Add(new Image
            {
                Source = "bikemate_logo.png",
                HeightRequest = 48,
                WidthRequest = 48,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.End
            }, 2, 0);

            var highlightLayout = new FlexLayout
            {
                Wrap = FlexWrap.Wrap,
                Direction = FlexDirection.Row,
                AlignItems = FlexAlignItems.Start
            };

            foreach (var highlight in highlights)
            {
                highlightLayout.Add(new Border
                {
                    BackgroundColor = Color.FromArgb("#FFF3EA"),
                    Stroke = Color.FromArgb("#FFD5B5"),
                    StrokeShape = new RoundRectangle { CornerRadius = 8 },
                    Padding = new Thickness(10, 7),
                    Margin = new Thickness(0, 0, 8, 8),
                    Content = new Label
                    {
                        Text = highlight,
                        FontSize = 12,
                        TextColor = Color.FromArgb("#C45500"),
                        FontAttributes = FontAttributes.Bold
                    }
                });
            }

            _content.Add(highlightLayout);
            _content.Add(_status);

            root.Add(header, 0, 0);
            root.Add(new ScrollView { Content = _content }, 0, 1);
            return root;
        }

        private async Task LoadAsync()
        {
            if (_isLoading || string.IsNullOrWhiteSpace(_endpoint))
            {
                return;
            }

            _isLoading = true;
            RenderCards(_fallbackCards, "Loading live module data...");
            try
            {
                using var http = await ApiConfig.CreateAuthorizedHttpClientAsync();
                var payload = await http.GetStringAsync(_endpoint);
                using var document = JsonDocument.Parse(payload);
                var cards = ToCards(document.RootElement).Take(24).ToArray();
                RenderCards(
                    cards.Length == 0 ? ["No live records yet|The API returned an empty result for this module."] : cards,
                    $"Live data from /api/{_endpoint}");
            }
            catch (Exception ex)
            {
                RenderCards(_fallbackCards, $"Could not load /api/{_endpoint}. {ex.Message}");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void RenderCards(IEnumerable<string> cards, string status)
        {
            while (_content.Children.Count > 2)
            {
                _content.Children.RemoveAt(2);
            }

            _status.Text = status;
            foreach (var card in cards)
            {
                _content.Add(Card(card));
            }
        }

        private static View Card(string card)
        {
            var pieces = card.Split('|', 2);
            return new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E8E8E8"),
                StrokeShape = new RoundRectangle { CornerRadius = 8 },
                Padding = new Thickness(16),
                Content = new VerticalStackLayout
                {
                    Spacing = 8,
                    Children =
                    {
                        new Label
                        {
                            Text = pieces.ElementAtOrDefault(0) ?? card,
                            FontSize = 16,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = Color.FromArgb("#1F2933")
                        },
                        new Label
                        {
                            Text = pieces.ElementAtOrDefault(1) ?? string.Empty,
                            FontSize = 13,
                            TextColor = Color.FromArgb("#6E6E6E"),
                            LineBreakMode = LineBreakMode.WordWrap
                        }
                    }
                }
            };
        }

        private static IEnumerable<string> ToCards(JsonElement root)
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
                foreach (var property in root.EnumerateObject())
                {
                    yield return $"{Humanize(property.Name)}|{Describe(property.Value)}";
                }
            }
        }

        private static string ToCard(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return $"Record|{Describe(element)}";
            }

            var title = FirstString(element, "title", "fullName", "customerName", "shopName", "serviceName", "email", "requestId", "paymentId")
                ?? "Record";
            return $"{title}|{Describe(element)}";
        }

        private static string Describe(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.Object => string.Join("\n", value.EnumerateObject()
                    .Where(x => x.Value.ValueKind is not JsonValueKind.Object and not JsonValueKind.Array)
                    .Take(8)
                    .Select(x => $"{Humanize(x.Name)}: {Scalar(x.Value)}")),
                JsonValueKind.Array => $"{value.GetArrayLength()} item(s)" + (value.GetArrayLength() > 0 ? $"\n{Describe(value.EnumerateArray().First())}" : ""),
                _ => Scalar(value)
            };
        }

        private static string? FirstString(JsonElement element, params string[] names)
        {
            foreach (var name in names)
            {
                if (element.TryGetProperty(name, out var value))
                {
                    return Scalar(value);
                }

                var property = element.EnumerateObject().FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(property.Name))
                {
                    return Scalar(property.Value);
                }
            }

            return null;
        }

        private static string Scalar(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String => value.GetString() ?? "",
                JsonValueKind.Number => value.ToString(),
                JsonValueKind.True => "Yes",
                JsonValueKind.False => "No",
                JsonValueKind.Null => "None",
                JsonValueKind.Undefined => "",
                _ => value.ToString()
            };
        }

        private static string Humanize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            var chars = new List<char> { char.ToUpperInvariant(value[0]) };
            foreach (var c in value.Skip(1))
            {
                if (char.IsUpper(c))
                {
                    chars.Add(' ');
                }
                chars.Add(c);
            }
            return new string(chars.ToArray());
        }
    }
}

namespace BikeMate.Views.Mechanic
{
    using BikeMate.Views.Shared;

    public sealed class MechanicDashboardPage() : PrototypePage(
        "Mechanic Dashboard",
        "Assigned jobs, status, rating, and live availability.",
        ["Online", "4.80 rating", "12 jobs"],
        [
            "Active Job|Flat Tire Rescue for Juan Customer.",
            "Status Actions|Accept, en route, arrived, in progress, completed.",
            "Location Sharing|Active jobs can push live location updates."
        ],
        "rider/dashboard");

    public sealed class MechanicJobsPage() : PrototypePage(
        "Jobs",
        "Incoming and assigned service requests.",
        ["Accept", "Reject", "Details"],
        [
            "Flat Tire Rescue|Customer location in Manila.",
            "Oil Change|Scheduled booking awaiting assignment.",
            "Emergency Help|High-priority roadside requests appear first."
        ],
        "rider/requests/incoming");

    public sealed class MechanicJobDetailsPage() : PrototypePage(
        "Job Details",
        "Customer, motorcycle, issue, location, and completion.",
        ["Customer", "Motorcycle", "Complete"],
        [
            "Customer|Juan Customer, visible only for the assigned job.",
            "Motorcycle|Honda Click 125i, black.",
            "Completion Photo|Upload proof after finishing the job."
        ],
        "rider/jobs/current");

    public sealed class MechanicMapPage() : PrototypePage(
        "Map",
        "Service location and navigation handoff.",
        ["Location", "Route", "Share"],
        [
            "Service Pin|Uses request latitude and longitude.",
            "Navigation|Open map navigation from the mechanic device.",
            "Live Updates|Posts to /api/mechanics/location."
        ],
        "rider/jobs/current");

    public sealed class MechanicMessagesPage() : PrototypePage(
        "Messages",
        "Job conversations with customers and shops.",
        ["Realtime", "Job chat", "Read"],
        [
            "Juan Customer|Mechanic and customer chat in one conversation.",
            "Shop Dispatch|Shop admins can coordinate assigned bookings.",
            "Attachments|Images can be attached through stored media URLs."
        ],
        "conversations");

    public sealed class MechanicHistoryPage() : PrototypePage(
        "History",
        "Completed, cancelled, and rejected jobs.",
        ["Completed", "Cancelled", "Rating"],
        [
            "Completed Jobs|Total jobs update from SQL trigger.",
            "Reviews|Average rating updates from review trigger.",
            "Earnings Basis|Completed services feed shop/admin reports."
        ],
        "rider/jobs/history");

    public sealed class MechanicProfilePage() : PrototypePage(
        "Profile",
        "Verification, availability, experience, and performance.",
        ["Verified", "Available", "Profile"],
        [
            "Bio|Certified roadside motorcycle technician.",
            "Availability|Online, busy, offline, or unavailable.",
            "Performance|Average rating and completed job count."
        ],
        "rider/dashboard");
}

namespace BikeMate.Views.Admin
{
    using BikeMate.Views.Shared;

    public sealed class AdminDashboardPage() : PrototypePage(
        "Admin Dashboard",
        "Platform analytics from backend database queries.",
        ["Users", "Requests", "Revenue"],
        [
            "Totals|Users, customers, mechanics, shops, and pending verifications.",
            "Requests|Pending, completed, cancelled, and active service monitoring.",
            "Revenue|Paid PayMongo payments feed analytics."
        ],
        "admin/dashboard");

    public sealed class AdminUsersPage() : PrototypePage(
        "Users",
        "Manage users, roles, and account status.",
        ["Activate", "Suspend", "Roles"],
        [
            "Customers|View customer account details.",
            "Mechanics|Verification and account health.",
            "Admins|SystemAdmin access controls platform-level operations."
        ],
        "admin/users");

    public sealed class AdminMechanicsVerificationPage() : PrototypePage(
        "Mechanic Verification",
        "Verify or reject mechanic applications.",
        ["Pending", "Verify", "Reject"],
        [
            "Certification|Review certification image and profile details.",
            "Decision|Approve or reject with notes.",
            "Audit|Actions can be written into audit logs."
        ],
        "admin/mechanics/pending");

    public sealed class AdminShopsVerificationPage() : PrototypePage(
        "Shop Verification",
        "Verify shop documents and profile data.",
        ["Permit", "Image", "Status"],
        [
            "Business Permit|Review uploaded permit URL.",
            "Shop Details|Address, contact number, and coordinates.",
            "Decision|Verified shops become visible to customers."
        ],
        "admin/shops/pending");

    public sealed class AdminRequestsPage() : PrototypePage(
        "Requests",
        "Monitor platform service requests.",
        ["Pending", "Active", "Completed"],
        [
            "Request Feed|All service requests with status and assignment.",
            "Assignment|System admin can assign mechanics.",
            "Timeline|Status history is saved for each request."
        ],
        "admin/service-requests");

    public sealed class AdminPaymentsPage() : PrototypePage(
        "Payments",
        "Monitor PayMongo status and payment events.",
        ["Paid", "Pending", "Webhook"],
        [
            "Payment Records|Amount, status, method, reference, provider IDs.",
            "Events|Webhook payloads are stored for audit and troubleshooting.",
            "Revenue|Paid transactions feed admin reports."
        ],
        "admin/payments");

    public sealed class AdminReportsPage() : PrototypePage(
        "Reports",
        "Revenue, top services, and top mechanics.",
        ["Revenue", "Services", "Mechanics"],
        [
            "Revenue Report|Date-range revenue from paid payments.",
            "Top Services|Most requested shop services.",
            "Top Mechanics|Rating and completed-job performance."
        ],
        "admin/reports/top-services");

    public sealed class AdminAuditLogsPage() : PrototypePage(
        "Audit Logs",
        "System actions and data changes.",
        ["Actor", "Entity", "Changes"],
        [
            "Actor|User that performed an admin action.",
            "Entity|Changed entity and ID.",
            "Values|Old and new values JSON for traceability."
        ],
        "admin/audit-logs");
}

namespace BikeMate.Views.ShopAdmin
{
    using BikeMate.Views.Shared;

    public sealed class ShopDashboardPage() : PrototypePage(
        "Shop Dashboard",
        "Bookings, services, products, staff, and earnings.",
        ["Bookings", "Stock", "Earnings"],
        [
            "Incoming Bookings|Accept or reject customer service requests.",
            "Services|Manage categories, prices, and active services.",
            "Earnings|Paid bookings calculate shop revenue."
        ],
        "shop/dashboard");

    public sealed class ShopProfilePage() : PrototypePage(
        "Shop Profile",
        "Edit business details, address, permit, and images.",
        ["Profile", "Permit", "Coordinates"],
        [
            "Shop Details|Name, description, contact number, address.",
            "Uploads|Business permit and shop image URLs.",
            "Location|Latitude and longitude support nearby discovery."
        ],
        "shop/profile");

    public sealed class ShopServicesPage() : PrototypePage(
        "Services",
        "Add, edit, and disable shop services.",
        ["Categories", "Pricing", "Images"],
        [
            "Flat Tire Rescue|Tire Service, PHP 350.00.",
            "Basic Oil Change|Oil Change, PHP 500.00.",
            "Emergency Roadside Help|Emergency, PHP 700.00."
        ],
        "shop/services");

    public sealed class ShopServiceEditPage() : PrototypePage(
        "Edit Service",
        "Service name, category, price, duration, and images.",
        ["Name", "Price", "Active"],
        [
            "Category|Choose a normalized service category.",
            "Pricing|Set base service fee.",
            "Status|Disable services without deleting historical requests."
        ],
        "shop/services");

    public sealed class ShopProductsPage() : PrototypePage(
        "Products",
        "Manage parts, stock, price, and product images.",
        ["Stock", "Price", "Images"],
        [
            "Engine Oil 1L|PHP 280.00, 20 in stock.",
            "Tubeless Tire Patch|PHP 120.00, 50 in stock.",
            "Inventory|Stock quantities are saved per product."
        ],
        "shop/inventory");

    public sealed class ShopProductEditPage() : PrototypePage(
        "Edit Product",
        "Product details, images, and stock quantity.",
        ["Name", "Stock", "Active"],
        [
            "Product Info|Name, description, and price.",
            "Stock|Quantity available for parts sales.",
            "Images|Product image URLs saved in product_images."
        ],
        "shop/inventory");

    public sealed class ShopBookingsPage() : PrototypePage(
        "Bookings",
        "Incoming, active, and completed shop bookings.",
        ["Accept", "Reject", "Assign"],
        [
            "Pending Requests|Review customer issue and location.",
            "Assignments|Assign shop mechanics to accepted jobs.",
            "Completed Jobs|Completed bookings feed earnings."
        ],
        "shop/bookings");

    public sealed class ShopMechanicsPage() : PrototypePage(
        "Mechanics",
        "Assign mechanics and manage shop staff.",
        ["Staff", "Assign", "Active"],
        [
            "Rico Mechanic|Verified mechanic assigned to this shop.",
            "Availability|Online, busy, offline, unavailable.",
            "Performance|Ratings and completed jobs help dispatch decisions."
        ],
        "shop/mechanics");

    public sealed class ShopEarningsPage() : PrototypePage(
        "Earnings",
        "Paid bookings and shop revenue.",
        ["Revenue", "Payments", "Receipts"],
        [
            "Paid Services|Only paid payment statuses count as revenue.",
            "PayMongo|Checkout session, payment ID, and reference stored.",
            "Summary|Date-range earnings from backend reports."
        ],
        "shop/payments");

    public sealed class ShopSchedulePage() : PrototypePage(
        "Shop Schedule",
        "Operating hours and upcoming service calendar.",
        ["Hours", "Calendar", "Staff"],
        [
            "Operating Hours|Per-day opening and closing times.",
            "Bookings Calendar|Scheduled appointments by date.",
            "Staff Coverage|Mechanic availability can be matched to bookings."
        ],
        "shop/bookings");
}
