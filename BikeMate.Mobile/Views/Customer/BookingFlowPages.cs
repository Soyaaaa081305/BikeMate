using System.Diagnostics;
using System.Globalization;
using System.Net;
using BikeMate.Core.DTOs;
using BikeMate.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Customer;

internal static class BookingDraft
{
    public static CustomerMeDto? Customer { get; set; }
    public static IReadOnlyList<ShopServiceDto> Services { get; set; } = [];
    public static int RequestId { get; set; }
    public static int PaymentId { get; set; }
    public static int? SelectedShopId { get; set; }
    public static int? SelectedShopServiceId { get; set; }
    public static string Region { get; set; } = "Mega Manila";
    public static string LocationName { get; set; } = "";
    public static string AddressLine { get; set; } = "";
    public static decimal? Latitude { get; set; } = 14.599512m;
    public static decimal? Longitude { get; set; } = 120.984222m;
    public static string Brand { get; set; } = "Honda";
    public static string Model { get; set; } = "Click 125i";
    public static string ProblemCategory { get; set; } = "Accessory Installation";
    public static string OtherDetails { get; set; } = "";
    public static string ServiceType { get; set; } = "Pick-up Repair";
    public static DateTime ScheduledAt { get; set; } = DateTime.Today.AddDays(3).AddHours(7);
    public static string? ImageMediaUrl { get; set; }
    public static string? VideoMediaUrl { get; set; }
    public static string? ImagePreviewPath { get; set; }
    public static string? VideoPreviewPath { get; set; }
    public static bool RequestMediaAttached { get; set; }
    public static string PaymentMethod { get; set; } = "GCASH";
    public static readonly string[] RegionOptions =
    [
        "Makati",
        "San Pedro",
        "Pasig",
        "Quezon City",
        "Baguio",
        "Cagayan de Oro",
        "Cebu",
        "Naga and Legazpi",
        "Central Luzon",
        "Mega Manila"
    ];
    public static readonly string[] LocationOptions =
    [
        "Makati Ave, Mega Manila",
        "San Pedro, Laguna",
        "Pasig City",
        "Quezon City",
        "Use current location"
    ];

    public static void Reset()
    {
        RequestId = 0;
        PaymentId = 0;
        SelectedShopId = null;
        SelectedShopServiceId = null;
        ImageMediaUrl = null;
        VideoMediaUrl = null;
        ImagePreviewPath = null;
        VideoPreviewPath = null;
        RequestMediaAttached = false;
        PaymentMethod = "GCASH";
        Region = "Mega Manila";
        LocationName = "";
        AddressLine = "";
        Latitude = 14.599512m;
        Longitude = 120.984222m;
        ProblemCategory = "Accessory Installation";
        OtherDetails = "";
        ServiceType = "Pick-up Repair";
        ScheduledAt = DateTime.Today.AddDays(3).AddHours(7);
    }

    public static void ApplyCustomer(CustomerMeDto customer)
    {
        Customer = customer;
        var address = customer.Addresses.FirstOrDefault(x => x.IsDefault) ?? customer.Addresses.FirstOrDefault();
        if (address is not null)
        {
            Latitude = address.Latitude ?? Latitude;
            Longitude = address.Longitude ?? Longitude;
            if (!string.IsNullOrWhiteSpace(address.City))
            {
                LocationName = address.City;
                Region = RegionForLocation($"{address.City} {address.Province}");
            }
        }

        var motorcycle = customer.Motorcycles.FirstOrDefault();
        if (motorcycle is not null)
        {
            Brand = motorcycle.Brand;
            Model = motorcycle.Model;
        }
    }

    public static MotorcycleDto? SelectedMotorcycle()
    {
        return Customer?.Motorcycles.FirstOrDefault(x =>
            string.Equals(x.Brand, Brand, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Model, Model, StringComparison.OrdinalIgnoreCase))
            ?? Customer?.Motorcycles.FirstOrDefault();
    }

    public static ShopServiceDto? SelectedService()
    {
        var explicitService = Services.FirstOrDefault(x => x.ShopServiceId == SelectedShopServiceId);
        if (explicitService is not null)
        {
            return explicitService;
        }

        var categoryNeedle = ProblemCategory switch
        {
            "Tire Problem" => "Tire",
            "Brake Adjustment" => "Brake",
            "Gear Shifting Issue" => "Engine",
            "Accessory Installation" => "Emergency",
            "Chain Maintenance" => "Oil",
            "General Tune-up" => "Oil",
            _ => ProblemCategory
        };

        return Services.FirstOrDefault(x =>
                   x.CategoryName.Contains(categoryNeedle, StringComparison.OrdinalIgnoreCase) ||
                   x.ServiceName.Contains(categoryNeedle, StringComparison.OrdinalIgnoreCase))
               ?? Services.FirstOrDefault();
    }

    public static string ConfirmationAddress()
    {
        var parts = new[] { AddressLine, LocationName }.Where(x => !string.IsNullOrWhiteSpace(x));
        var address = string.Join("\n", parts);
        return string.IsNullOrWhiteSpace(address) ? "Address not set" : address;
    }

    public static string LocationCaption()
    {
        var parts = new[] { LocationName, AddressLine }.Where(x => !string.IsNullOrWhiteSpace(x));
        var caption = string.Join("\n", parts);
        return string.IsNullOrWhiteSpace(caption) ? "Select a location" : caption;
    }

    public static void SetManualLocation(string locationName)
    {
        var (latitude, longitude) = CoordinatesForLocation(locationName);
        ApplyLocation(locationName, null, latitude, longitude);
    }

    public static void SetRegion(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
        {
            return;
        }

        Region = region;
        var matchingLocation = Region switch
        {
            "Makati" => "Makati Ave, Mega Manila",
            "San Pedro" => "San Pedro, Laguna",
            "Pasig" => "Pasig City",
            "Quezon City" => "Quezon City",
            "Baguio" => "Baguio City",
            "Cagayan de Oro" => "Cagayan de Oro",
            "Cebu" => "Cebu City",
            "Naga and Legazpi" => "Naga City",
            "Central Luzon" => "San Fernando, Pampanga",
            "Mega Manila" => "Mega Manila",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(matchingLocation))
        {
            LocationName = matchingLocation;
            var (latitude, longitude) = CoordinatesForLocation(matchingLocation);
            Latitude = latitude ?? Latitude;
            Longitude = longitude ?? Longitude;
        }
    }

    public static void ApplyLocation(string locationName, string? addressLine, decimal? latitude, decimal? longitude)
    {
        if (!string.IsNullOrWhiteSpace(locationName))
        {
            LocationName = locationName;
            Region = RegionForLocation(locationName);
        }

        if (!string.IsNullOrWhiteSpace(addressLine))
        {
            AddressLine = addressLine;
        }

        Latitude = latitude ?? Latitude;
        Longitude = longitude ?? Longitude;
    }

    public static string RegionForLocation(string? text)
    {
        var value = text?.ToLowerInvariant() ?? string.Empty;
        if (value.Contains("san pedro") || value.Contains("laguna"))
        {
            return "San Pedro";
        }
        if (value.Contains("makati"))
        {
            return "Makati";
        }
        if (value.Contains("pasig"))
        {
            return "Pasig";
        }
        if (value.Contains("quezon"))
        {
            return "Quezon City";
        }
        if (value.Contains("baguio"))
        {
            return "Baguio";
        }
        if (value.Contains("cagayan"))
        {
            return "Cagayan de Oro";
        }
        if (value.Contains("cebu"))
        {
            return "Cebu";
        }
        if (value.Contains("naga") || value.Contains("legazpi"))
        {
            return "Naga and Legazpi";
        }
        if (value.Contains("pampanga") || value.Contains("bulacan") || value.Contains("tarlac") || value.Contains("central luzon"))
        {
            return "Central Luzon";
        }

        return "Mega Manila";
    }

    private static (decimal? Latitude, decimal? Longitude) CoordinatesForLocation(string locationName)
    {
        return locationName switch
        {
            "Makati Ave, Mega Manila" => (14.554729m, 121.024445m),
            "San Pedro, Laguna" => (14.358333m, 121.058333m),
            "Pasig City" => (14.576377m, 121.085110m),
            "Quezon City" => (14.676041m, 121.043700m),
            "Baguio City" => (16.402333m, 120.596008m),
            "Cagayan de Oro" => (8.454236m, 124.631897m),
            "Cebu City" => (10.315699m, 123.885437m),
            "Naga City" => (13.621775m, 123.194824m),
            "San Fernando, Pampanga" => (15.033333m, 120.683333m),
            "Mega Manila" => (14.599512m, 120.984222m),
            _ => (Latitude, Longitude)
        };
    }
}

