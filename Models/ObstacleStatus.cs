namespace NRLApp.Models
{
    public enum ObstacleStatus
    {
        Pending = 0,   // innsendt, ikke vurdert
        Approved = 1,  // godkjent av admin
        Rejected = 2   // avslått av admin
    }
}

