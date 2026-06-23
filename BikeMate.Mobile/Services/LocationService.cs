using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace BikeMate.Services;

internal sealed record EmergencyLocationSnapshot(
    decimal Latitude,
    decimal Longitude,
    string Address,
    bool IsGpsLocation,
    string? ErrorMessage,
    string LocationName = "");

internal static class LocationService
{
    public static async Task<EmergencyLocationSnapshot?> GetCurrentLocationAsync(Page page)
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
                return Failed("Location permission was denied. Open Android Settings > Apps > BikeMate > Permissions, then allow Location while using the app.");
            }

            var location = await GetBestAvailableLocationAsync();
            if (location is null)
            {
                return Failed("BikeMate could not get a GPS fix. Turn on phone Location, enable Google Location Accuracy or High accuracy, then try again near a window or outdoors.");
            }

            var (address, locationName) = await DescribeLocationAsync(location);
            return new EmergencyLocationSnapshot(
                (decimal)location.Latitude,
                (decimal)location.Longitude,
                string.IsNullOrWhiteSpace(address) ? $"Lat {location.Latitude:0.000000}, Lng {location.Longitude:0.000000}" : address,
                true,
                null,
                locationName);
        }
        catch (FeatureNotEnabledException)
        {
            return Failed("Phone Location is turned off. Turn on Location/GPS in Android quick settings, then try again.");
        }
        catch (FeatureNotSupportedException)
        {
            return Failed("This device does not support GPS location.");
        }
        catch (PermissionException)
        {
            return Failed("Location permission was blocked. Open Android Settings > Apps > BikeMate > Permissions and allow Location.");
        }
        catch (Exception ex)
        {
            return Failed($"Location failed: {ex.Message}");
        }
    }

    private static async Task<Location?> GetBestAvailableLocationAsync()
    {
        var requests = new[]
        {
            new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(30)),
            new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(20)),
            new GeolocationRequest(GeolocationAccuracy.Low, TimeSpan.FromSeconds(10))
        };

        foreach (var request in requests)
        {
            var location = await TryGetLocationAsync(request);
            if (IsUsable(location))
            {
                return location;
            }
        }

        var lastKnown = await TryGetLastKnownLocationAsync();
        return IsUsable(lastKnown) ? lastKnown : null;
    }

    private static async Task<Location?> TryGetLocationAsync(GeolocationRequest request)
    {
        try
        {
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch (FeatureNotEnabledException)
        {
            throw;
        }
        catch (PermissionException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<Location?> TryGetLastKnownLocationAsync()
    {
        try
        {
            return await Geolocation.Default.GetLastKnownLocationAsync();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsUsable(Location? location)
    {
        return location is not null &&
               !double.IsNaN(location.Latitude) &&
               !double.IsNaN(location.Longitude) &&
               Math.Abs(location.Latitude) <= 90d &&
               Math.Abs(location.Longitude) <= 180d &&
               !(Math.Abs(location.Latitude) < 0.000001d && Math.Abs(location.Longitude) < 0.000001d);
    }

    private static EmergencyLocationSnapshot Failed(string message)
    {
        return new EmergencyLocationSnapshot(0m, 0m, "", false, message);
    }

    public static async Task<string?> ReverseGeocodeAsync(Location location)
    {
        var (address, _) = await DescribeLocationAsync(location);
        return address;
    }

    private static async Task<(string? Address, string LocationName)> DescribeLocationAsync(Location location)
    {
        try
        {
            var placemarks = await Geocoding.Default.GetPlacemarksAsync(location.Latitude, location.Longitude);
            var place = placemarks?.FirstOrDefault();
            if (place is null)
            {
                return (null, "Current Location");
            }

            var street = string.Join(" ", new[] { place.SubThoroughfare, place.Thoroughfare }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var locality = string.Join(", ", new[] { place.Locality, place.AdminArea }.Where(x => !string.IsNullOrWhiteSpace(x)));
            var locationName = !string.IsNullOrWhiteSpace(locality)
                ? locality
                : place.SubLocality ?? place.FeatureName ?? "Current Location";
            var address = string.Join(", ", new[] { street, locality, place.CountryName }.Where(x => !string.IsNullOrWhiteSpace(x)));
            return (address, locationName);
        }
        catch
        {
            return (null, "Current Location");
        }
    }
}