internal static class BookingFlowActions
{
    public static async Task<ServiceRequestDto> EnsureRequestAsync(bool attachMedia)
    {
        var customer = BookingDraft.Customer ?? await CustomerApiClient.GetCustomerAsync();
        BookingDraft.Customer = customer;
        if (BookingDraft.Services.Count == 0)
        {
            BookingDraft.Services = await CustomerApiClient.SearchServicesAsync();
        }

        var motorcycle = BookingDraft.SelectedMotorcycle();
        var shopId = BookingDraft.SelectedShopId;
        var shopServiceId = shopId is null ? null : BookingDraft.SelectedShopServiceId;
        if (shopId is not null && shopServiceId is null)
        {
            shopServiceId = BookingDraft.Services.FirstOrDefault(x => x.ShopId == shopId)?.ShopServiceId;
            BookingDraft.SelectedShopServiceId = shopServiceId;
        }

        ServiceRequestDto request;
        if (BookingDraft.RequestId == 0)
        {
            request = await CustomerApiClient.CreateRequestAsync(new CreateServiceRequestDto(
                shopId,
                shopServiceId,
                motorcycle?.MotorcycleId,
                $"{BookingDraft.ProblemCategory}: {BookingDraft.OtherDetails}",
                BookingDraft.AddressLine,
                BookingDraft.Latitude,
                BookingDraft.Longitude,
                BookingDraft.ScheduledAt.ToUniversalTime()));
            BookingDraft.RequestId = request.RequestId;
        }
        else
        {
            request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
        }

        if (attachMedia && !BookingDraft.RequestMediaAttached)
        {
            if (!string.IsNullOrWhiteSpace(BookingDraft.ImageMediaUrl))
            {
                await CustomerApiClient.AttachRequestMediaAsync(BookingDraft.RequestId, new UploadMediaDto(BookingDraft.ImageMediaUrl, "image", "Customer issue picture"));
            }

            if (!string.IsNullOrWhiteSpace(BookingDraft.VideoMediaUrl))
            {
                await CustomerApiClient.AttachRequestMediaAsync(BookingDraft.RequestId, new UploadMediaDto(BookingDraft.VideoMediaUrl, "video", "Customer issue video"));
            }

            BookingDraft.RequestMediaAttached = true;
        }

        return request;
    }

    public static async Task<ServiceRequestDto> SelectShopAsync(int shopId, int? shopServiceId)
    {
        BookingDraft.SelectedShopId = shopId;
        BookingDraft.SelectedShopServiceId = shopServiceId ?? BookingDraft.Services.FirstOrDefault(x => x.ShopId == shopId)?.ShopServiceId;
        var request = await EnsureRequestAsync(false);
        return await CustomerApiClient.SelectShopAsync(request.RequestId, new SelectShopDto(shopId, BookingDraft.SelectedShopServiceId));
    }
}

internal static class BookingVisuals
{
    public const string FallenBikeImage = "https://images.unsplash.com/photo-1517649763962-0c623066013b?auto=format&fit=crop&w=700&q=80";
    public const string RiderImage = "https://images.unsplash.com/photo-1534787238916-9ba6764efd4f?auto=format&fit=crop&w=700&q=80";
    public const string ShopImage = "https://images.unsplash.com/photo-1558981806-ec527fa84c39?auto=format&fit=crop&w=700&q=80";
    public const string MechanicImage = "https://images.unsplash.com/photo-1542046272227-d247df21628a?auto=format&fit=crop&w=500&q=80";

    public static HtmlWebViewSource GoogleMapSource(decimal latitude, decimal longitude)
    {
        var latText = latitude.ToString(CultureInfo.InvariantCulture);
        var lngText = longitude.ToString(CultureInfo.InvariantCulture);
        var query = $"{latText},{lngText}";
        var key = GoogleMapsEmbedKey();
        var frameUrl = string.IsNullOrWhiteSpace(key)
            ? $"https://maps.google.com/maps?q={Uri.EscapeDataString(query)}&z=15&output=embed"
            : $"https://www.google.com/maps/embed/v1/place?key={Uri.EscapeDataString(key)}&q={Uri.EscapeDataString(query)}&zoom=15";
        return GoogleMapFrame(frameUrl);
    }

    public static HtmlWebViewSource GoogleDirectionsSource(
        decimal originLatitude,
        decimal originLongitude,
        decimal destinationLatitude,
        decimal destinationLongitude)
    {
        var origin = $"{originLatitude.ToString(CultureInfo.InvariantCulture)},{originLongitude.ToString(CultureInfo.InvariantCulture)}";
        var destination = $"{destinationLatitude.ToString(CultureInfo.InvariantCulture)},{destinationLongitude.ToString(CultureInfo.InvariantCulture)}";
        var key = GoogleMapsEmbedKey();
        var frameUrl = string.IsNullOrWhiteSpace(key)
            ? $"https://maps.google.com/maps?saddr={Uri.EscapeDataString(origin)}&daddr={Uri.EscapeDataString(destination)}&dirflg=d&output=embed"
            : $"https://www.google.com/maps/embed/v1/directions?key={Uri.EscapeDataString(key)}&origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&mode=driving";
        return GoogleMapFrame(frameUrl);
    }

    private static HtmlWebViewSource GoogleMapFrame(string frameUrl)
    {
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
    html, body, iframe {
      width: 100%;
      height: 100%;
      margin: 0;
      padding: 0;
      border: 0;
      overflow: hidden;
      background: #eef1f4;
    }
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

    public static View FlowHeader(string title)
    {
        var grid = new Grid
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(14, 14, 14, 10),
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10
        };

        grid.Add(new Button
        {
            Text = "<",
            BackgroundColor = Color.FromArgb("#FFF2EA"),
            TextColor = CustomerUi.Dark,
            FontSize = 18,
            WidthRequest = 40,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = new Thickness(0),
            Command = new Command(async () => await Shell.Current.GoToAsync(".."))
        }, 0, 0);

        var text = new VerticalStackLayout { Spacing = 2 };
        text.Add(new Label
        {
            Text = title,
            TextColor = CustomerUi.Dark,
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            LineBreakMode = LineBreakMode.TailTruncation
        });
        text.Add(new Label
        {
            Text = "Book a service",
            TextColor = CustomerUi.Muted,
            FontSize = 11
        });
        grid.Add(text, 1, 0);
        return grid;
    }

