namespace gRPC_Service.Utils;

public class LineIntersectionChecker
{
    public static bool DoLinesIntersect(LatLng line1Start, LatLng line1End, LatLng line2Start, LatLng line2End)
    {
        double x1 = line1Start.Longitude;
        double y1 = line1Start.Latitude;
        double x2 = line1End.Longitude;
        double y2 = line1End.Latitude;
        double x3 = line2Start.Longitude;
        double y3 = line2Start.Latitude;
        double x4 = line2End.Longitude;
        double y4 = line2End.Latitude;

        // Calculate the orientation of the lines
        double orientation1 = CalculateOrientation(x1, y1, x2, y2);
        double orientation2 = CalculateOrientation(x3, y3, x4, y4);

        // Check if the lines are parallel
        if (orientation1 == orientation2)
            return false;

        // Calculate the intersection point
        double intersectionX, intersectionY;
        CalculateIntersection(x1, y1, x2, y2, x3, y3, x4, y4, out intersectionX, out intersectionY);

        // Check if the intersection point lies within both line segments
        return IsPointOnLineSegment(x1, y1, x2, y2, intersectionX, intersectionY)
            && IsPointOnLineSegment(x3, y3, x4, y4, intersectionX, intersectionY);
    }
    
    public static bool DoLinesIntersectRaw(
        double line1StartLat, double line1StartLon,
        double line1EndLat, double line1EndLon, 
        double line2StartLat, double line2StartLon, 
        double line2EndLat, double line2EndLon)
    {
        double x1 = line1StartLat;
        double y1 = line1StartLon;
        double x2 = line1EndLat;
        double y2 = line1EndLon;
        double x3 = line2StartLat;
        double y3 = line2StartLon;
        double x4 = line2EndLat;
        double y4 = line2EndLon;

        // Calculate the orientation of the lines
        double orientation1 = CalculateOrientation(x1, y1, x2, y2);
        double orientation2 = CalculateOrientation(x3, y3, x4, y4);

        // Check if the lines are parallel
        if (orientation1 == orientation2)
            return false;

        // Calculate the intersection point
        double intersectionX, intersectionY;
        CalculateIntersection(x1, y1, x2, y2, x3, y3, x4, y4, out intersectionX, out intersectionY);

        // Check if the intersection point lies within both line segments
        return IsPointOnLineSegment(x1, y1, x2, y2, intersectionX, intersectionY)
               && IsPointOnLineSegment(x3, y3, x4, y4, intersectionX, intersectionY);
    }

    // Helper function to calculate the orientation of a line
    private static double CalculateOrientation(double x1, double y1, double x2, double y2)
    {
        return (y2 - y1) / (x2 - x1);
    }

    // Helper function to calculate the intersection point of two lines
    private static void CalculateIntersection(double x1, double y1, double x2, double y2,
        double x3, double y3, double x4, double y4, out double intersectionX, out double intersectionY)
    {
        double denominator = ((x1 - x2) * (y3 - y4)) - ((y1 - y2) * (x3 - x4));

        intersectionX = (((x1 * y2) - (y1 * x2)) * (x3 - x4) - (x1 - x2) * ((x3 * y4) - (y3 * x4))) / denominator;
        intersectionY = (((x1 * y2) - (y1 * x2)) * (y3 - y4) - (y1 - y2) * ((x3 * y4) - (y3 * x4))) / denominator;
    }

    // Helper function to check if a point lies within a line segment
    private static bool IsPointOnLineSegment(double x1, double y1, double x2, double y2, double pointX, double pointY)
    {
        return (pointX >= Math.Min(x1, x2) && pointX <= Math.Max(x1, x2)
            && pointY >= Math.Min(y1, y2) && pointY <= Math.Max(y1, y2));
    }

    public static bool IsPointInPolygon(Utils.LatLng point, List<Utils.LatLng> polygon)
    {
        int intersectCount = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            var p1 = polygon[i];
            var p2 = polygon[(i + 1) % polygon.Count];

            if (p1.Longitude < point.Longitude && p2.Longitude < point.Longitude)
                continue;

            if (point.Longitude >= Math.Max(p1.Longitude, p2.Longitude))
                continue;

            double xIntersection = (point.Longitude - p1.Longitude) * (p2.Latitude - p1.Latitude)
                / (p2.Longitude - p1.Longitude) + p1.Latitude;

            if (xIntersection < point.Latitude)
                intersectCount++;
        }

        return intersectCount % 2 == 1;
    }
}