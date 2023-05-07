namespace gRPC_Service.Utils;

public class TriangleHelper
{
    public static (LatLng, LatLng) FindTriangleSidePoints(LatLng p1, LatLng p2)
    {
        // Calculate the distance between p1 and the side points (5 meters)
        double sideDistance = 0.00008;

        // Calculate the bearing angle between p1 and p2
        double bearingAngle = Math.Atan2(p2.Latitude - p1.Latitude, p2.Longitude - p1.Longitude);

        // Calculate the angle between p1-p2 and the side points (90 degrees)
        double angle = Math.PI / 2.0;

        // Calculate the relative coordinates for the side points
        double deltaX = sideDistance * Math.Cos(bearingAngle);
        double deltaY = sideDistance * Math.Sin(bearingAngle);

        // Calculate the coordinates for the side points
        LatLng p3 = new LatLng(p1.Latitude + deltaX, p1.Longitude - deltaY);
        LatLng p4 = new LatLng(p1.Latitude - deltaX, p1.Longitude + deltaY);

        return (p3, p4);
    }
}