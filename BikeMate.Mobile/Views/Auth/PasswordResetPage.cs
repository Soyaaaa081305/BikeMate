using System.Net.Http.Json;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Views.Customer;
using Microsoft.Maui.Controls.Shapes;

namespace BikeMate.Views.Auth;

public sealed class PasswordResetPage : CustomerPageBase, IQueryAttributable
{
    private string _email = "";
    private string _code = "";
    private string _newPassword = "";
    private string _confirmPassword = "";
    private string? _banner;
    private bool _codeSent;
    private bool _isBusy;

    public PasswordResetPage()
    {
        Title = "Reset password";
        Render();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("email", out var value))
        {
            _email = Uri.UnescapeDataString(value?.ToString() ?? "");
            Render();
        }
    }

    private void Render()
    {
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(18, 8, 18, 24),
            Spacing = 14,
            BackgroundColor = CustomerUi.Page
        };

        body.Add(Header("Reset Password"));
        body.Add(Card(new VerticalStackLayout
        {
            Spacing = 8,
            Children =
            {
                Label("Verify your email", 20, CustomerUi.Dark, FontAttributes.Bold),
                Label("BikeMate will send a six-digit code before letting you set a new password.", 12, CustomerUi.Muted),
                BuildStepRow()
            }
        }, Colors.White, 8, new Thickness(14)));

        if (!string.IsNullOrWhiteSpace(_banner))
        {
            body.Add(Card(Label(_banner, 12, CustomerUi.Muted), Colors.White, 8, new Thickness(12)));
        }

        body.Add(_codeSent ? BuildPasswordStep() : BuildEmailStep());
        SetScaffold(new ScrollView { Content = body }, "Home", false);
    }

    private View BuildStepRow()
    {
        var grid = new Grid { ColumnSpacing = 8 };
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        grid.Add(StepPill("1 Email", true), 0, 0);
        grid.Add(StepPill("2 Code", _codeSent), 1, 0);
        grid.Add(StepPill("3 Password", _codeSent), 2, 0);
        return grid;
    }

    private static View StepPill(string text, bool active)
    {
        return new Border
        {
            BackgroundColor = active ? CustomerUi.LightOrange : Color.FromArgb("#F2F2F2"),
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 8 },
            Padding = new Thickness(8, 7),
            Content = new Label
            {
                Text = text,
                TextColor = active ? CustomerUi.Orange : CustomerUi.Muted,
                FontSize = 10,
                FontAttributes = FontAttributes.Bold,
                FontFamily = CustomerUi.FontCaptionBold,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.NoWrap
            }
        };
    }

    private View BuildEmailStep()
    {
        var email = new Entry
        {
            Placeholder = "Email address",
            Text = _email,
            Keyboard = Keyboard.Email,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent
        };
        email.TextChanged += (_, e) => _email = e.NewTextValue ?? "";

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label("Where should we send the code?", 14, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(Card(email, Colors.White, 8, new Thickness(12, 2)));
        stack.Add(OrangeButton(_isBusy ? "Sending code..." : "Send reset code", new Command(async () => await SendCodeAsync())));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private View BuildPasswordStep()
    {
        var code = new Entry
        {
            Placeholder = "6-digit code",
            Text = _code,
            Keyboard = Keyboard.Numeric,
            MaxLength = 6,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent
        };
        code.TextChanged += (_, e) => _code = e.NewTextValue ?? "";

        var password = new Entry
        {
            Placeholder = "New password",
            Text = _newPassword,
            IsPassword = true,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent
        };
        password.TextChanged += (_, e) => _newPassword = e.NewTextValue ?? "";

        var confirm = new Entry
        {
            Placeholder = "Confirm new password",
            Text = _confirmPassword,
            IsPassword = true,
            TextColor = CustomerUi.Dark,
            PlaceholderColor = CustomerUi.Muted,
            BackgroundColor = Colors.Transparent
        };
        confirm.TextChanged += (_, e) => _confirmPassword = e.NewTextValue ?? "";

        var stack = new VerticalStackLayout { Spacing = 12 };
        stack.Add(Label($"Code sent to {_email}", 14, CustomerUi.Dark, FontAttributes.Bold));
        stack.Add(Card(code, Colors.White, 8, new Thickness(12, 2)));
        stack.Add(Card(password, Colors.White, 8, new Thickness(12, 2)));
        stack.Add(Card(confirm, Colors.White, 8, new Thickness(12, 2)));
        stack.Add(OrangeButton(_isBusy ? "Updating password..." : "Update password", new Command(async () => await ResetPasswordAsync())));
        stack.Add(GhostButton("Send a new code", new Command(async () => await SendCodeAsync())));
        return Card(stack, Colors.White, 8, new Thickness(14));
    }

    private async Task SendCodeAsync()
    {
        if (_isBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_email) || !_email.Contains('@', StringComparison.Ordinal))
        {
            _banner = "Enter the email address on your BikeMate account.";
            Render();
            return;
        }

        _isBusy = true;
        _banner = null;
        Render();
        try
        {
            using var http = ApiConfig.CreateHttpClient();
            using var response = await http.PostAsJsonAsync("auth/forgot-password", new ForgotPasswordRequestDto(_email.Trim()));
            if (!response.IsSuccessStatusCode)
            {
                _banner = await response.Content.ReadAsStringAsync();
                return;
            }

            _email = _email.Trim();
            _codeSent = true;
            _banner = "If that email exists, a six-digit reset code was sent. It expires in 15 minutes.";
        }
        catch
        {
            _banner = "BikeMate could not reach the API. Start the API, then send the code again.";
        }
        finally
        {
            _isBusy = false;
            Render();
        }
    }

    private async Task ResetPasswordAsync()
    {
        if (_isBusy)
        {
            return;
        }

        if (_code.Trim().Length != 6)
        {
            _banner = "Enter the six-digit code from your email.";
            Render();
            return;
        }

        if (_newPassword.Length < 8)
        {
            _banner = "Use at least 8 characters for the new password.";
            Render();
            return;
        }

        if (!string.Equals(_newPassword, _confirmPassword, StringComparison.Ordinal))
        {
            _banner = "The new passwords do not match.";
            Render();
            return;
        }

        _isBusy = true;
        _banner = null;
        Render();
        try
        {
            using var http = ApiConfig.CreateHttpClient();
            using var response = await http.PostAsJsonAsync("auth/reset-password", new ResetPasswordRequestDto(
                _email.Trim(),
                _code.Trim(),
                _newPassword,
                _confirmPassword));

            if (!response.IsSuccessStatusCode)
            {
                _banner = await response.Content.ReadAsStringAsync();
                return;
            }

            await DisplayAlertAsync("Password updated", "You can sign in with your new password now.", "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch
        {
            _banner = "BikeMate could not reach the API. Check your connection and try again.";
        }
        finally
        {
            _isBusy = false;
            Render();
        }
    }
}
