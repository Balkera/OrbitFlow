namespace SquareFlow.Core
{
    public readonly struct Shooter
    {
        public Shooter(string id, FlowColor color, int ammo, bool wild, bool hidden = false)
        {
            Id = id;
            Color = color;
            Ammo = ammo;
            Wild = wild;
            Hidden = hidden;
        }

        public string Id { get; }
        public FlowColor Color { get; }
        public int Ammo { get; }
        public bool Wild { get; }
        public bool Hidden { get; }

        public Shooter Revealed()
        {
            return new Shooter(Id, Color, Ammo, Wild, false);
        }
    }
}
