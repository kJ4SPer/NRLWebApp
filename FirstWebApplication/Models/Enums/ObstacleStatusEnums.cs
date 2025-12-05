namespace FirstWebApplication.Models.Enums
{
    public enum ObstacleStatusEnum
    {
        Registered = 1, // Ufullstendig / Kladd
        Pending = 2,    // Venter på godkjenning
        Approved = 3,   // Godkjent
        Rejected = 4    // Avvist
    }
}