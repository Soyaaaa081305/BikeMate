using System.Net.Http.Json;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Storage;

namespace BikeMate.Views.Auth;

public partial class RegisterPage : ContentPage
{
    private const string Orange = "#FF6B2C";
    private const string Dark = "#222222";
    private const string Muted = "#777777";
    private const string BorderColor = "#FFB199";

    private int _step = 1;
    private bool _acceptedTerms;
    private FileResult? _validIdFile;
    private string? _validIdPreviewPath;

    private bool _formattingPhoneNumber;

    private readonly Entry _phoneEntry = Entry("9XX XXX XXXX", Keyboard.Telephone);
    private readonly Entry _emailEntry = Entry("Enter email", Keyboard.Email);
    private readonly Entry _passwordEntry = Entry("Enter password", null, true);
    private readonly Entry _confirmPasswordEntry = Entry("Confirm password", null, true);
    private readonly Entry _firstNameEntry = Entry("Enter first name");
    private readonly Entry _middleNameEntry = Entry("Enter middle name");
    private readonly Entry _lastNameEntry = Entry("Enter last name");
    private readonly Picker _sexPicker = new() { Title = "Select sex", TextColor = Color.FromArgb(Dark), TitleColor = Color.FromArgb(Muted) };
    private readonly DatePicker _birthdayPicker = new() { TextColor = Color.FromArgb(Dark), MaximumDate = DateTime.Today.AddYears(-16) };
    private readonly Entry _provinceEntry = Entry("Province");
    private readonly Entry _cityEntry = Entry("City");
    private readonly Entry _barangayEntry = Entry("Barangay");
    private readonly Editor _addressEditor = new()
    {
        Placeholder = "House number, street, landmark",
        PlaceholderColor = Color.FromArgb(Muted),
        TextColor = Color.FromArgb(Dark),
        HeightRequest = 72,
        BackgroundColor = Colors.Transparent,
        FontSize = 13
    };
    private readonly ActivityIndicator _busy = new() { Color = Color.FromArgb(Orange), IsVisible = false, IsRunning = false };

    public RegisterPage()
    {
        InitializeComponent();
        _phoneEntry.MaxLength = 14;
        _phoneEntry.TextChanged += PhoneEntry_TextChanged;
        _sexPicker.Items.Add("Female");
        _sexPicker.Items.Add("Male");
        _sexPicker.Items.Add("Prefer not to say");
        RenderStep();
    }

    protected override bool OnBackButtonPressed()
    {
        _ = GoBackAsync();
        return true;
    }

