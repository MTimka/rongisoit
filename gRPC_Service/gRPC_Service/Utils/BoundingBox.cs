namespace gRPC_Service.Utils;

public class BoundingBox {
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public bool Intersects(double startLat, double startLon, double latitude, double longitude)
    {
        double minX = X;
        double minY = Y;
        double maxX = X + Width;
        double maxY = Y + Height;
    
        return (latitude >= minX && latitude <= maxX)
               && (longitude >= minY && longitude <= maxY);
        //
        // var doesIntersectTop = LineIntersectionChecker.DoLinesIntersectRaw(
        //     startLat, startLon,
        //     latitude, longitude,
        //     X, Y,
        //     X + Width, Y
        // );
        //
        // var doesIntersectRight = LineIntersectionChecker.DoLinesIntersectRaw(
        //     startLat, startLon,
        //     latitude, longitude,
        //     X + Width, Y,
        //     X + Width, Y + Height
        // );
        //
        // var doesIntersectBottom = LineIntersectionChecker.DoLinesIntersectRaw(
        //     startLat, startLon,
        //     latitude, longitude,
        //     X + Width, Y + Height,
        //     X, Y + Height
        // );
        //
        // var doesIntersectLeft = LineIntersectionChecker.DoLinesIntersectRaw(
        //     startLat, startLon,
        //     latitude, longitude,
        //     X, Y + Height,
        //     X, Y
        // );
        //
        // return doesIntersectTop || doesIntersectRight || doesIntersectBottom || doesIntersectLeft;
    }
}