    public static Label Text(string value, double size, Color? color = null, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = value,
            FontSize = CustomerUi.SizeFor(size),
            TextColor = color ?? CustomerUi.Dark,
            FontAttributes = attributes,
            FontFamily = CustomerUi.FontFor(size, attributes),
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    public static Button PrimaryButton(string text, Command command)
    {
        return new Button
        {
            Text = text,
            Command = command,
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            FontSize = 13,
            CornerRadius = 8,
            HeightRequest = 48,
            FontAttributes = FontAttributes.Bold,
            FontFamily = CustomerUi.FontDisplay
        };
    }

    public static Border WhiteCard(View content, double radius = 6, Thickness? padding = null)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CustomerUi.Border,
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = Math.Min(radius, 8) },
            Padding = padding ?? new Thickness(14),
            Content = content
        };
    }

    public static View FieldRow(string label, string value, Command command)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        var stack = new VerticalStackLayout { Spacing = 1 };
        stack.Add(Text(label, 10, CustomerUi.Muted));
        stack.Add(Text(string.IsNullOrWhiteSpace(value) ? "Select" : value, 12, CustomerUi.Dark));
        grid.Add(stack, 0, 0);
        grid.Add(Text("v", 11, CustomerUi.Muted), 1, 0);

        var border = WhiteCard(grid, 8, new Thickness(12, 9));
        border.GestureRecognizers.Add(new TapGestureRecognizer { Command = command });
        return border;
    }

    public static View PickerRow(string label, IReadOnlyList<string> options, string? selected, Func<string, Task> onChanged)
    {
        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(Text(label, 10, CustomerUi.Muted));

        var picker = new Picker
        {
            Title = $"Select {label}",
            FontSize = 13,
            FontFamily = CustomerUi.FontBody,
            TextColor = CustomerUi.Dark,
            TitleColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Fill
        };

        var values = options
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!string.IsNullOrWhiteSpace(selected) && values.All(x => !string.Equals(x, selected, StringComparison.OrdinalIgnoreCase)))
        {
            values.Insert(0, selected);
        }

        foreach (var option in values)
        {
            picker.Items.Add(option);
        }

        picker.SelectedIndex = values.FindIndex(x => string.Equals(x, selected, StringComparison.OrdinalIgnoreCase));
        var busy = false;
        picker.SelectedIndexChanged += async (_, _) =>
        {
            if (busy || picker.SelectedIndex < 0 || picker.SelectedIndex >= picker.Items.Count)
            {
                return;
            }

            busy = true;
            try
            {
                await onChanged(picker.Items[picker.SelectedIndex]);
            }
            finally
            {
                busy = false;
            }
        };

        stack.Add(picker);
        return WhiteCard(stack, 8, new Thickness(12, 8));
    }

    public static View MapPanel(double height, bool withLocateButton = false)
    {
        var latitude = BookingDraft.Latitude ?? 14.599512m;
        var longitude = BookingDraft.Longitude ?? 120.984222m;
        var map = new Grid { HeightRequest = height, BackgroundColor = Color.FromArgb("#EEF1F4") };
        map.Add(new WebView
        {
            Source = GoogleMapSource(latitude, longitude),
            HeightRequest = height
        });
        map.Add(new Label
        {
            Text = BookingDraft.LocationCaption(),
            TextColor = CustomerUi.Dark,
            BackgroundColor = Color.FromRgba(255, 255, 255, 0.86),
            Padding = new Thickness(10, 6),
            FontSize = 11,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(10)
        });

        if (withLocateButton)
        {
            var actions = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 8,
                HorizontalOptions = LayoutOptions.Fill,
                VerticalOptions = LayoutOptions.End,
                Margin = new Thickness(10, 0, 10, 10)
            };

            actions.Add(new Button
            {
                Text = "Access location",
                BackgroundColor = CustomerUi.Orange,
                TextColor = Colors.White,
                FontSize = 11,
                CornerRadius = 18,
                HeightRequest = 34,
                HorizontalOptions = LayoutOptions.Fill,
                Command = new Command(async () =>
                {
                    try
                    {
                        await LocationAccessPage.ShowAsync();
                        if (Shell.Current?.CurrentPage is BookServicePage bookPage)
                        {
                            bookPage.RefreshLocationUi();
                        }
                    }
                    catch (Exception ex)
                    {
                        var page = Shell.Current?.CurrentPage ?? Application.Current?.Windows.FirstOrDefault()?.Page;
                        if (page is not null)
                        {
                            await page.DisplayAlertAsync("Location", ex.Message, "OK");
                        }
                    }
                })
            }, 0, 0);

            actions.Add(new Button
            {
                Text = "Open Google Maps",
                BackgroundColor = Colors.White,
                TextColor = CustomerUi.Dark,
                BorderColor = CustomerUi.Border,
                BorderWidth = 1,
                FontSize = 11,
                CornerRadius = 18,
                HeightRequest = 34,
                HorizontalOptions = LayoutOptions.Fill,
                Command = new Command(async () => await OpenGoogleMapsAsync())
            }, 1, 0);

            map.Add(actions);
        }

        return map;
    }

    public static async Task<bool> UpdateCurrentLocationAsync(Page page)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                await page.DisplayAlertAsync("Location blocked", "Location permission is required to use your current location.", "OK");
                return false;
            }

            Location? location = null;
            try
            {
                location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(15)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Real-time geolocation failed, falling back to last known: {ex}");
                location = await Geolocation.Default.GetLastKnownLocationAsync();
            }

            if (location is null)
            {
                await page.DisplayAlertAsync("Location unavailable", "Your device did not return a current location. Please check GPS and try again.", "OK");
                return false;
            }

            var locationName = "Current Location";
            var addressLine = string.Empty;

            try
            {
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
                var place = placemarks?.FirstOrDefault();
                if (place is not null)
                {
                    var locality = string.Join(", ", new[] { place.Locality, place.AdminArea }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (!string.IsNullOrWhiteSpace(locality))
                    {
                        locationName = locality;
                    }

                    addressLine = string.Join(" ", new[] { place.SubThoroughfare, place.Thoroughfare }.Where(x => !string.IsNullOrWhiteSpace(x)));
                    if (string.IsNullOrWhiteSpace(addressLine))
                    {
                        addressLine = place.FeatureName ?? place.SubLocality ?? BookingDraft.AddressLine;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Reverse geocoding failed: {ex}");
                addressLine = $"Lat {location.Latitude:0.000000}, Lng {location.Longitude:0.000000}";
            }

            BookingDraft.ApplyLocation(locationName, addressLine, (decimal)location.Latitude, (decimal)location.Longitude);
            return true;
        }
        catch (Exception ex)
        {
            await page.DisplayAlertAsync("Location error", ex.Message, "OK");
            return false;
        }
    }

    public static async Task OpenGoogleMapsAsync(string? query = null)
    {
        var latitude = (BookingDraft.Latitude ?? 14.599512m).ToString(CultureInfo.InvariantCulture);
        var longitude = (BookingDraft.Longitude ?? 120.984222m).ToString(CultureInfo.InvariantCulture);
        var rawQuery = string.IsNullOrWhiteSpace(query) ? $"{latitude},{longitude}" : query.Trim();
        var encodedQuery = Uri.EscapeDataString(rawQuery);

        if (await TryOpenUriAsync(new Uri($"geo:0,0?q={encodedQuery}")) ||
            await TryOpenUriAsync(new Uri($"https://www.google.com/maps/search/?api=1&query={encodedQuery}")))
        {
            return;
        }

        await Shell.Current.DisplayAlertAsync("Maps unavailable", "No maps or browser app could open this location.", "OK");
    }

    public static async Task OpenGoogleDirectionsAsync(
        decimal originLatitude,
        decimal originLongitude,
        decimal destinationLatitude,
        decimal destinationLongitude)
    {
        var origin = $"{originLatitude.ToString(CultureInfo.InvariantCulture)},{originLongitude.ToString(CultureInfo.InvariantCulture)}";
        var destination = $"{destinationLatitude.ToString(CultureInfo.InvariantCulture)},{destinationLongitude.ToString(CultureInfo.InvariantCulture)}";
        var url = $"https://www.google.com/maps/dir/?api=1&origin={Uri.EscapeDataString(origin)}&destination={Uri.EscapeDataString(destination)}&travelmode=driving";

        if (await TryOpenUriAsync(new Uri(url)))
        {
            return;
        }

        await OpenGoogleMapsAsync(destination);
    }

    private static async Task<bool> TryOpenUriAsync(Uri uri)
    {
        try
        {
            return await Microsoft.Maui.ApplicationModel.Launcher.Default.OpenAsync(uri);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open URI {uri}: {ex}");
            return false;
        }
    }

    public static View UploadBox(string icon, string title, string? uploadedUrl, string? previewPath, Command command)
    {
        var grid = new Grid
        {
            HeightRequest = 134,
            BackgroundColor = Color.FromArgb("#FAFAFA")
        };

        if (!string.IsNullOrWhiteSpace(previewPath) && System.IO.File.Exists(previewPath) && IsImagePath(previewPath))
        {
            grid.Add(new Image
            {
                Source = ImageSource.FromFile(previewPath),
                Aspect = Aspect.AspectFit,
                HeightRequest = 134
            });
        }
        else
        {
            grid.Add(new Label
            {
                Text = string.IsNullOrWhiteSpace(uploadedUrl) ? "{    +    }" : icon,
                TextColor = string.IsNullOrWhiteSpace(uploadedUrl) ? Color.FromArgb("#D7D7D7") : CustomerUi.Orange,
                FontSize = CustomerUi.TitleSize,
                FontFamily = CustomerUi.FontDisplay,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        if (!string.IsNullOrWhiteSpace(previewPath) && !IsImagePath(previewPath))
        {
            grid.Add(new Label
            {
                Text = $"Selected video\n{System.IO.Path.GetFileName(previewPath)}",
                TextColor = CustomerUi.Dark,
                FontSize = 13,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        grid.Add(new Button
        {
            Text = string.IsNullOrWhiteSpace(uploadedUrl) ? "Upload" : "Replace",
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            FontSize = 11,
            CornerRadius = 14,
            HeightRequest = 30,
            WidthRequest = 82,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(0, 0, 10, 10),
            Command = command
        });

        var stack = new VerticalStackLayout { Spacing = 6 };
        stack.Add(Text(title, 10, CustomerUi.Muted));
        stack.Add(WhiteCard(grid, 0, new Thickness(0)));
        return stack;
    }

    private static bool IsImagePath(string path)
    {
        var extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
        return extension is ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp";
    }

    public static string Money(decimal amount)
    {
        return string.Format(CultureInfo.GetCultureInfo("en-PH"), "PHP {0:N0}", amount);
    }
}

internal sealed class LocationAccessPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _closed = new();
    private bool _isBusy;

    private LocationAccessPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Colors.White;

        var accessButton = BookingVisuals.PrimaryButton("ACCESS LOCATION", new Command(async () => await AccessLocationAsync()));
        var cancelButton = new Button
        {
            Text = "Not now",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Muted,
            HeightRequest = 42,
            Command = new Command(async () => await CloseAsync())
        };

        var card = new VerticalStackLayout
        {
            Padding = new Thickness(24, 26),
            Spacing = 14,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Image
                {
                    Source = ImageSource.FromUri(new Uri("https://img.icons8.com/color/240/google-maps-new.png")),
                    HeightRequest = 142,
                    WidthRequest = 142
                },
                accessButton,
                cancelButton,
                BookingVisuals.Text("TURN ON OR WILL ONLY USE YOUR APP\nWHEN USING THIS APP", 9, CustomerUi.Muted)
            }
        };

        Content = card;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _closed.TrySetResult(true);
    }

    private async Task AccessLocationAsync()
    {
        if (_isBusy)
        {
            return;
        }

        _isBusy = true;
        try
        {
            await BookingVisuals.UpdateCurrentLocationAsync(this);
            await CloseAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Location unavailable", ex.Message, "OK");
        }
        finally
        {
            _isBusy = false;
        }
    }

    private async Task CloseAsync()
    {
        _closed.TrySetResult(true);
        if (Navigation.ModalStack.Contains(this))
        {
            await Navigation.PopModalAsync();
        }
    }

    public static async Task ShowAsync()
    {
        var shell = Shell.Current ?? throw new InvalidOperationException("BikeMate navigation is not ready yet.");
        var page = new LocationAccessPage();
        await shell.Navigation.PushModalAsync(page);
        await page._closed.Task;
    }
}

public sealed class BookingFillUpPage : CustomerPageBase
{
    private Editor? _detailsEditor;

    public BookingFillUpPage()
    {
        Title = "Fill-Up";
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            BookingDraft.Customer ??= await CustomerApiClient.GetCustomerAsync();
            if (BookingDraft.Services.Count == 0)
            {
                BookingDraft.Services = await CustomerApiClient.SearchServicesAsync();
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load fill-up details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14, 0, 14, 16), Spacing = 9 };
        body.Add(BookingVisuals.FlowHeader("Fill-Up"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        body.Add(BookingVisuals.PickerRow("Brand", BrandOptions(), BookingDraft.Brand, selected =>
        {
            BookingDraft.Brand = selected;
            var model = BookingDraft.Customer?.Motorcycles.FirstOrDefault(x => x.Brand == selected)?.Model;
            if (!string.IsNullOrWhiteSpace(model))
            {
                BookingDraft.Model = model;
            }

            Render();
            return Task.CompletedTask;
        }));
        body.Add(BookingVisuals.PickerRow("Model", ModelOptions(), BookingDraft.Model, selected =>
        {
            BookingDraft.Model = selected;
            Render();
            return Task.CompletedTask;
        }));
        body.Add(BookingVisuals.PickerRow("Problem Category", ProblemOptions(), BookingDraft.ProblemCategory, selected =>
        {
            BookingDraft.ProblemCategory = selected;
            BookingDraft.SelectedShopServiceId = BookingDraft.SelectedService()?.ShopServiceId;
            Render();
            return Task.CompletedTask;
        }));

        _detailsEditor = new Editor
        {
            Text = BookingDraft.OtherDetails,
            Placeholder = "Other Details *",
            HeightRequest = 178,
            FontSize = 11,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Orange,
            BackgroundColor = Colors.Transparent
        };
        _detailsEditor.TextChanged += (_, e) => BookingDraft.OtherDetails = e.NewTextValue ?? "";
        body.Add(BookingVisuals.WhiteCard(_detailsEditor, 4, new Thickness(8)));
        body.Add(new BoxView { HeightRequest = 8, Opacity = 0 });
        body.Add(BookingVisuals.PrimaryButton("Continue", new Command(async () =>
        {
            BookingDraft.SelectedShopServiceId = BookingDraft.SelectedService()?.ShopServiceId;
            await Shell.Current.GoToAsync(nameof(BookingServiceTypePage));
        })));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static IReadOnlyList<string> BrandOptions()
    {
        var brands = BookingDraft.Customer?.Motorcycles.Select(x => x.Brand).Distinct().ToArray();
        return brands is { Length: > 0 }
            ? brands
            : ["Trek", "Specialized", "Giant", "Merida", "Cannondale", "Scott", "Honda"];
    }

    private static IReadOnlyList<string> ModelOptions()
    {
        var models = BookingDraft.Customer?.Motorcycles
            .Where(x => string.Equals(x.Brand, BookingDraft.Brand, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.Model)
            .Distinct()
            .ToArray();
        return models is { Length: > 0 }
            ? models
            : ["Specialized Tarmac SL8", "Factor OSTRO VAM", "Scott Addict RC", "Dt. Swiss 6 1800", "Trek Domane", "Specialized Roubaix", "Click 125i"];
    }

    private static IReadOnlyList<string> ProblemOptions()
    {
        return
        [
            "Tire Problem",
            "Brake Adjustment",
            "Gear Shifting Issue",
            "Accessory Installation",
            "Chain Maintenance",
            "General Tune-up"
        ];
    }

}

public sealed class BookingServiceTypePage : CustomerPageBase
{
    public BookingServiceTypePage()
    {
        Title = "Service Type";
        Render();
    }

    private void Render()
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14, 0, 14, 16), Spacing = 14 };
        body.Add(BookingVisuals.FlowHeader("Service Type"));

        var intro = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        intro.Add(BookingVisuals.Text("Please choose your preferred service type.\n\nOn-Site Repair: A certified mechanic will come to your location for quick fixes like tire changes, brake adjustments, or gear tuning. Ideal for minor repairs or maintenance.\n\nPick-Up Repair: We'll pick up your bike and bring it to our partner shop for in-depth inspection and repairs at our workshop, then return it to you. Best for major repairs, overhauls, or if you're short on time.", 11, CustomerUi.Dark), 0, 0);
        intro.Add(new Image
        {
            Source = ImageSource.FromUri(new Uri("https://img.icons8.com/color/144/maintenance.png")),
            WidthRequest = 74,
            HeightRequest = 120,
            VerticalOptions = LayoutOptions.Start
        }, 1, 0);
        body.Add(intro);

        body.Add(ServiceTypeCard("On-Site Repair", "Mechanic visits your location for quick roadside or home repairs."));
        body.Add(ServiceTypeCard("Pick-up Repair", "Shop picks up the bike for deeper workshop inspection and repair."));
        body.Add(new Label
        {
            Text = "Not sure what to choose?",
            TextColor = CustomerUi.Dark,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 8, 0, 0)
        });
        body.Add(new Label
        {
            Text = "No problem! Share your bike issue, and we'll suggest the best service for you.",
            TextColor = CustomerUi.Muted,
            FontSize = 11,
            HorizontalTextAlignment = TextAlignment.Center
        });
        body.Add(BookingVisuals.PrimaryButton("Continue", new Command(async () => await Shell.Current.GoToAsync(nameof(BookingSchedulePage)))));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View ServiceTypeCard(string title, string subtitle)
    {
        var selected = string.Equals(BookingDraft.ServiceType, title, StringComparison.OrdinalIgnoreCase);
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(new Label
        {
            Text = selected ? "(o)" : "( )",
            TextColor = CustomerUi.Orange,
            FontSize = 18,
            VerticalTextAlignment = TextAlignment.Center
        }, 0, 0);
        var text = new VerticalStackLayout { Spacing = 2, Margin = new Thickness(8, 0, 0, 0) };
        text.Add(BookingVisuals.Text(title, 12, selected ? CustomerUi.Orange : CustomerUi.Dark, FontAttributes.Bold));
        text.Add(BookingVisuals.Text(subtitle, 10, CustomerUi.Muted));
        grid.Add(text, 1, 0);

        var card = BookingVisuals.WhiteCard(grid, 6, new Thickness(10));
        card.BackgroundColor = selected ? Color.FromArgb("#FFF2EC") : Colors.White;
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.ServiceType = title;
                Shell.Current.CurrentPage.Dispatcher.Dispatch(() =>
                {
                    if (Shell.Current.CurrentPage is BookingServiceTypePage page)
                    {
                        page.Render();
                    }
                });
            })
        });
        return card;
    }
}

