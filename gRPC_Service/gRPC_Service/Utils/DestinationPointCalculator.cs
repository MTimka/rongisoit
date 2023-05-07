namespace gRPC_Service.Utils;

public class DestinationPointCalculator
{
    public static LatLng CalculateDestinationPoint(LatLng point1, double bearing, double distance)
    {
        double radius = 6371000.0; // Earth's radius in meters
        double lat1 = DegreesToRadians(point1.Latitude);
        double lon1 = DegreesToRadians(point1.Longitude);
        double angularDistance = distance / radius;

        double bearingRad = DegreesToRadians(bearing);
        double lat2 = Math.Asin(Math.Sin(lat1) * Math.Cos(angularDistance) +
                                Math.Cos(lat1) * Math.Sin(angularDistance) * Math.Cos(bearingRad));
        double lon2 = lon1 + Math.Atan2(Math.Sin(bearingRad) * Math.Sin(angularDistance) * Math.Cos(lat1),
            Math.Cos(angularDistance) - Math.Sin(lat1) * Math.Sin(lat2));

        double lat2Degrees = RadiansToDegrees(lat2);
        double lon2Degrees = RadiansToDegrees(lon2);

        return new LatLng(lat2Degrees, lon2Degrees);
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }

    private static double RadiansToDegrees(double radians)
    {
        return radians * (180.0 / Math.PI);
    }
}