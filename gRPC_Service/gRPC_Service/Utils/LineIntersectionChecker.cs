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

}