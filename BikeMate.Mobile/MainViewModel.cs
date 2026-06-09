using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using BikeMate.Core.Constants;
using BikeMate.Core.DTOs;
using BikeMate.Helpers;
using BikeMate.Services;
using Microsoft.Maui.Authentication;
using Microsoft.Maui.Controls;

namespace BikeMate
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private int _currentPosition;
        private bool _isOnboardingVisible = true;
        private bool _isPaywallVisible = false;
        private bool _isLoginVisible = false;
        private string _email = "customer@bikemate.test";
        private string _password = "Password123!";
        private string _loginStatus = "Choose a demo mode or sign in with an account.";

        public ObservableCollection<OnboardingItem> BoardingItems { get; set; }

        public int CurrentPosition
        {
            get => _currentPosition;
            set
            {
                _currentPosition = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentButtonText));
            }
        }

        public string CurrentButtonText =>
            BoardingItems.Count == 0
                ? "Continue"
                : BoardingItems[Math.Clamp(CurrentPosition, 0, BoardingItems.Count - 1)].ButtonText;

        public bool IsOnboardingVisible
        {
            get => _isOnboardingVisible;
            set { _isOnboardingVisible = value; OnPropertyChanged(); }
        }

        public bool IsPaywallVisible
        {
            get => _isPaywallVisible;
            set { _isPaywallVisible = value; OnPropertyChanged(); }
        }

        public bool IsLoginVisible
        {
            get => _isLoginVisible;
            set { _isLoginVisible = value; OnPropertyChanged(); }
        }

        public string Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        public string LoginStatus
        {
            get => _loginStatus;
            set { _loginStatus = value; OnPropertyChanged(); }
        }

        // All Commands declared here
        public ICommand NextCommand { get; }
        public ICommand SkipOnboardingCommand { get; }
        public ICommand SelectDemoRoleCommand { get; }
        public ICommand ClosePaywallCommand { get; }
        public ICommand StartTrialCommand { get; }
        public ICommand SignInCommand { get; }
        public ICommand CreateAccountCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand GoogleLoginCommand { get; }
        public ICommand FacebookLoginCommand { get; }

        public MainViewModel()
        {
            BoardingItems = new ObservableCollection<OnboardingItem>
            {
                new OnboardingItem { Title = "BikeMate", Description = "Need a fix? Our expert bike techs come to you, whether you're at home, at work, or out riding. Get fast, reliable service on demand.", ImageSource = "bikemate_logo.png", ButtonText = "Let's Go!" },
                new OnboardingItem { Title = "Get your bike fixed in minutes", Description = "From flat tires to tune-ups, BikeMate lets you book a repair anytime, anywhere. Just tell us what's wrong—we'll handle the rest.", ImageSource = "bike_wrench.png", ButtonText = "Continue" },
                new OnboardingItem { Title = "No more shop visits", Description = "Our trusted mechanics come to you. Whether you're at home or on the road, we fix your bike on-site or bring it to our shop—no need to wait in line.", ImageSource = "mechanic_door.png", ButtonText = "Continue" },
                new OnboardingItem { Title = "Quick turnaround, no hassle", Description = "We promise speed and quality. Your bike is repaired and returned fast—like it never had a problem.", ImageSource = "running_man.png", ButtonText = "Start" }
            };

            // All Commands instantiated inside the constructor
            NextCommand = new Command(GoToNextSlide);
            SkipOnboardingCommand = new Command(ShowLogin);
            SelectDemoRoleCommand = new Command<string>(async role => await OpenDemoRoleAsync(role));
            ClosePaywallCommand = new Command(() => { IsPaywallVisible = false; IsLoginVisible = true; });
            StartTrialCommand = new Command(() => { IsPaywallVisible = false; IsLoginVisible = true; });

            SignInCommand = new Command(async () => await SignInAsync());
            CreateAccountCommand = new Command(async () => await OpenCreateAccountAsync());
            ForgotPasswordCommand = new Command(async () => await ForgotPasswordAsync());
            GoogleLoginCommand = new Command(async () => await LoginWithProviderAsync("Google"));
            FacebookLoginCommand = new Command(async () => await LoginWithProviderAsync("Facebook"));
        }

        private void GoToNextSlide()
        {
            if (CurrentPosition < BoardingItems.Count - 1)
            {
                CurrentPosition++;
            }
            else
            {
                ShowLogin();
            }
        }

        private void ShowLogin()
        {
            IsOnboardingVisible = false;
            IsPaywallVisible = false;
            IsLoginVisible = true;
        }

        private async Task OpenDemoRoleAsync(string? role)
        {
            var selectedRole = role switch
            {
                AppRoles.Mechanic => AppRoles.Mechanic,
                AppRoles.ShopAdmin => AppRoles.ShopAdmin,
                AppRoles.SystemAdmin => AppRoles.SystemAdmin,
                _ => AppRoles.Customer
            };

            Email = selectedRole switch
            {
                AppRoles.Mechanic => "mechanic@bikemate.test",
                AppRoles.ShopAdmin => "shop@bikemate.test",
                AppRoles.SystemAdmin => "admin@bikemate.test",
                _ => "customer@bikemate.test"
            };
            Password = "Password123!";

            var signedIn = await CustomerApiClient.TryLoginDemoAccountAsync(Email, selectedRole);
            if (!signedIn)
            {
                LoginStatus = "API is offline. Opening the selected classroom demo mode with sample data.";
                await SecureStorage.Default.SetAsync("primary_role", selectedRole);
            }

            await AppNavigation.NavigateByRoleAsync(selectedRole);
        }

        private async Task OpenCreateAccountAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(Views.Auth.RegisterPage));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlertAsync("Create account", $"Could not open registration: {ex.Message}", "OK");
            }
        }

        private async Task ForgotPasswordAsync()
        {
            var email = await Shell.Current.DisplayPromptAsync(
                "Forgot password",
                "Enter your email address.",
                "Send",
                "Cancel",
                "email@example.com",
                keyboard: Keyboard.Email,
                initialValue: Email);

            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            try
            {
                using var http = ApiConfig.CreateHttpClient();
                var response = await http.PostAsJsonAsync("auth/forgot-password", new ForgotPasswordRequestDto(email.Trim()));
                if (response.IsSuccessStatusCode)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Password reset",
                        "Request sent. In local development, the reset token is printed in the API terminal/log.",
                        "OK");
                    return;
                }

                await Shell.Current.DisplayAlertAsync("Password reset failed", await response.Content.ReadAsStringAsync(), "OK");
            }
            catch
            {
                await Shell.Current.DisplayAlertAsync(
                    "API offline",
                    "Start the BikeMate API first, then try forgot password again.",
                    "OK");
            }
        }

        private async Task LoginWithProviderAsync(string provider)
        {
            try
            {
                string authUrl = "";
                string redirectUri = "http://localhost/auth";

                if (provider == "Google")
                {
                    string clientId = "1049211486363-4c2qi058nipgjur9pjtvkursib3gdpb7.apps.googleusercontent.com";
                    authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?response_type=token&client_id={clientId}&redirect_uri={redirectUri}&scope=email%20profile";
                }
                else if (provider == "Facebook")
                {
                    string appId = "YOUR_FACEBOOK_APP_ID_HERE";
                    authUrl = $"https://www.facebook.com/v19.0/dialog/oauth?client_id={appId}&redirect_uri={redirectUri}&response_type=token&scope=email,public_profile";
                }

                WebAuthenticatorResult result = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(authUrl),
                    new Uri(redirectUri));

                string? accessToken = result?.AccessToken;

                if (!string.IsNullOrEmpty(accessToken))
                {
                    await HandleAutoSignUpAsync(accessToken, provider);
                }
            }
            catch (TaskCanceledException)
            {
                // Handled gracefully if the user closes the login window manually
            }
        }

        private async Task HandleAutoSignUpAsync(string token, string provider)
        {
            // Placeholder for your backend logic
            // Send token to your database to check/create user

            await Shell.Current.DisplayAlertAsync("Success", $"Successfully authenticated via {provider}!", "OK");
            await AppNavigation.NavigateByRoleAsync(AppRoles.Customer);
        }

        private async Task SignInAsync()
        {
            try
            {
                using var http = ApiConfig.CreateHttpClient();
                var response = await http.PostAsJsonAsync("auth/login", new LoginRequestDto(Email, Password));

                if (response.IsSuccessStatusCode)
                {
                    var auth = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (auth is not null)
                    {
                        var role = PickPrimaryRole(auth.User.Roles);
                        await SecureStorage.Default.SetAsync("access_token", auth.AccessToken);
                        await SecureStorage.Default.SetAsync("primary_role", role);
                        await AppNavigation.NavigateByRoleAsync(role);
                        return;
                    }
                }

                var error = await response.Content.ReadAsStringAsync();
                LoginStatus = string.IsNullOrWhiteSpace(error)
                    ? "Wrong email or password."
                    : error;
                await Shell.Current.DisplayAlertAsync("Sign in failed", LoginStatus, "OK");
                return;
            }
            catch
            {
                LoginStatus = "API is offline. Opening the selected classroom demo mode.";
            }

            await AppNavigation.NavigateByRoleAsync(AppNavigation.InferRoleFromEmail(Email));
        }

        private static string PickPrimaryRole(IReadOnlyCollection<string> roles)
        {
            if (roles.Contains(AppRoles.SystemAdmin)) return AppRoles.SystemAdmin;
            if (roles.Contains(AppRoles.ShopAdmin)) return AppRoles.ShopAdmin;
            if (roles.Contains(AppRoles.Mechanic)) return AppRoles.Mechanic;
            return AppRoles.Customer;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged([CallerMemberName] string propertyName = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }
    }
