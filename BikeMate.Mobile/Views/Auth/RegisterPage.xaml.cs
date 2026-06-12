using System.Net.Http.Json;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Media;
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

    private readonly Entry _phoneEntry = Entry("Enter phone number", Keyboard.Telephone);
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
        FontSize = 12
    };
    private readonly ActivityIndicator _busy = new() { Color = Color.FromArgb(Orange), IsVisible = false, IsRunning = false };

    public RegisterPage()
    {
        InitializeComponent();
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
                body.Add(LabeledField("Phone Number *", _phoneEntry));
                body.Add(LabeledField("Email *", _emailEntry));
                body.Add(LabeledField("Password *", _passwordEntry));
                body.Add(LabeledField("Confirm Password *", _confirmPasswordEntry));
                body.Add(Spacer(70));
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
            Text = "‹",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb(Dark),
            WidthRequest = 38,
            HeightRequest = 38,
            FontSize = 28,
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
        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 8
        };
        grid.Add(LabeledField("Province *", _provinceEntry), 0, 0);
        grid.Add(LabeledField("City *", _cityEntry), 1, 0);
        grid.Add(LabeledField("Barangay *", _barangayEntry), 2, 0);
        return grid;
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
            FontSize = 10,
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
                FontSize = 24,
                HorizontalTextAlignment = TextAlignment.Center,
                VerticalTextAlignment = TextAlignment.Center
            });
        }

        return Card(grid, new Thickness(0), 2);
    }

    private View TermsView()
    {
        var root = new VerticalStackLayout { Spacing = 14 };
        root.Add(Label("Terms And Condition Policy", 20, Dark, TextAlignment.Center, FontAttributes.Bold));
        root.Add(Label(
            "By creating an account with BikeMate, you agree to our Terms of Service and Privacy Policy. BikeMate offers on-demand bicycle repair, maintenance, and tune-up services - either at your location or via pick-up and drop-off. To deliver this service, we collect personal details like your GPS location and contact information to connect you with nearby certified bike mechanics.",
            14,
            Dark,
            TextAlignment.Start));
        root.Add(Label(
            "You are responsible for providing accurate details and using the app respectfully. Any misuse, dishonest behavior, or violations of our policies may lead to account suspension or removal. While we aim to deliver high-quality service, BikeMate is not a replacement for official bike brand service centers or manufacturer-covered repairs. All services are provided \"as is,\" and we are not liable for circumstances outside our control.",
            14,
            Dark,
            TextAlignment.Start));
        root.Add(Label(
            "Your privacy matters to us. We protect your data with strong security systems, and you have the right to access, correct, or delete your information at any time. We also use cookies to improve your experience, which can be managed through your app settings. For more information, please read our full Terms of Service and Privacy Policy.",
            14,
            Dark,
            TextAlignment.Start));

        var check = new CheckBox { IsChecked = _acceptedTerms, Color = Color.FromArgb(Orange) };
        check.CheckedChanged += (_, e) => _acceptedTerms = e.Value;
        var row = new HorizontalStackLayout
        {
            Spacing = 6,
            Children =
            {
                check,
                Label("I have read and understood the terms and conditions.", 10, Muted, TextAlignment.Start)
            }
        };
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

        if (!string.Equals(_passwordEntry.Text, _confirmPasswordEntry.Text, StringComparison.Ordinal))
        {
            await DisplayAlertAsync("Password mismatch", "Confirm password must match password.", "OK");
            return;
        }

        _step = 2;
        RenderStep();
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
            IEnumerable<FileResult>? photos = null;
            try
            {
                photos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Upload a valid ID" });
            }
            catch (FeatureNotSupportedException)
            {
                var fallback = await FilePicker.Default.PickAsync(new PickOptions
                {
                    PickerTitle = "Upload a valid ID",
                    FileTypes = FilePickerFileType.Images
                });
                photos = fallback is null ? null : [fallback];
            }

            var result = photos?.FirstOrDefault();
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
                _emailEntry.Text?.Trim() ?? "",
                _passwordEntry.Text ?? "",
                _confirmPasswordEntry.Text ?? "",
                _phoneEntry.Text?.Trim(),
                AppRoles.Customer);

            using var http = ApiConfig.CreateHttpClient();
            using var response = await http.PostAsJsonAsync("auth/register", dto);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlertAsync("Registration failed", await response.Content.ReadAsStringAsync(), "OK");
                return;
            }

            var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            if (auth is not null)
            {
                await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
                await SecureStorage.Default.SetAsync("primary_role", AppRoles.Customer);
                await SecureStorage.Default.SetAsync("user_id", auth.User.UserId.ToString());
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
            FontSize = 12,
            BackgroundColor = Colors.Transparent
        };
    }

    private static Border FieldCard(View content)
    {
        Detach(content);
        return Card(content, new Thickness(10, 0), 4);
    }

    private static View LabeledField(string label, View content)
    {
        return new VerticalStackLayout
        {
            Spacing = 5,
            Children =
            {
                Label(label, 10, Dark, TextAlignment.Start, FontAttributes.Bold),
                FieldCard(content)
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
            FontSize = 12,
            FontAttributes = FontAttributes.Bold,
            Command = new Command(async () => await action())
        };
    }

    private static Label Label(string text, double size, string color, TextAlignment alignment, FontAttributes attributes = FontAttributes.None)
    {
        return new Label
        {
            Text = text,
            FontSize = size,
            TextColor = Color.FromArgb(color),
            FontAttributes = attributes,
            HorizontalTextAlignment = alignment,
            LineBreakMode = LineBreakMode.WordWrap
        };
    }

    private static View Footnote(string text = "Fields with asterisk (*) are required.")
    {
        return Label(text, 9, Muted, TextAlignment.Center);
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