    private void PhoneEntry_TextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_formattingPhoneNumber)
        {
            return;
        }

        var digits = Regex.Replace(e.NewTextValue ?? string.Empty, @"\D", "");
        if (digits.StartsWith("63", StringComparison.Ordinal) && digits.Length > 10)
        {
            digits = digits[2..];
        }
        else if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length > 10)
        {
            digits = digits[1..];
        }

        if (digits.Length > 10)
        {
            digits = digits[..10];
        }

        if (!string.Equals(digits, e.NewTextValue, StringComparison.Ordinal))
        {
            _formattingPhoneNumber = true;
            _phoneEntry.Text = digits;
            _formattingPhoneNumber = false;
        }
    }

    private void RenderStep()
    {
        DetachReusableControls();
        var body = new VerticalStackLayout
        {
            Padding = new Thickness(14, 18, 14, 8),
            Spacing = 12,
            BackgroundColor = Colors.White
        };

        body.Add(Header(_step == 4 ? "" : "Create an Account"));
        if (_step <= 3)
        {
            body.Add(StepIndicator(_step));
        }

        switch (_step)
        {
            case 1:
                body.Add(PhoneNumberField());
                body.Add(LabeledField("Email *", _emailEntry));
                body.Add(LabeledField("Password *", _passwordEntry));
                body.Add(LabeledField("Confirm Password *", _confirmPasswordEntry));
                body.Add(Footnote("Use more than 8 characters."));
                body.Add(Spacer(32));
                body.Add(Footnote());
                body.Add(PrimaryButton("Continue", () => ContinueFromStep1Async()));
                break;
            case 2:
                body.Add(Label("Basic\nInformation", 11, Dark, TextAlignment.Center));
                body.Add(LabeledField("First Name *", _firstNameEntry));
                body.Add(LabeledField("Middle Name", _middleNameEntry));
                body.Add(LabeledField("Last Name *", _lastNameEntry));
                body.Add(LabeledField("Sex *", _sexPicker));
                body.Add(LabeledField("Birthdate *", _birthdayPicker));
                body.Add(Spacer(24));
                body.Add(Footnote("Fields with asterisk (*) are required."));
                body.Add(PrimaryButton("Continue", () => ContinueFromStep2Async()));
                break;
            case 3:
                body.Add(Label("Basic\nInformation", 11, Dark, TextAlignment.Center));
                body.Add(AddressGrid());
                body.Add(LabeledField("Address *", _addressEditor));
                body.Add(UploadRow());
                body.Add(IdPreview());
                body.Add(Spacer(18));
                body.Add(Footnote("Fields with asterisk (*) are required."));
                body.Add(PrimaryButton("Continue", () => ContinueFromStep3Async()));
                break;
            case 4:
                body.Add(TermsView());
                break;
        }

        body.Add(_busy);
        Content = new ScrollView { Content = body };
    }

    private View Header(string title)
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };

        grid.Add(new Button
        {
            Text = "<",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb(Dark),
            WidthRequest = 38,
            HeightRequest = 38,
            FontSize = 18,
            Padding = new Thickness(0),
            Command = new Command(async () => await GoBackAsync())
        }, 0, 0);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var copy = new VerticalStackLayout { Spacing = 0 };
            copy.Add(Label(title, 14, Dark, TextAlignment.Start, FontAttributes.Bold));
            copy.Add(Label("Let us know you more!", 10, Muted, TextAlignment.Start));
            grid.Add(copy, 1, 0);
        }

        return grid;
    }

    private static View StepIndicator(int step)
    {
        var stack = new VerticalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(0, 28, 0, 10),
            HorizontalOptions = LayoutOptions.Center
        };
        stack.Add(Label($"Step {step}", 10, Dark, TextAlignment.Center));
        stack.Add(new Border
        {
            WidthRequest = 6,
            HeightRequest = 6,
            Stroke = Colors.Transparent,
            StrokeShape = new RoundRectangle { CornerRadius = 3 },
            BackgroundColor = Colors.Black,
            HorizontalOptions = LayoutOptions.Center
        });
        stack.Add(new BoxView { HeightRequest = 1, WidthRequest = 88, BackgroundColor = Color.FromArgb("#E5E5E5") });
        return stack;
    }

    private View AddressGrid()
    {
        return new VerticalStackLayout
        {
            Spacing = 10,
            Children =
            {
                LabeledField("Province *", _provinceEntry),
                LabeledField("City *", _cityEntry),
                LabeledField("Barangay *", _barangayEntry)
            }
        };
    }

    private View UploadRow()
    {
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            },
            Padding = new Thickness(0, 4)
        };
        grid.Add(Label(_validIdFile is null ? "Upload a valid ID *" : _validIdFile.FileName, 11, Dark, TextAlignment.Start), 0, 0);
        grid.Add(new Button
        {
            Text = "Upload",
            BackgroundColor = Color.FromArgb(Orange),
            TextColor = Colors.White,
            FontSize = 11,
            HeightRequest = 30,
            CornerRadius = 14,
            Padding = new Thickness(16, 0),
            Command = new Command(async () => await UploadValidIdAsync())
        }, 1, 0);
        return grid;
    }

    private View IdPreview()
    {
        var grid = new Grid
        {
            HeightRequest = 126,
            BackgroundColor = Color.FromArgb("#FAFAFA")
        };

        if (!string.IsNullOrWhiteSpace(_validIdPreviewPath) && File.Exists(_validIdPreviewPath))
        {
            grid.Add(new Image
            {
                Source = ImageSource.FromFile(_validIdPreviewPath),
                Aspect = Aspect.AspectFit
            });
        }
        else
        {
            grid.Add(new Label
            {
                Text = "{    +    }",
                TextColor = Color.FromArgb("#D7D7D7"),
                FontSize = 18,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        return Card(grid, new Thickness(0), 2);
    }

    private View TermsView()
    {
        var root = new VerticalStackLayout { Spacing = 14 };
        root.Add(Label("Terms and Conditions", 18, Dark, TextAlignment.Center, FontAttributes.Bold));
        root.Add(Label("Please review these terms before creating your BikeMate account.", 13, Muted, TextAlignment.Center));
        root.Add(Label(
            "Service use. BikeMate connects customers with repair shops, mechanics, emergency roadside support, booking tools, live location, chat, and secure payment. You agree to provide accurate booking, contact, bike, address, and location information so the assigned shop or mechanic can serve you safely.",
            13,
            Dark,
            TextAlignment.Start));
        root.Add(Label(
            "Account responsibility. Keep your login details private, use your real email and Philippine mobile number, and do not create duplicate, misleading, abusive, or fraudulent accounts. BikeMate may suspend or remove accounts that misuse bookings, payments, emergency tools, reviews, or chat.",
            13,
            Dark,
            TextAlignment.Start));
        root.Add(Label(
            "Payments and repairs. Prices, availability, pickup, arrival time, parts, and repair outcomes may vary by shop, service, location, and inspection. PayMongo processes secure payments; BikeMate does not store card or wallet credentials. Refunds, cancellations, and disputes may require review of booking records and payment status.",
            13,
            Dark,
            TextAlignment.Start));
        root.Add(Label(
            "Privacy and safety. BikeMate collects and uses account details, uploaded IDs, contact details, GPS/location data, booking media, chat records, and payment references only to operate, protect, and improve the service. You may request correction or deletion where allowed by law. Emergency and location features are support tools and do not replace public emergency services.",
            13,
            Dark,
            TextAlignment.Start));

        var check = new CheckBox { IsChecked = _acceptedTerms, Color = Color.FromArgb(Orange) };
        check.CheckedChanged += (_, e) => _acceptedTerms = e.Value;
        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        row.Add(check, 0, 0);
        row.Add(Label("I have read, understood, and agree to BikeMate's Terms and Conditions and Privacy Policy.", 11, Muted, TextAlignment.Start), 1, 0);
        root.Add(row);
        root.Add(PrimaryButton("Continue", RegisterAsync));
        return root;
    }

    private async Task ContinueFromStep1Async()
    {
        if (string.IsNullOrWhiteSpace(_phoneEntry.Text) ||
            string.IsNullOrWhiteSpace(_emailEntry.Text) ||
            string.IsNullOrWhiteSpace(_passwordEntry.Text) ||
            string.IsNullOrWhiteSpace(_confirmPasswordEntry.Text))
        {
            await DisplayAlertAsync("Missing details", "Please complete all required fields.", "OK");
            return;
        }

        string normalizedEmail;
        string normalizedPhone;
        try
        {
            normalizedEmail = NormalizeEmail(_emailEntry.Text);
            normalizedPhone = NormalizePhilippineMobile(_phoneEntry.Text);
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync("Check your details", ex.Message, "OK");
            return;
        }

        if ((_passwordEntry.Text ?? string.Empty).Length <= 8)
        {
            await DisplayAlertAsync("Weak password", "Password must be more than 8 characters.", "OK");
            return;
        }

        if (!string.Equals(_passwordEntry.Text, _confirmPasswordEntry.Text, StringComparison.Ordinal))
        {
            await DisplayAlertAsync("Password mismatch", "Confirm password must match password.", "OK");
            return;
        }

        if (!await CheckAvailabilityAsync(normalizedEmail, normalizedPhone))
        {
            return;
        }

        _emailEntry.Text = normalizedEmail;
        _phoneEntry.Text = normalizedPhone;
        _step = 2;
        RenderStep();
    }

    private async Task<bool> CheckAvailabilityAsync(string email, string phone)
    {
        try
        {
            SetBusy(true);
            using var http = ApiConfig.CreateHttpClient();
            var route = $"auth/availability?email={Uri.EscapeDataString(email)}&phone={Uri.EscapeDataString(phone)}";
            using var response = await http.GetAsync(route);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return true;
            }

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Validation failed", await ReadErrorAsync(response), "OK");
                return false;
            }

            var availability = await response.Content.ReadFromJsonAsync<AuthAvailabilityDto>();
            if (availability is null)
            {
                await DisplayAlertAsync("Validation failed", "BikeMate could not check account availability. Please try again.", "OK");
                return false;
            }

            if (!availability.EmailAvailable)
            {
                await DisplayAlertAsync("Email already used", "This email address is already registered. Use a different email or sign in instead.", "OK");
                return false;
            }

            if (!availability.PhoneAvailable)
            {
                await DisplayAlertAsync("Phone already used", "This Philippine mobile number is already registered. Use a different number or sign in instead.", "OK");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("API offline", $"BikeMate could not check duplicate email or phone yet. Start the API, then try again. {ex.Message}", "OK");
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task ContinueFromStep2Async()
    {
        if (string.IsNullOrWhiteSpace(_firstNameEntry.Text) ||
            string.IsNullOrWhiteSpace(_lastNameEntry.Text) ||
            _sexPicker.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Missing details", "Please complete your basic information.", "OK");
            return;
        }

        _step = 3;
        RenderStep();
    }

    private async Task ContinueFromStep3Async()
    {
        if (string.IsNullOrWhiteSpace(_provinceEntry.Text) ||
            string.IsNullOrWhiteSpace(_cityEntry.Text) ||
            string.IsNullOrWhiteSpace(_barangayEntry.Text) ||
            string.IsNullOrWhiteSpace(_addressEditor.Text) ||
            _validIdFile is null)
        {
            await DisplayAlertAsync("Missing details", "Please complete your address and upload a valid ID.", "OK");
            return;
        }

        _step = 4;
        RenderStep();
    }

    private async Task UploadValidIdAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Upload a valid ID",
                FileTypes = FilePickerFileType.Images
            });

            if (result is null)
            {
                return;
            }

            _validIdFile = result;
            _validIdPreviewPath = await CopyToCacheAsync(result);
            await DisplayAlertAsync("Approved ID", "The ID you provided has been approved by our system.", "Dismiss");
            RenderStep();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Upload failed", ex.Message, "OK");
        }
    }

    private async Task RegisterAsync()
    {
        if (!_acceptedTerms)
        {
            await DisplayAlertAsync("Terms required", "Please read and accept the terms and conditions.", "OK");
            return;
        }

        SetBusy(true);
        try
        {
            var dto = new RegisterRequestDto(
                _firstNameEntry.Text?.Trim() ?? "",
                _lastNameEntry.Text?.Trim() ?? "",
                NormalizeEmail(_emailEntry.Text),
                _passwordEntry.Text ?? "",
                _confirmPasswordEntry.Text ?? "",
                NormalizePhilippineMobile(_phoneEntry.Text),
                AppRoles.Customer);

            using var http = ApiConfig.CreateHttpClient();
            using var response = await http.PostAsJsonAsync("auth/register", dto);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Registration failed", await ReadErrorAsync(response), "OK");
                return;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (auth is not null)
            {
                await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
                await SecureStorage.Default.SetAsync("primary_role", AppRoles.Customer);
                await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
                await PersistCustomerSignupDetailsAsync(dto);
            }

            await Shell.Current.GoToAsync($"{nameof(OtpVerificationPage)}?email={Uri.EscapeDataString(dto.Email)}&fromRegister=true");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("API offline", $"Start the BikeMate API, then try registration again. {ex.Message}", "OK");
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task PersistCustomerSignupDetailsAsync(RegisterRequestDto dto)
    {
        string? validIdUrl = null;
        if (_validIdFile is not null)
        {
            var upload = await CustomerApiClient.UploadFileAsync(_validIdFile, "customer-id");
            validIdUrl = upload.Url;
            await CustomerApiClient.UpdateCustomerValidIdAsync(validIdUrl);
        }

        await CustomerApiClient.UpdateCustomerAsync(new UpsertCustomerProfileDto(
            dto.FirstName,
            dto.LastName,
            dto.Email,
            dto.PhoneNumber,
            Clean(_middleNameEntry.Text),
            _sexPicker.SelectedItem?.ToString(),
            _birthdayPicker.Date.Date));

        await CustomerApiClient.UpsertAddressAsync(null, new UpsertCustomerAddressDto(
            "Home",
            Clean(_addressEditor.Text) ?? string.Empty,
            Clean(_barangayEntry.Text),
            Clean(_cityEntry.Text),
            Clean(_provinceEntry.Text),
            null,
            null,
            null,
            true));
    }

    private async Task GoBackAsync()
    {
        if (_step > 1)
        {
            _step--;
            RenderStep();
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private void SetBusy(bool value)
    {
        _busy.IsVisible = value;
        _busy.IsRunning = value;
    }

    private static Entry Entry(string placeholder, Keyboard? keyboard = null, bool isPassword = false)
    {
        return new Entry
        {
            Placeholder = placeholder,
            Keyboard = keyboard ?? Keyboard.Default,
            IsPassword = isPassword,
            TextColor = Color.FromArgb(Dark),
            PlaceholderColor = Color.FromArgb("#FF8F68"),
            FontSize = 13,
            BackgroundColor = Colors.Transparent
        };
    }

    private static Border FieldCard(View content)
    {
        Detach(content);
        var card = Card(content, new Thickness(10, 0), 4);
        card.HorizontalOptions = LayoutOptions.Fill;
        return card;
    }

    private static View LabeledField(string label, View content)
    {
        return new VerticalStackLayout
        {
            Spacing = 5,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                Label(label, 10, Dark, TextAlignment.Start, FontAttributes.Bold),
                FieldCard(content)
            }
        };
    }

    private View PhoneNumberField()
    {
        Detach(_phoneEntry);

        var prefix = Label("+63", 13, Dark, TextAlignment.Start, FontAttributes.Bold);
        prefix.VerticalOptions = LayoutOptions.Center;

        var divider = new BoxView
        {
            WidthRequest = 1,
            HeightRequest = 24,
            Color = Color.FromArgb(BorderColor),
            VerticalOptions = LayoutOptions.Center
        };

        var row = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 10,
            HorizontalOptions = LayoutOptions.Fill
        };
        row.Add(prefix, 0, 0);
        row.Add(divider, 1, 0);
        row.Add(_phoneEntry, 2, 0);

        return new VerticalStackLayout
        {
            Spacing = 5,
            HorizontalOptions = LayoutOptions.Fill,
            Children =
            {
                Label("Phone Number *", 10, Dark, TextAlignment.Start, FontAttributes.Bold),
                Card(row, new Thickness(10, 0), 4)
            }
        };
    }

    private static Border Card(View content, Thickness padding, double radius)
    {
        return new Border
        {
            Stroke = Color.FromArgb(BorderColor),
            StrokeThickness = 1,
            StrokeShape = new RoundRectangle { CornerRadius = radius },
            BackgroundColor = Colors.White,
            Padding = padding,
            HorizontalOptions = LayoutOptions.Fill,
            Content = content
        };
    }

    private static Button PrimaryButton(string text, Func<Task> action)
    {
        return new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb(Orange),
            TextColor = Colors.White,
            CornerRadius = 10,
            HeightRequest = 46,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            HorizontalOptions = LayoutOptions.Fill,
            Command = new Command(async () => await action())
        };
    }

    private static Label Label(string text, double size, string color, TextAlignment alignment, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = text,
            FontSize = AppTypography.SizeFor(size),
            TextColor = Color.FromArgb(color),
            FontAttributes = attributes,
            FontFamily = FontFor(size, attributes),
            HorizontalTextAlignment = alignment,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static string FontFor(double size, FontAttributes attributes = FontAttributes.None)
    {
        return AppTypography.FontFor(size, attributes);
    }

    private static View Footnote(string text = "Fields with asterisk (*) are required.")
    {
        return Label(text, 9, Muted, TextAlignment.Center);
    }

    private static string NormalizeEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("Email is required.");
        }

        try
        {
            var address = new MailAddress(normalized);
            if (!string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase))
            {
                throw new FormatException();
            }
        }
        catch
        {
            throw new InvalidOperationException("Enter a valid email address.");
        }

        return normalized;
    }

    private static string NormalizePhilippineMobile(string? phoneNumber)
    {
        var digits = Regex.Replace(phoneNumber?.Trim() ?? string.Empty, @"\D", "");
        var localNumber = digits;

        if (digits.StartsWith("63", StringComparison.Ordinal) && digits.Length == 12)
        {
            localNumber = digits[2..];
        }
        else if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length == 11)
        {
            localNumber = digits[1..];
        }

        if (!Regex.IsMatch(localNumber, @"^9\d{9}$"))
        {
            throw new InvalidOperationException("Enter the 10 digits after +63, for example 9171234567.");
        }

        return $"+63{localNumber}";
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(content))
        {
            return $"Request failed with HTTP {(int)response.StatusCode}.";
        }

        try
        {
            using var document = JsonDocument.Parse(content);
            if (document.RootElement.TryGetProperty("error", out var error) &&
                error.ValueKind == JsonValueKind.String)
            {
                return error.GetString() ?? content;
            }
        }
        catch
        {
            // Fall back to raw content below.
        }

        return content;
    }

    private static View Spacer(double height)
    {
        return new BoxView { HeightRequest = height, Opacity = 0 };
    }

    private static async Task<string> CopyToCacheAsync(FileResult result)
    {
        var extension = System.IO.Path.GetExtension(result.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var targetPath = System.IO.Path.Combine(FileSystem.CacheDirectory, $"valid-id-{DateTime.UtcNow:yyyyMMddHHmmss}{extension}");
        await using var input = await result.OpenReadAsync();
        await using var output = File.Create(targetPath);
        await input.CopyToAsync(output);
        return targetPath;
    }

    private void DetachReusableControls()
    {
        foreach (var view in new View[]
        {
            _phoneEntry,
            _emailEntry,
            _passwordEntry,
            _confirmPasswordEntry,
            _firstNameEntry,
            _middleNameEntry,
            _lastNameEntry,
            _sexPicker,
            _birthdayPicker,
            _provinceEntry,
            _cityEntry,
            _barangayEntry,
            _addressEditor,
            _busy
        })
        {
            Detach(view);
        }
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
}
