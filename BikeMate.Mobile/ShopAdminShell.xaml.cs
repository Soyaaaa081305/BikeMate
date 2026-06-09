namespace BikeMate;

public partial class ShopAdminShell : Shell
{
    public ShopAdminShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.ShopAdmin.ShopServiceEditPage), typeof(Views.ShopAdmin.ShopServiceEditPage));
        Routing.RegisterRoute(nameof(Views.ShopAdmin.ShopProductEditPage), typeof(Views.ShopAdmin.ShopProductEditPage));
        Routing.RegisterRoute(nameof(Views.ShopAdmin.ShopMechanicsPage), typeof(Views.ShopAdmin.ShopMechanicsPage));
        Routing.RegisterRoute(nameof(Views.ShopAdmin.ShopEarningsPage), typeof(Views.ShopAdmin.ShopEarningsPage));
        Routing.RegisterRoute(nameof(Views.ShopAdmin.ShopSchedulePage), typeof(Views.ShopAdmin.ShopSchedulePage));
    }
}