public sealed class BookingSchedulePage : CustomerPageBase
{
    public BookingSchedulePage()
    {
        Title = "Scheduling";
        Render();
    }

    private void Render()
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14, 0, 14, 16), Spacing = 14 };
        body.Add(BookingVisuals.FlowHeader("Scheduling"));
        body.Add(CalendarCard());
        body.Add(BookingVisuals.PickerRow("Time", TimeOptions(), BookingDraft.ScheduledAt.ToString("h:mm tt", CultureInfo.InvariantCulture), selected =>
        {
            if (DateTime.TryParse(selected, CultureInfo.InvariantCulture, out var parsed))
            {
                BookingDraft.ScheduledAt = BookingDraft.ScheduledAt.Date.Add(parsed.TimeOfDay);
                Render();
            }

            return Task.CompletedTask;
        }));
        body.Add(new BoxView { HeightRequest = 64, Opacity = 0 });
        body.Add(BookingVisuals.PrimaryButton("Continue", new Command(async () => await Shell.Current.GoToAsync(nameof(BookingUploadPage)))));
        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static IReadOnlyList<string> TimeOptions()
    {
        return ["7:00 AM", "8:30 AM", "10:00 AM", "1:30 PM", "3:00 PM", "5:00 PM"];
    }

    private View CalendarCard()
    {
        var month = new DateTime(BookingDraft.ScheduledAt.Year, BookingDraft.ScheduledAt.Month, 1);
        var root = new VerticalStackLayout { Spacing = 12 };
        var header = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        header.Add(BookingVisuals.Text(month.ToString("MMMM yyyy", CultureInfo.InvariantCulture), 13, Colors.Black, FontAttributes.Bold), 0, 0);
        header.Add(BookingVisuals.Text("<", 13, CustomerUi.Muted), 1, 0);
        header.Add(BookingVisuals.Text(">", 13, CustomerUi.Muted), 2, 0);
        root.Add(header);

        var grid = new Grid
        {
            RowSpacing = 8,
            ColumnSpacing = 4,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        for (var i = 0; i < 6; i++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        }

        var startOffset = (int)month.DayOfWeek;
        var cursor = month.AddDays(-startOffset);
        for (var index = 0; index < 42; index++)
        {
            var date = cursor.AddDays(index);
            var selected = date.Date == BookingDraft.ScheduledAt.Date;
            var label = new Label
            {
                Text = date.Day.ToString(CultureInfo.InvariantCulture),
                FontSize = 11,
                TextColor = selected ? Colors.White : date.Month == month.Month ? CustomerUi.Dark : Color.FromArgb("#C8C8C8"),
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            };
            var cell = new Border
            {
                WidthRequest = 30,
                HeightRequest = 30,
                Stroke = Colors.Transparent,
                StrokeShape = new RoundRectangle { CornerRadius = 15 },
                BackgroundColor = selected ? CustomerUi.Orange : Colors.Transparent,
                Content = label
            };
            var chosen = date;
            cell.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(() =>
                {
                    BookingDraft.ScheduledAt = chosen.Date.Add(BookingDraft.ScheduledAt.TimeOfDay);
                    Render();
                })
            });
            grid.Add(cell, index % 7, index / 7);
        }
        root.Add(grid);

        return new Border
        {
            Margin = new Thickness(16, 0),
            Padding = new Thickness(16),
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 2 },
            BackgroundColor = Colors.White,
            Shadow = new Shadow { Brush = Brush.Black, Opacity = 0.13f, Radius = 14, Offset = new Point(0, 8) },
            Content = root
        };
    }

    private async Task SelectTimeAsync()
    {
        var times = new[] { "7:00 AM", "8:30 AM", "10:00 AM", "1:30 PM", "3:00 PM", "5:00 PM" };
        var selected = await BookingOptionSheet.ShowAsync("Select Time", times, BookingDraft.ScheduledAt.ToString("h:mm tt", CultureInfo.InvariantCulture));
        if (!string.IsNullOrWhiteSpace(selected) && DateTime.TryParse(selected, CultureInfo.InvariantCulture, out var parsed))
        {
            BookingDraft.ScheduledAt = BookingDraft.ScheduledAt.Date.Add(parsed.TimeOfDay);
            Render();
        }
    }
}

