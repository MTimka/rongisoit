namespace gRPC_Service.Utils;

public class LineIntersectionChecker
{
    public static bool DoLinesIntersect(LatLng line1Start, LatLng line1End, LatLng line2Start, LatLng line2End)
    {
        // Convert latitude and longitude values to radians
        double line1StartLatRad = ToRadians(line1Start.Latitude);
        double line1StartLonRad = ToRadians(line1Start.Longitude);
        double line1EndLatRad = ToRadians(line1End.Latitude);
        double line1EndLonRad = ToRadians(line1End.Longitude);

        double line2StartLatRad = ToRadians(line2Start.Latitude);
        double line2StartLonRad = ToRadians(line2Start.Longitude);
        double line2EndLatRad = ToRadians(line2End.Latitude);
        double line2EndLonRad = ToRadians(line2End.Longitude);

        // Calculate differences between points
        double deltaLat1 = line1EndLatRad - line1StartLatRad;
        double deltaLon1 = line1EndLonRad - line1StartLonRad;
        double deltaLat2 = line2EndLatRad - line2StartLatRad;
        double deltaLon2 = line2EndLonRad - line2StartLonRad;

        // Calculate cross product
        double crossProduct = (deltaLon1 * deltaLat2) - (deltaLon2 * deltaLat1);

        if (Math.Abs(crossProduct) < 1e-10) // Lines are parallel
            return false;

        // Calculate line parameters
        double s = ((line1StartLonRad - line2StartLonRad) * deltaLat2 - (line1StartLatRad - line2StartLatRad) * deltaLon2) / crossProduct;
        double t = ((line1StartLonRad - line2StartLonRad) * deltaLat1 - (line1StartLatRad - line2StartLatRad) * deltaLon1) / crossProduct;

        // Check if intersection point lies within the line segments
        if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
            return true;

        return false;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180.0);
    }
}