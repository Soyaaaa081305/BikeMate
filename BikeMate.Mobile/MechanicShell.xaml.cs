namespace BikeMate;

public partial class MechanicShell : Shell
{
    public MechanicShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicJobDetailsPage), typeof(Views.Mechanic.MechanicJobDetailsPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicMapPage), typeof(Views.Mechanic.MechanicMapPage));
        Routing.RegisterRoute(nameof(Views.Mechanic.MechanicEmergencyRequestsPage), typeof(Views.Mechanic.MechanicEmergencyRequestsPage));
    }
}