public sealed class BookingUploadPage : CustomerPageBase
{
    public BookingUploadPage()
    {
        Title = "Upload";
        Render();
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14, 0, 14, 16), Spacing = 16 };
        body.Add(BookingVisuals.FlowHeader("Upload"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        body.Add(BookingVisuals.UploadBox("OK", "Upload device issue pictures*", BookingDraft.ImageMediaUrl, BookingDraft.ImagePreviewPath, new Command(async () => await UploadAsync("image"))));
        body.Add(BookingVisuals.UploadBox("PLAY", "Upload device issue videos*", BookingDraft.VideoMediaUrl, BookingDraft.VideoPreviewPath, new Command(async () => await UploadAsync("video"))));
        body.Add(new BoxView { HeightRequest = 52, Opacity = 0 });
        body.Add(BookingVisuals.PrimaryButton("Continue", new Command(async () => await Shell.Current.GoToAsync(nameof(BookingConfirmationPage)))));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task UploadAsync(string mediaType)
    {
        try
        {
            var result = await PickMediaAsync(mediaType);

            if (result is null)
            {
                return;
            }

            var previewPath = await CopyPickedFileToCacheAsync(result, mediaType);
            var uploaded = await CustomerApiClient.UploadFileAsync(result, "booking");
            var uploadUrl = uploaded.Url;

            if (mediaType == "image")
            {
                BookingDraft.ImagePreviewPath = previewPath;
                BookingDraft.ImageMediaUrl = uploadUrl;
                await DisplayAlertAsync("Final picture?", "Make sure that the picture captures the problem well.", "Yes");
            }
            else
            {
                BookingDraft.VideoPreviewPath = previewPath;
                BookingDraft.VideoMediaUrl = uploadUrl;
                await DisplayAlertAsync("Final video?", "Make sure that the video captures the problem well.", "Yes");
            }

            Render();
        }
        catch (Exception ex)
        {
            Render($"Upload failed. {ex.Message}");
        }
    }

    private static async Task<FileResult?> PickMediaAsync(string mediaType)
    {
        return await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = mediaType == "image" ? "Select issue picture" : "Select issue video",
            FileTypes = mediaType == "image" ? FilePickerFileType.Images : FilePickerFileType.Videos
        });
    }

    private static async Task<string> CopyPickedFileToCacheAsync(FileResult result, string mediaType)
    {
        var extension = System.IO.Path.GetExtension(result.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = mediaType == "image" ? ".jpg" : ".mp4";
        }

        var fileName = $"booking-{mediaType}-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}";
        var targetPath = System.IO.Path.Combine(FileSystem.CacheDirectory, fileName);
        await using var input = await result.OpenReadAsync();
        await using var output = System.IO.File.Create(targetPath);
        await input.CopyToAsync(output);
        return targetPath;
    }
}

