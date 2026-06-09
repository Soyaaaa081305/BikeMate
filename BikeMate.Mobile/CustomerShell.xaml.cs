namespace BikeMate;

public partial class CustomerShell : Shell
{
    public CustomerShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.Customer.CustomerProfilePage), typeof(Views.Customer.CustomerProfilePage));
        Routing.RegisterRoute(nameof(Views.Customer.CustomerHelpDeskPage), typeof(Views.Customer.CustomerHelpDeskPage));
        Routing.RegisterRoute(nameof(Views.Customer.CustomerChatPage), typeof(Views.Customer.CustomerChatPage));
        Routing.RegisterRoute(nameof(Views.Customer.BookServicePage), typeof(Views.Customer.BookServicePage));
        Routing.RegisterRoute(nameof(Views.Customer.BookingDetailsPage), typeof(Views.Customer.BookingDetailsPage));
        Routing.RegisterRoute(nameof(Views.Customer.TrackMechanicPage), typeof(Views.Customer.TrackMechanicPage));
        Routing.RegisterRoute(nameof(Views.Customer.PaymentCheckoutPage), typeof(Views.Customer.PaymentCheckoutPage));
        Routing.RegisterRoute(nameof(Views.Customer.PaymentReceiptPage), typeof(Views.Customer.PaymentReceiptPage));
        Routing.RegisterRoute(nameof(Views.Customer.PaymentInvoicePage), typeof(Views.Customer.PaymentInvoicePage));
        Routing.RegisterRoute(nameof(Views.Customer.PaymentOptionsPage), typeof(Views.Customer.PaymentOptionsPage));
    }
}
