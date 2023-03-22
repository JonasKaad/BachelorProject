namespace FlightPatternDetection.DTO.NavDBEntities
{
    public abstract class EPointBase :
        IEquatable<EPointBase>
    {
        protected EPointBase(double latitude, double longitude, string identifier, int uid)
        {
            Identifier = identifier;
            Latitude = latitude;
            Longitude = longitude;
            UID = uid;
        }

        public double Latitude { get; }

        public double Longitude { get; }

        public string Identifier { get; set; }

        public int UID { get; }

        public string Name
        {
            get => m_Name ?? string.Empty;
            set => m_Name = value;
        }

        private string m_Name = "";

        public override string ToString() => Identifier;

        public bool Equals(EPointBase other) => Equals((object)other);

        public override bool Equals(object obj) => ReferenceEquals(this, obj);

        public override int GetHashCode() => UID;
    }
}