public sealed class BookingConfirmationPage : CustomerPageBase
{
    public BookingConfirmationPage()
    {
        Title = "Confirmation";
        Render("Preparing booking summary...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            BookingDraft.Customer ??= await CustomerApiClient.GetCustomerAsync();
            if (BookingDraft.Services.Count == 0)
            {
                BookingDraft.Services = await CustomerApiClient.SearchServicesAsync();
            }
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load confirmation details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var service = BookingDraft.SelectedService();
        var body = new VerticalStackLayout { Padding = new Thickness(14, 0, 14, 16), Spacing = 12 };
        body.Add(BookingVisuals.FlowHeader("Confirmation"));
        body.Add(new BoxView { HeightRequest = 1, BackgroundColor = Color.FromArgb("#D7D7D7") });
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        body.Add(SummaryLine("Location", BookingDraft.ConfirmationAddress()));
        body.Add(SummaryLine("Device Info", $"Brand: {BookingDraft.Brand}\nModel: {BookingDraft.Model}"));
        body.Add(SummaryLine("Issue", $"Problem Category: {BookingDraft.ProblemCategory}\nOther Details: {BookingDraft.OtherDetails}"));
        body.Add(SummaryLine("Service", $"Service Type: {BookingDraft.ServiceType}\nSchedule: {BookingDraft.ScheduledAt:MM/dd/yyyy}\nTime: {BookingDraft.ScheduledAt:h:mm tt}"));
        body.Add(SummaryLine("Repair Shop", BookingDraft.SelectedShopId is null ? "Choose a shop next" : "Shop selected"));
        body.Add(SummaryLine("Estimated Total", BookingDraft.SelectedShopId is null ? "Set by the selected shop service" : BookingVisuals.Money(service?.BasePrice ?? 0m)));
        body.Add(new BoxView { HeightRequest = 28, Opacity = 0 });
        body.Add(BookingVisuals.PrimaryButton("Find repair shop", new Command(async () => await ContinueAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View SummaryLine(string title, string value)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(BookingVisuals.Text(title, 11, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(value, 10, Colors.Black));
        return stack;
    }

    private async Task ContinueAsync()
    {
        try
        {
            await BookingFlowActions.EnsureRequestAsync(true);
            await Shell.Current.GoToAsync(nameof(BookingSearchShopPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Booking failed", ex.Message, "OK");
        }
    }
}

public sealed class BookingSearchShopPage : CustomerPageBase
{
    private bool _navigated;

    public BookingSearchShopPage()
    {
        Title = "Booking";
        Render();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_navigated)
        {
            return;
        }

        _navigated = true;
        await Task.Delay(900);
        await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
    }

    private void Render()
    {
        var root = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto)
            },
            BackgroundColor = Colors.White
        };
        root.Add(BookingVisuals.MapPanel(540), 0, 0);
        root.Add(new VerticalStackLayout
        {
            Padding = new Thickness(14, 12, 14, 18),
            Spacing = 8,
            Children =
            {
                new Label
                {
                    Text = "Searching for available repair\nshop in the area...",
                    TextColor = CustomerUi.Dark,
                    FontSize = 11,
                    HorizontalTextAlignment = TextAlignment.Center
                },
                BookingVisuals.PrimaryButton("Please wait...", new Command(async () => await Shell.Current.GoToAsync(nameof(StoreSelectionPage))))
            }
        }, 0, 1);

        SetScaffold(root, "Home", false);
    }
}

public sealed class StoreSelectionPage : CustomerPageBase
{
    private IReadOnlyList<ShopSummaryDto> _shops = [];
    private IReadOnlyList<ShopServiceDto> _services = [];

    public StoreSelectionPage()
    {
        Title = "Choose a store";
        Render("Loading nearby repair shops...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _shops = await CustomerApiClient.GetShopsAsync();
            _services = await CustomerApiClient.SearchServicesAsync();
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load nearby stores. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var body = new VerticalStackLayout { Padding = new Thickness(14, 6, 14, 16), Spacing = 9 };
        body.Add(Header());
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        if (_shops.Count == 0)
        {
            body.Add(StoreCard(null, "E&E Bike Repair Shop", "4.9 stars\n4 years in the business", "San Pedro, Laguna"));
            body.Add(StoreCard(null, "MUMAR Bike Repair Shop", "4.5 stars\n2 years in the business", "Manila"));
            body.Add(StoreCard(null, "Parasan ni Divata Repair Shop", "4.6 stars\n12 years in the business", "Mega Manila"));
        }
        else
        {
            foreach (var shop in _shops)
            {
                body.Add(StoreCard(shop, shop.ShopName, "4.9 stars\n4 years in the business", shop.City ?? shop.AddressLine ?? "Nearby"));
            }
        }

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private static View Header()
    {
        var root = new VerticalStackLayout { Spacing = 8 };
        var top = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        top.Add(new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Transparent,
            TextColor = CustomerUi.Dark,
            FontSize = 13,
            WidthRequest = 56,
            HeightRequest = 40,
            CornerRadius = 20,
            Padding = new Thickness(0),
            Command = new Command(async () => await Shell.Current.GoToAsync(".."))
        }, 0, 0);
        top.Add(new Label
        {
            Text = "Choose a store",
            TextColor = CustomerUi.Dark,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        }, 1, 0);
        root.Add(top);
        root.Add(new Entry
        {
            Placeholder = "Type keywords",
            FontSize = 11,
            HeightRequest = 36,
            BackgroundColor = Color.FromArgb("#F1F1F1")
        });
        return root;
    }

    private View StoreCard(ShopSummaryDto? shop, string name, string meta, string area)
    {
        var service = shop is null
            ? BookingDraft.SelectedService()
            : _services.FirstOrDefault(x => x.ShopId == shop.ShopId) ?? BookingDraft.SelectedService();

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(new Image
        {
            Source = ImageSource.FromUri(new Uri(BookingVisuals.ShopImage)),
            WidthRequest = 64,
            HeightRequest = 64,
            Aspect = Aspect.AspectFill
        }, 0, 0);
        var details = new VerticalStackLayout { Spacing = 5, Margin = new Thickness(8, 0, 0, 0) };
        details.Add(BookingVisuals.Text(name, 11, CustomerUi.Dark, FontAttributes.Bold));
        details.Add(BookingVisuals.Text($"{meta}\n{area}", 9, CustomerUi.Muted));
        var actions = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8
        };
        actions.Add(new Button
        {
            Text = "View Profile",
            BackgroundColor = CustomerUi.Orange,
            TextColor = Colors.White,
            FontSize = 11,
            CornerRadius = 6,
            HeightRequest = 32,
            Command = new Command(async () =>
            {
                if (shop is not null)
                {
                    await Shell.Current.GoToAsync($"{nameof(StoreDetailsPage)}?shopId={shop.ShopId}");
                }
                else
                {
                    await Shell.Current.GoToAsync(nameof(StoreDetailsPage));
                }
            })
        }, 0, 0);
        actions.Add(new Button
        {
            Text = "Book here!",
            BackgroundColor = Color.FromArgb("#22A447"),
            TextColor = Colors.White,
            FontSize = 11,
            CornerRadius = 6,
            HeightRequest = 32,
            Command = new Command(async () => await BookHereAsync(shop?.ShopId, service?.ShopServiceId))
        }, 1, 0);
        details.Add(actions);
        grid.Add(details, 1, 0);
        return BookingVisuals.WhiteCard(grid, 2, new Thickness(8));
    }

    private async Task BookHereAsync(int? shopId, int? serviceId)
    {
        try
        {
            if (shopId is not null)
            {
                await BookingFlowActions.SelectShopAsync(shopId.Value, serviceId ?? BookingDraft.SelectedShopServiceId);
            }

            await DisplayAlertAsync("Repair shop selected", "Choose the exact service on the shop profile before payment.", "OK");
            await Shell.Current.GoToAsync(shopId is null ? nameof(StoreDetailsPage) : $"{nameof(StoreDetailsPage)}?shopId={shopId}");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Store selection failed", ex.Message, "OK");
        }
    }
}

public sealed class StoreDetailsPage : CustomerPageBase, IQueryAttributable
{
    private int _shopId;
    private ShopDetailsDto? _shop;
    private IReadOnlyList<ShopServiceDto> _services = [];

    public StoreDetailsPage()
    {
        Title = "Store Details";
        Render("Loading store details...");
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("shopId", out var value) && int.TryParse(Uri.UnescapeDataString(value?.ToString() ?? ""), out var shopId))
        {
            _shopId = shopId;
            BookingDraft.SelectedShopId = shopId;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _shopId = _shopId == 0 ? BookingDraft.SelectedShopId ?? 0 : _shopId;
            if (_shopId > 0)
            {
                _shop = await CustomerApiClient.GetShopDetailsAsync(_shopId);
                _services = await CustomerApiClient.GetShopServicesAsync(_shopId);
                if (BookingDraft.SelectedShopServiceId is null || _services.All(x => x.ShopServiceId != BookingDraft.SelectedShopServiceId))
                {
                    BookingDraft.SelectedShopServiceId = _services.FirstOrDefault()?.ShopServiceId;
                }
            }
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load store details. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var shopName = _shop?.ShopName ?? "E&E Bike Repair Shop";
        var address = _shop is null
            ? "B4 L22 18 Bayani St., Brgy. San Vicente, San Pedro"
            : $"{_shop.AddressLine}, {_shop.City}";

        var body = new VerticalStackLayout { Padding = new Thickness(14, 6, 14, 16), Spacing = 10 };
        body.Add(Header("Store Details"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        body.Add(new Image { Source = ImageSource.FromUri(new Uri(BookingVisuals.ShopImage)), HeightRequest = 115, Aspect = Aspect.AspectFill });
        body.Add(BookingVisuals.Text(shopName, 13, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text("4.9 stars in the business", 10, Color.FromArgb("#22A447")));
        body.Add(BookingVisuals.Text($"{shopName} is your trusted local bike workshop. Specializing in professional bicycle repairs, maintenance, and upgrades at affordable prices.", 10, CustomerUi.Dark));
        body.Add(BookingVisuals.PrimaryButton("View Top Technician", new Command(async () => await ContinueToTechnicianAsync())));
        body.Add(BookingVisuals.Text("Store Hours", 11, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text("Monday - Saturday: 9:00 AM - 7:00 PM\nSunday: Closed", 10, CustomerUi.Dark));
        body.Add(BookingVisuals.Text($"Address\n{address}", 11, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text("Services Offered", 11, CustomerUi.Dark, FontAttributes.Bold));
        if (_services.Count == 0)
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text("No active services are available from this shop yet.", 11, CustomerUi.Muted), 4, new Thickness(10)));
        }
        else
        {
            foreach (var service in _services.Take(6))
            {
                body.Add(ServiceChoice(service));
            }
        }

        body.Add(BookingVisuals.Text("Contact Us\nMobile: 0912 345 6789\nLandline: (02) 8123 4567\nEmail: ee.repairshop@gmail.com", 10, CustomerUi.Dark));
        body.Add(BookingVisuals.PrimaryButton("Like what you see?", new Command(async () => await DisplayAlertAsync("Booking Popup", "Successfully added to favorites", "Ok"))));
        body.Add(BookingVisuals.PrimaryButton("Continue to technician", new Command(async () => await ContinueToTechnicianAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View ServiceChoice(ShopServiceDto service)
    {
        var selected = BookingDraft.SelectedShopServiceId == service.ShopServiceId;
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 8
        };
        grid.Add(BookingVisuals.Text(selected ? "(o)" : "( )", 13, CustomerUi.Orange, FontAttributes.Bold), 0, 0);
        var text = new VerticalStackLayout { Spacing = 2 };
        text.Add(BookingVisuals.Text(service.ServiceName, 13, CustomerUi.Dark, FontAttributes.Bold));
        text.Add(BookingVisuals.Text($"{service.CategoryName} - {service.EstimatedMinutes} min", 11, CustomerUi.Muted));
        if (!string.IsNullOrWhiteSpace(service.ServiceDescription))
        {
            text.Add(BookingVisuals.Text(service.ServiceDescription, 11, CustomerUi.Muted));
        }

        grid.Add(text, 1, 0);
        grid.Add(BookingVisuals.Text(BookingVisuals.Money(service.BasePrice), 13, CustomerUi.Orange, FontAttributes.Bold), 2, 0);
        var card = BookingVisuals.WhiteCard(grid, 4, new Thickness(10));
        card.BackgroundColor = selected ? Color.FromArgb("#FFF2EC") : Colors.White;
        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() =>
            {
                BookingDraft.SelectedShopServiceId = service.ShopServiceId;
                Render();
            })
        });
        return card;
    }

    private async Task ContinueToTechnicianAsync()
    {
        try
        {
            if (_shopId <= 0)
            {
                await DisplayAlertAsync("Choose a shop", "Select a repair shop before continuing.", "OK");
                await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
                return;
            }

            var serviceId = BookingDraft.SelectedShopServiceId ?? _services.FirstOrDefault()?.ShopServiceId;
            if (serviceId is null)
            {
                await DisplayAlertAsync("Choose a service", "Select an available service before payment.", "OK");
                return;
            }

            await BookingFlowActions.SelectShopAsync(_shopId, serviceId);
            await Shell.Current.GoToAsync(nameof(TaskerProfilePage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Shop selection failed", ex.Message, "OK");
        }
    }
}

public sealed class TaskerProfilePage : CustomerPageBase
{
    private MechanicProfileDto? _mechanic;

    public TaskerProfilePage()
    {
        Title = "Technician Profile";
        Render("Loading technician profile...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            _mechanic = await CustomerApiClient.GetMechanicProfileAsync(1);
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load technician profile. {ex.Message}");
        }
    }

    private void Render(string? banner = null)
    {
        var name = _mechanic?.FullName ?? "Steve Jobs";
        var body = new VerticalStackLayout { Padding = new Thickness(14, 6, 14, 16), Spacing = 12 };
        body.Add(Header("Technician Profile"));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        body.Add(new Image
        {
            Source = ImageSource.FromUri(new Uri(BookingVisuals.MechanicImage)),
            HeightRequest = 94,
            WidthRequest = 94,
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Center
        });
        body.Add(new Label { Text = name, TextColor = CustomerUi.Dark, FontAttributes = FontAttributes.Bold, FontSize = 18, HorizontalTextAlignment = TextAlignment.Center });
        body.Add(new Label { Text = "Bike Expert", TextColor = CustomerUi.Orange, FontSize = 11, HorizontalTextAlignment = TextAlignment.Center });
        body.Add(ProfileStats(_mechanic));
        body.Add(BookingVisuals.Text("Experience & Specialties", 12, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text($"Hi, I'm {name.Split(' ').FirstOrDefault() ?? "your technician"}, with over {_mechanic?.YearsExperience ?? 5} years of motorcycle roadside and service repair experience.", 10, CustomerUi.Dark));
        body.Add(BookingVisuals.Text("Customer Ratings", 12, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(BookingVisuals.Text($"{(_mechanic?.AverageRating ?? 4.9m):0.0}  *****\nBased on {Math.Max(_mechanic?.TotalCompletedJobs ?? 20, 20)} ratings", 11, CustomerUi.Dark));
        body.Add(RatingBar("Work quality", 4.9));
        body.Add(RatingBar("Reliability", 4.9));
        body.Add(RatingBar("Punctuality", 4.8));
        body.Add(RatingBar("Solution", 5.0));
        body.Add(RatingBar("Value", 4.5));
        body.Add(BookingVisuals.Text("Customer Reviews", 12, CustomerUi.Dark, FontAttributes.Bold));
        body.Add(ReviewRow("Nicole", "Booking was quick and the technician arrived on time.", "*****"));
        body.Add(ReviewRow("David", "Clear updates and careful repair work.", "****-"));
        body.Add(BookingVisuals.PrimaryButton("Continue to payment", new Command(async () => await ConfirmRiderAsync())));

        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private async Task ConfirmRiderAsync()
    {
        try
        {
            if (BookingDraft.SelectedShopId is null || BookingDraft.SelectedShopServiceId is null)
            {
                await DisplayAlertAsync("Choose a service", "Select a repair shop and service before payment.", "OK");
                await Shell.Current.GoToAsync(nameof(StoreSelectionPage));
                return;
            }

            await BookingFlowActions.SelectShopAsync(BookingDraft.SelectedShopId.Value, BookingDraft.SelectedShopServiceId);
            await DisplayAlertAsync("Ready for payment", "Your shop and service are set. Complete secure payment to confirm tracking.", "OK");
            await Shell.Current.GoToAsync(nameof(PaymentOptionsPage));
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Booking update failed", ex.Message, "OK");
        }
    }

    private static View ProfileStats(MechanicProfileDto? mechanic)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(Stat("PHP 50.00", "Per hour"), 0, 0);
        grid.Add(Stat($"{(mechanic?.AverageRating ?? 4.9m):0.0}", "Rating"), 1, 0);
        grid.Add(Stat($"{mechanic?.YearsExperience ?? 2}h", "Min required"), 2, 0);
        return BookingVisuals.WhiteCard(grid, 6, new Thickness(8));
    }

    private static View Stat(string value, string label)
    {
        return new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = value, TextColor = CustomerUi.Dark, FontSize = 13, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center },
                new Label { Text = label, TextColor = CustomerUi.Muted, FontSize = 11, HorizontalTextAlignment = TextAlignment.Center }
            }
        };
    }

    private static View RatingBar(string title, double value)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        grid.Add(BookingVisuals.Text(title, 10, CustomerUi.Dark), 0, 0);
        grid.Add(BookingVisuals.Text(value.ToString("0.0", CultureInfo.InvariantCulture), 10, CustomerUi.Dark), 1, 0);
        return grid;
    }

    private static View ReviewRow(string name, string body, string stars)
    {
        var stack = new VerticalStackLayout { Spacing = 3 };
        stack.Add(BookingVisuals.Text($"{name}   {stars}", 10, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(body, 9, CustomerUi.Dark));
        return BookingVisuals.WhiteCard(stack, 4, new Thickness(8));
    }
}

public sealed class BookingTrackMapPage : CustomerPageBase
{
    private ServiceRequestDto? _request;
    private LiveLocationDto? _location;

    public BookingTrackMapPage()
    {
        Title = "Track";
        Render("Loading tracking details...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            if (BookingDraft.RequestId > 0)
            {
                _request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
                if (!await CustomerPaymentGate.EnsurePaidOrRedirectAsync(this, BookingDraft.RequestId))
                {
                    return;
                }

                _location = await CustomerApiClient.GetLatestRequestLocationAsync(BookingDraft.RequestId);
            }
            Render();
        }
        catch (Exception ex)
        {
            Render($"Connect the API to load tracking details. {ex.Message}");
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
        root.Add(Header("Track Order", true, "Return home", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))), 0, 0);
        var map = new Grid { BackgroundColor = Color.FromArgb("#EEF1F4") };
        var riderLat = _location?.Latitude ?? BookingDraft.Latitude ?? 14.599512m;
        var riderLng = _location?.Longitude ?? BookingDraft.Longitude ?? 120.984222m;
        var destinationLat = _request?.ServiceLatitude ?? BookingDraft.Latitude;
        var destinationLng = _request?.ServiceLongitude ?? BookingDraft.Longitude;
        var route = BuildRouteSummary(riderLat, riderLng, destinationLat, destinationLng, _location is not null);
        var mapSource = _location is not null && destinationLat is not null && destinationLng is not null
            ? BookingVisuals.GoogleDirectionsSource(riderLat, riderLng, destinationLat.Value, destinationLng.Value)
            : BookingVisuals.GoogleMapSource(riderLat, riderLng);
        map.Add(new WebView
        {
            Source = mapSource
        });
        map.Add(new Label
        {
            Text = _location is null ? "Waiting for rider live location" : "Rider to service location",
            TextColor = Color.FromArgb("#503CFF"),
            BackgroundColor = Color.FromRgba(255, 255, 255, 0.86),
            FontSize = 13,
            Padding = new Thickness(10, 6),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(10)
        });
        root.Add(map, 0, 1);

        var card = new VerticalStackLayout { Padding = new Thickness(14), Spacing = 8 };
        if (!string.IsNullOrWhiteSpace(banner))
        {
            card.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        var riderRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10
        };
        riderRow.Add(Avatar("R", 42, CustomerUi.LightOrange), 0, 0);
        var riderText = new VerticalStackLayout { Spacing = 2 };
        riderText.Add(BookingVisuals.Text(_request?.MechanicName ?? "Assigned rider", 13, CustomerUi.Dark, FontAttributes.Bold));
        riderText.Add(BookingVisuals.Text(_location is null ? "Location not shared yet" : "Live location active", 10, _location is null ? CustomerUi.Muted : CustomerUi.Orange));
        riderRow.Add(riderText, 1, 0);
        riderRow.Add(new Border
        {
            BackgroundColor = _location is null ? Color.FromArgb("#F2F2F2") : Color.FromArgb("#EAF8EF"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(10, 5),
            Content = new Label
            {
                Text = _location is null ? "Waiting" : "On map",
                FontSize = 11,
                TextColor = _location is null ? CustomerUi.Muted : Color.FromArgb("#167A3A"),
                FontAttributes = FontAttributes.Bold
            }
        }, 2, 0);
        card.Add(riderRow);

        card.Add(RouteLine("From", route.Origin));
        card.Add(RouteLine("To", route.Destination));

        var stats = new Grid { ColumnSpacing = 8 };
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        stats.Add(RouteStat("Distance", route.Distance), 0, 0);
        stats.Add(RouteStat("ETA", route.Time), 1, 0);
        card.Add(stats);

        var buttons = new Grid { ColumnSpacing = 8 };
        buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        buttons.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        buttons.Add(BookingVisuals.PrimaryButton("View order status", new Command(async () => await Shell.Current.GoToAsync(nameof(TrackOrderPage)))), 0, 0);
        buttons.Add(new Button
        {
            Text = "Return home",
            BackgroundColor = Colors.White,
            TextColor = CustomerUi.Dark,
            BorderColor = CustomerUi.Border,
            BorderWidth = 1,
            CornerRadius = 8,
            HeightRequest = 48,
            Command = new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))
        }, 1, 0);
        card.Add(buttons);
        root.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = CustomerUi.Border,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Padding = new Thickness(0),
            Margin = new Thickness(12),
            Content = card
        }, 0, 2);

        SetScaffold(root, "Schedule", false);
    }

    private (string Origin, string Destination, string Distance, string Time) BuildRouteSummary(
        decimal riderLatitude,
        decimal riderLongitude,
        decimal? destinationLatitude,
        decimal? destinationLongitude,
        bool hasRiderLocation)
    {
        var destination = _request?.ServiceLocationAddress ?? BookingDraft.AddressLine;
        if (string.IsNullOrWhiteSpace(destination))
        {
            destination = "Service location";
        }

        if (!hasRiderLocation || destinationLatitude is null || destinationLongitude is null)
        {
            return ("Rider live location", destination, "--", "--");
        }

        var km = DistanceKm(riderLatitude, riderLongitude, destinationLatitude.Value, destinationLongitude.Value);
        var minutes = Math.Max(1, (int)Math.Ceiling((double)km / 18d * 60d));
        return ("Rider live location", destination, $"{km:0.##} km", $"{minutes} min");
    }

    private static View RouteLine(string label, string value)
    {
        var stack = new VerticalStackLayout { Spacing = 2 };
        stack.Add(BookingVisuals.Text(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(value, 12, CustomerUi.Dark));
        return stack;
    }

    private static View RouteStat(string label, string value)
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
                    BookingVisuals.Text(label.ToUpperInvariant(), 9, CustomerUi.Muted, FontAttributes.Bold),
                    BookingVisuals.Text(value, 15, CustomerUi.Dark, FontAttributes.Bold)
                }
            }
        };
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
}

public sealed class TrackOrderPage : CustomerPageBase
{
    private ServiceRequestDto? _request;
    private string? _banner;

