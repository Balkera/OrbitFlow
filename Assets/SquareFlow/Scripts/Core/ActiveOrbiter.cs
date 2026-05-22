namespace SquareFlow.Core
{
    public sealed class ActiveOrbiter
    {
        public ActiveOrbiter(Shooter shooter)
        {
            Id = shooter.Id;
            Color = shooter.Color;
            Ammo = shooter.Ammo;
            Wild = shooter.Wild;
            Distance = 0f;
        }

        public string Id { get; }
        public FlowColor Color { get; }
        public int Ammo { get; set; }
        public bool Wild { get; }
        public float Distance { get; set; }
    }
}
