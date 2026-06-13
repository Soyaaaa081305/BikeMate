using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.Controls.Shapes;

namespace BikeMate.Views.Auth;

[QueryProperty(nameof(Email), "email")]
[QueryProperty(nameof(FromRegister), "fromRegister")]
public partial class OtpVerificationPage : ContentPage
{
    private const string Orange = "#FF6B2C";
    private const string Dark = "#222222";
    private const string Muted = "#777777";

    private readonly Entry _otpEntry = new()
    {
        Keyboard = Keyboard.Numeric,
        MaxLength = 6,
        HorizontalTextAlignment = TextAlignment.Center,
        TextColor = Color.FromArgb(Dark),
        FontSize = 22,
        BackgroundColor = Colors.Transparent
    };
    private readonly ActivityIndicator _busy = new() { Color = Color.FromArgb(Orange), IsVisible = false, IsRunning = false };
    private string _email = string.Empty;
    private bool _fromRegister;
    private bool _isBusy;
    private DateTimeOffset _resendAvailableAt = DateTimeOffset.MinValue;
    private IDispatcherTimer? _resendTimer;

    public string Email
    {
        get => _email;
        set
        {
            _email = Uri.UnescapeDataString(value ?? string.Empty);
            Render();
        }
    }

    public string FromRegister
    {
        set => _fromRegister = bool.TryParse(value, out var parsed) && parsed;
    }

    public OtpVerificationPage()
    {
        InitializeComponent();
        Render();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = GoBackAsync();
        return true;
    }

    private void Render()
    {
        Detach(_otpEntry);
        Detach(_busy);
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(16, 18, 16, 24),
            Spacing = 20,
            BackgroundColor = Colors.White
        };

        body.Add(new Button
        {
            Text = "Back",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb(Dark),
            WidthRequest = 62,
            HeightRequest = 38,
            FontSize = 12,
            Padding = new Thickness(0),
            HorizontalOptions = LayoutOptions.Start,
            Command = new Command(async () => await GoBackAsync())
        });

        body.Add(new BoxView { HeightRequest = 26, Opacity = 0 });
        body.Add(new Label
        {
            Text = "Account Verification",
            TextColor = Color.FromArgb(Dark),
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        });

        body.Add(EnvelopeMark());
        body.Add(new Label
        {
            Text = $"We sent a verification code to {DisplayEmail(_email)}.",
            TextColor = Color.FromArgb(Muted),
            FontSize = 12,
            HorizontalTextAlignment = TextAlignment.Center
        });

        body.Add(OtpBox());
        var resendSeconds = ResendSecondsRemaining();
        body.Add(new Button
        {
            Text = resendSeconds > 0 ? $"Resend Code ({resendSeconds}s)" : "Resend Code",
            BackgroundColor = Colors.Transparent,
            TextColor = resendSeconds > 0 || _isBusy ? Color.FromArgb("#9AA7C7") : Color.FromArgb("#4F6FD8"),
            FontSize = 11,
            IsEnabled = !_isBusy && resendSeconds == 0,
            Command = new Command(async () => await ResendAsync())
        });
        body.Add(PrimaryButton(_isBusy ? "Please wait..." : "Continue", VerifyAsync, !_isBusy));
        body.Add(_busy);

        Content = new ScrollView { Content = body };
    }

    private View EnvelopeMark()
    {
        var grid = new Grid { HeightRequest = 130 };
        grid.Add(new Border
        {
            WidthRequest = 94,
            HeightRequest = 66,
            BackgroundColor = Color.FromArgb("#F4F4F4"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 32 },
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        });
        grid.Add(new Label
        {
            Text = "M",
            TextColor = Color.FromArgb("#E94335"),
            FontSize = 58,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center
        });
        return grid;
    }

    private View OtpBox()
    {
        Detach(_otpEntry);
        return new Border
        {
            Stroke = Color.FromArgb("#DDDDDD"),
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            BackgroundColor = Colors.White,
            Padding = new Thickness(14, 0),
            HeightRequest = 52,
            Content = _otpEntry
        };
    }

    private async Task VerifyAsync()
    {
        await RunOtpRequestAsync("auth/verify-otp", new VerifyOtpRequestDto(Email, _otpEntry.Text ?? string.Empty, "email_verification"), true);
    }

    private async Task ResendAsync()
    {
        if (ResendSecondsRemaining() > 0)
        {
            return;
        }

        await RunOtpRequestAsync("auth/resend-otp", new ResendOtpRequestDto(Email, "email_verification"), false);
    }

    private async Task RunOtpRequestAsync<T>(string route, T dto, bool verified)
    {
        if (_isBusy)
        {
            return;
        }

        SetBusy(true);
        Render();
        try
        {
            using var http = ApiConfig.CreateHttpClient();
            using var response = await http.PostAsJsonAsync(route, dto);
            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("OTP failed", await response.Content.ReadAsStringAsync(), "OK");
                return;
            }

            if (!verified)
            {
                StartResendCooldown();
                await DisplayAlertAsync("Code resent", $"Please check {DisplayEmail(_email)} for the new code.", "OK");
                return;
            }

            var proceed = await DisplayAlertAsync("Account Successfully Verified", "Your BikeMate customer account is ready.", "Proceed to Dashboard", "Stay Here");
            if (proceed)
            {
                await Shell.Current.GoToAsync("//CustomerHomePage");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("API offline", $"Start the BikeMate API, then try again. {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
            Render();
        }
    }

    private async Task GoBackAsync()
    {
        if (_fromRegister && Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }

    private void SetBusy(bool value)
    {
        _isBusy = value;
        _busy.IsVisible = value;
        _busy.IsRunning = value;
    }

    private void StartResendCooldown()
    {
        _resendAvailableAt = DateTimeOffset.UtcNow.AddSeconds(30);
        _resendTimer ??= Dispatcher.CreateTimer();
        _resendTimer.Interval = TimeSpan.FromSeconds(1);
        _resendTimer.Tick -= OnResendTimerTick;
        _resendTimer.Tick += OnResendTimerTick;
        _resendTimer.Start();
    }

    private void OnResendTimerTick(object? sender, EventArgs e)
    {
        if (ResendSecondsRemaining() == 0)
        {
            _resendTimer?.Stop();
        }

        Render();
    }

    private int ResendSecondsRemaining()
    {
        var remaining = _resendAvailableAt - DateTimeOffset.UtcNow;
        return remaining <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(remaining.TotalSeconds);
    }

    private static void Detach(View view)
    {
        switch (view.Parent)
        {
            case Border border when ReferenceEquals(border.Content, view):
                border.Content = null;
                break;
            case Layout layout:
                layout.Remove(view);
                break;
            case ContentView contentView when ReferenceEquals(contentView.Content, view):
                contentView.Content = null;
                break;
            case ScrollView scrollView when ReferenceEquals(scrollView.Content, view):
                scrollView.Content = null;
                break;
        }
    }

    private static Button PrimaryButton(string text, Func<Task> action, bool isEnabled = true)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(Orange),
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 46,
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            IsEnabled = isEnabled,
            Command = new Command(async () => await action())
        };
    }

    private static string DisplayEmail(string email)
    {
        return string.IsNullOrWhiteSpace(email)
            ? "your email address"
            : email.Trim();
    }
}