    public TrackOrderPage()
    {
        Title = "Track Order";
        Render("Loading live order status...");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            if (BookingDraft.RequestId > 0)
            {
                _request = await CustomerApiClient.GetRequestAsync(BookingDraft.RequestId);
            }
            else
            {
                _request = (await CustomerApiClient.GetMyRequestsAsync()).OrderByDescending(x => x.CreatedAt).FirstOrDefault();
                BookingDraft.RequestId = _request?.RequestId ?? 0;
            }

            _banner = null;
            Render();
        }
        catch (Exception ex)
        {
            _banner = $"Connect the API to load rider status. {ex.Message}";
            Render(_banner);
        }
    }

    private void Render(string? banner = null)
    {
        var request = _request;
        var currentStatus = request?.CurrentStatus ?? "pending";
        var scheduledAt = request?.ScheduledAt?.ToLocalTime() ?? BookingDraft.ScheduledAt;
        var serviceName = request?.ServiceName ?? BookingDraft.SelectedService()?.ServiceName ?? "Basic Service";
        var shopName = request?.ShopName ?? "Waiting for repair shop";

        var body = new VerticalStackLayout { Padding = new Thickness(14, 6, 14, 16), Spacing = 12 };
        body.Add(Header("Track Order", true, "Return home", new Command(async () => await Shell.Current.GoToAsync("//CustomerHomePage"))));
        if (!string.IsNullOrWhiteSpace(banner))
        {
            body.Add(BookingVisuals.WhiteCard(BookingVisuals.Text(banner, 11, CustomerUi.Muted)));
        }

        body.Add(BookingVisuals.WhiteCard(new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 3,
                    Children =
                    {
                        BookingVisuals.Text(serviceName, 11, CustomerUi.Dark, FontAttributes.Bold),
                        BookingVisuals.Text($"Booking ID: {Math.Max(BookingDraft.RequestId, request?.RequestId ?? 0)}", 10, CustomerUi.Dark),
                        BookingVisuals.Text(shopName, 10, CustomerUi.Orange),
                        BookingVisuals.Text($"STATUS\n{CustomerPageBase.FormatStatus(currentStatus)}", 9, CustomerUi.Dark, FontAttributes.Bold),
                        BookingVisuals.Text($"DATE\n{scheduledAt:MMMM d, yyyy} ({scheduledAt:dddd})", 9, CustomerUi.Muted),
                        BookingVisuals.Text($"PICK-UP TIME\n{scheduledAt:h:mm tt}", 9, CustomerUi.Muted)
                    }
                },
                new Image { Source = ImageSource.FromUri(new Uri(CustomerUi.OnlineBikeRepairImage)), WidthRequest = 62, HeightRequest = 62 }
            }
        }, 6, new Thickness(12)));

        body.Add(StatusRow("Booking placed", request?.CreatedAt.ToLocalTime().ToString("MMM d, yyyy h:mm tt", CultureInfo.InvariantCulture) ?? "Waiting", IsAtLeast(currentStatus, "pending")));
        body.Add(StatusRow("Rider accepted", "Updated by rider side", IsAtLeast(currentStatus, "accepted")));
        body.Add(StatusRow("Rider en route", "Live location appears on Track map", IsAtLeast(currentStatus, "en_route")));
        body.Add(StatusRow("Rider arrived", "Rider reached service location", IsAtLeast(currentStatus, "arrived")));
        body.Add(StatusRow("Repair in progress", "Mechanic started the job", IsAtLeast(currentStatus, "in_progress")));
        body.Add(StatusRow("Completed", "Ready for review and receipt", IsAtLeast(currentStatus, "completed")));
        body.Add(new BoxView { HeightRequest = 80, Opacity = 0 });
        body.Add(BookingVisuals.PrimaryButton(IsAtLeast(currentStatus, "completed") ? "Continue" : "Refresh status", new Command(async () =>
        {
            if (IsAtLeast(currentStatus, "completed"))
            {
                await Shell.Current.GoToAsync(nameof(BookingRatingPage));
                return;
            }

            await LoadAsync();
        })));

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private static bool IsAtLeast(string status, string target)
    {
        return StatusRank(status) >= StatusRank(target);
    }

    private static int StatusRank(string status)
    {
        return status switch
        {
            "pending" => 1,
            "accepted" => 2,
            "en_route" => 3,
            "arrived" => 4,
            "in_progress" => 5,
            "completed" => 6,
            "cancelled" => 6,
            _ => 0
        };
    }

    private static View StatusRow(string title, string date, bool done)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        grid.Add(new Label
        {
            Text = done ? "o" : "-",
            TextColor = done ? CustomerUi.Orange : CustomerUi.Muted,
            FontSize = 18
        }, 0, 0);
        var stack = new VerticalStackLayout { Margin = new Thickness(9, 0, 0, 0), Spacing = 2 };
        stack.Add(BookingVisuals.Text(title, 11, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(BookingVisuals.Text(date, 9, CustomerUi.Muted));
        grid.Add(stack, 1, 0);
        return grid;
    }
}

