namespace BikeMate
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
            Routing.RegisterRoute(nameof(Views.Auth.LoginPage), typeof(Views.Auth.LoginPage));
            Routing.RegisterRoute(nameof(Views.Auth.RegisterPage), typeof(Views.Auth.RegisterPage));
            Routing.RegisterRoute(nameof(Views.Auth.OtpVerificationPage), typeof(Views.Auth.OtpVerificationPage));
        }
    }
}
