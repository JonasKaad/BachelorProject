using TrafficApiClient;

namespace PatternDetectionEngine
{
    public class Coord : ICoordinate
    {
        public Coord(double lat, double lon) 
        {
            Latitude = lat;
            Longitude = lon;
        }

        public Coord(TrafficPosition point)
        {
            Latitude = point.Lat;
            Longitude = point.Lon;
        }

        public double Latitude { get; }

        public double Longitude { get; }

        public double DistanceTo(ICoordinate other, double precision = 1e-8)
        {
            // http://www.movable-type.co.uk/scripts/latlong.html
            // http://earth-info.nga.mil/GandG/publications/tr8350.2/wgs84fin.pdf
            const double _a = 6378137.0;
            const double _f = 1 / 298.257223563;
            const double _b = _a * (1 - _f);

            double φ1 = InRadians(Latitude);
            double φ2 = InRadians(other.Latitude);
            double λ1 = (Longitude == -180) ? Math.PI : InRadians(Longitude);
            double λ2 = (other.Longitude == -180) ? Math.PI : InRadians(other.Longitude);

            double l = λ2 - λ1;

            double tanU1 = (1.0 - _f) * Math.Tan(φ1);
            double cosU1 = 1.0 / Math.Sqrt(1.0 + (tanU1 * tanU1));
            double sinU1 = tanU1 * cosU1;
            double tanU2 = (1.0 - _f) * Math.Tan(φ2);
            double cosU2 = 1.0 / Math.Sqrt(1.0 + (tanU2 * tanU2));
            double sinU2 = tanU2 * cosU2;

            double cosSqα = 0.0;
            double cos2σM = 0.0;
            double cos2SqσM = 0.0;
            double sinσ = 0.0;
            double cosσ = 0.0;
            double σ = 0.0;

            double λ = l;
            double λPrime;
            int iterationLimit = 100;

            do
            {
                double sinλ = Math.Sin(λ);
                double cosλ = Math.Cos(λ);
                double a1 = cosU2 * sinλ;
                double b1 = (cosU1 * sinU2) - (sinU1 * cosU2 * cosλ);
                double sinSqσ = (a1 * a1) + (b1 * b1);
                if (sinSqσ == 0)
                {
                    // co-incident points
                    break;
                }

                sinσ = Math.Sqrt(sinSqσ);
                cosσ = (sinU1 * sinU2) + (cosU1 * cosU2 * cosλ);
                σ = Math.Atan2(sinσ, cosσ);
                double sinα = cosU1 * cosU2 * sinλ / sinσ;
                cosSqα = 1.0 - (sinα * sinα);
                cos2σM = (cosSqα != 0) ? (cosσ - (2.0 * sinU1 * sinU2 / cosSqα)) : 0; // equatorial line: cosSqAlpha=0 (§6)
                cos2SqσM = cos2σM * cos2σM;
                double c = _f / 16 * cosSqα * (4 + (_f * (4 - (3 * cosSqα))));
                λPrime = λ;
                λ = l + ((1 - c) * _f * sinα * (σ + (c * sinσ * (cos2σM + (c * cosσ * (-1 + (2 * cos2SqσM)))))));
            }
            while (Math.Abs(λ - λPrime) > precision && (--iterationLimit) > 0);

            const double factor = ((_a * _a) - (_b * _b)) / (_b * _b);
            double uSq = cosSqα * factor;
            double a = 1 + (uSq / 16384 * (4096 + (uSq * (-768 + (uSq * (320 - (175 * uSq)))))));
            double b = uSq / 1024 * (256 + (uSq * (-128 + (uSq * (74 - (47 * uSq))))));
            double deltaSigma = b * sinσ * (cos2σM + (b / 4 * ((cosσ * (-1 + (2 * cos2SqσM)))
                                - (b / 6 * cos2σM * (-3 + (4 * sinσ * sinσ)) * (-3 + (4 * cos2SqσM))))));

            return _b * a * (σ - deltaSigma) / 1852;
        }

        private static double InRadians(double degrees) => degrees * (Math.PI / 180);
    }
}