public sealed class BookingRatingPage : CustomerPageBase
{
    private int _rating = 4;
    private Editor? _commentEditor;

    public BookingRatingPage()
    {
        Title = "Rating";
        Render();
    }

    private void Render()
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 54, 16, 18),
            Spacing = 16,
            BackgroundColor = CustomerUi.Orange
        };
        body.Add(new Image
        {
            Source = ImageSource.FromUri(new Uri(BookingVisuals.MechanicImage)),
            HeightRequest = 84,
            WidthRequest = 84,
            Aspect = Aspect.AspectFill,
            HorizontalOptions = LayoutOptions.Center
        });

        var card = new VerticalStackLayout { Padding = new Thickness(14, 22), Spacing = 12 };
        card.Add(new Label { Text = "Gregory Smith\nNadia's Repair", TextColor = CustomerUi.Dark, FontSize = 11, HorizontalTextAlignment = TextAlignment.Center });
        card.Add(new Label { Text = "How is your repair?", TextColor = CustomerUi.Dark, FontSize = 18, HorizontalTextAlignment = TextAlignment.Center });
        card.Add(new Label { Text = "Your feedback will help improve\nthe experience", TextColor = CustomerUi.Muted, FontSize = 11, HorizontalTextAlignment = TextAlignment.Center });
        card.Add(Stars());
        _commentEditor = new Editor
        {
            Placeholder = "Additional comments...",
            HeightRequest = 88,
            FontSize = 11,
            BackgroundColor = Color.FromArgb("#F2F2F6")
        };
        card.Add(_commentEditor);
        card.Add(BookingVisuals.PrimaryButton("Submit Review", new Command(async () => await SubmitAsync())));

        body.Add(new Border
        {
            BackgroundColor = Colors.White,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Content = card
        });

        SetScaffold(new ScrollView { Content = body }, "Schedule", false);
    }

    private View Stars()
    {
        var row = new HorizontalStackLayout { Spacing = 5, HorizontalOptions = LayoutOptions.Center };
        for (var i = 1; i <= 5; i++)
        {
            var value = i;
            var button = new Button
            {
                Text = i <= _rating ? "*" : "-",
                TextColor = Color.FromArgb("#FFC107"),
                BackgroundColor = Colors.Transparent,
                FontSize = 18,
                WidthRequest = 42,
                HeightRequest = 42,
                Padding = new Thickness(0),
                Command = new Command(() =>
                {
                    _rating = value;
                    Render();
                })
            };
            row.Add(button);
        }
        return row;
    }

    private async Task SubmitAsync()
    {
        try
        {
            if (BookingDraft.RequestId > 0)
            {
                await CustomerApiClient.SubmitReviewAsync(new CreateReviewDto(BookingDraft.RequestId, _rating, _commentEditor?.Text));
            }

            await DisplayAlertAsync("Thank you", "Your repair review has been submitted.", "OK");
            await Shell.Current.GoToAsync("//CustomerHomePage");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Review saved locally", $"The review UI is complete, but the API could not save it yet. {ex.Message}", "OK");
            await Shell.Current.GoToAsync("//CustomerHomePage");
        }
    }
}
