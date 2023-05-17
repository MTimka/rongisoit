using System.Globalization;
using NetTopologySuite.Geometries;

namespace gRPC_Service.Utils;

using System;


public class Quadtree
{
    private const int MaxPointsPerNode = 10;

    private readonly QuadtreeNode root;

    public Quadtree(BoundingBox boundary)
    {
        root = new QuadtreeNode(boundary);
    }

    public void Insert(Point point)
    {
        root.Insert(point);
    }

    public List<Point> QueryRange(BoundingBox range)
    {
        var result = new List<Point>();
        root.QueryRange(range, result);
        return result;
    }
}

public class QuadtreeNode
{
    private const int MaxPointsPerNode = 100;

    private readonly BoundingBox boundary;
    private readonly List<Point> points;
    private QuadtreeNode[] children;

    public QuadtreeNode(BoundingBox boundary)
    {
        this.boundary = boundary;
        points = new List<Point>();
        children = null;
    }

    public void Insert(Point point)
    {
        if (!boundary.Contains(point))
            return;

        if (points.Count < MaxPointsPerNode)
        {
            points.Add(point);
        }
        else
        {
            if (children == null)
                Split();

            foreach (var child in children)
                child.Insert(point);
        }
    }

    private void Split()
    {
        var subWidth = boundary.Width / 2;
        var subHeight = boundary.Height / 2;
        var x = boundary.X;
        var y = boundary.Y;

        children = new QuadtreeNode[4];
        children[0] = new QuadtreeNode(new BoundingBox(x, y, subWidth, subHeight));
        children[1] = new QuadtreeNode(new BoundingBox(x + subWidth, y, subWidth, subHeight));
        children[2] = new QuadtreeNode(new BoundingBox(x, y + subHeight, subWidth, subHeight));
        children[3] = new QuadtreeNode(new BoundingBox(x + subWidth, y + subHeight, subWidth, subHeight));

        foreach (var point in points)
        {
            foreach (var child in children)
            {
                if (child.boundary.Contains(point))
                {
                    child.Insert(point);
                    break;
                }
            }
        }

        points.Clear();
    }

    public void QueryRange(BoundingBox range, List<Point> result)
    {
        if (!boundary.Intersects(range))
            return;

        foreach (var point in points)
        {
            if (range.Contains(point))
                result.Add(point);
        }

        if (children != null)
        {
            foreach (var child in children)
                child.QueryRange(range, result);
        }
    }
}

public class BoundingBox
{
    public double X { get; }
    public double Y { get; }
    public double Width { get; }
    public double Height { get; }

    public BoundingBox(double x, double y, double width, double height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public bool Contains(Point point)
    {
        return point.X >= X && point.X <= X + Width && point.Y >= Y && point.Y <= Y + Height;
    }

    public bool Intersects(BoundingBox other)
    {
        return X < other.X + other.Width && X + Width > other.X && Y < other.Y + other.Height && Y + Height > other.Y;
    }
}

public class Point
{
    public double X { get; }
    public double Y { get; }

    public Point? Previous { get; set; } = null;

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }
}

public class TrainLocationPredictor
{
    private List<List<Tuple<double, double>>> railways;
    private List<List<dynamic>> tracks;
    private Quadtree quadtree;
    
    public TrainLocationPredictor()
    {
        railways = new List<List<Tuple<double, double>>>();
        string filePath = "train_tracks.data";
        
        // Open the file for reading
        using (StreamReader reader = new StreamReader(filePath))
        {
            string line;
            int lineCount = 0;
            
            // Read and process each line until the end of the file is reached
            while ((line = reader.ReadLine()) != null)
            {
                lineCount += 1;

                // Process the line
                var splits = line.Split(" ");
                
                if (splits.Length < 4)
                {
                    continue;
                }

                var track = new List<Tuple<double, double>>();
                // Console.WriteLine("line " + lineCount);
                for (var i = 0; i < splits.Length; i += 2)
                {
                    var item = Tuple.Create(
                        Convert.ToDouble(splits[i], CultureInfo.InvariantCulture), 
                        Convert.ToDouble(splits[i+1], CultureInfo.InvariantCulture)
                    );
                    track.Add(item);
                }
                
                railways.Add(track);
            }
        }

        Console.WriteLine("railways len " + railways.Count);
        
        tracks = new List<List<dynamic>>();
        SpatialIndex<LatLng> index = new SpatialIndex<LatLng>();

        foreach (List<Tuple<double, double>> track in railways)
        {
            var res = new List<dynamic>();
            foreach (Tuple<double, double> point in track)
            {
                var pointDict = new 
                {
                    Latitude = point.Item1,
                    Longitude = point.Item2
                };
                res.Add(pointDict);
            }
            
            tracks.Add(res);
        }
        
        Console.WriteLine("build quadtree ");

        quadtree = new Quadtree(GetBoundaryOfAllTracks(tracks));
        
        foreach (var track in tracks)
        {
            for (int i = 1; i < track.Count; i++)
            {
                var pp = new Point(track[0].Latitude, track[0].Longitude);
                pp.Previous = new Point(track[1].Latitude, track[1].Longitude);
                quadtree.Insert(pp);
                
                var point1 = track[i];
                var point2 = track[i-1];
            
                var p = new Point(point1.Latitude, point1.Longitude);
                p.Previous = new Point(point2.Latitude, point2.Longitude);
                quadtree.Insert(p);
            }
        }
        
    }
    
    public BoundingBox GetBoundaryOfAllTracks(List<List<dynamic>> tracks)
    {
        double minX = double.MaxValue;
        double minY = double.MaxValue;
        double maxX = double.MinValue;
        double maxY = double.MinValue;

        foreach (var track in tracks)
        {
            foreach (var point in track)
            {
                minX = Math.Min(minX, point.Latitude);
                minY = Math.Min(minY, point.Longitude);
                maxX = Math.Max(maxX, point.Latitude);
                maxY = Math.Max(maxY, point.Longitude);
            }
        }

        double width = maxX - minX;
        double height = maxY - minY;
        double x = minX;
        double y = minY;

        return new BoundingBox(x, y, width, height);
    }

    public Tuple<double, double> PredictLocation(List<TrainLocation> trainLocations, double targetTimestamp)
    {
        // Extrapolate the location based on the rate of change between the last two known data points

        // Get the last two known data points
        var secondLastPoint = trainLocations[trainLocations.Count - 2];
        var lastPoint = trainLocations[trainLocations.Count - 1];

        // Calculate the time difference and the fraction of time passed
        double timeDiff = lastPoint.Timestamp - secondLastPoint.Timestamp;
        double fraction = (targetTimestamp - lastPoint.Timestamp) / timeDiff;

        // Extrapolate the latitude and longitude
        double latitudeDiff = lastPoint.Latitude - secondLastPoint.Latitude;
        double longitudeDiff = lastPoint.Longitude - secondLastPoint.Longitude;

        double extrapolatedLatitude = lastPoint.Latitude + fraction * latitudeDiff;
        double extrapolatedLongitude = lastPoint.Longitude + fraction * longitudeDiff;

        // Find the closest point on any track to the extrapolated location
        Tuple<double, double> closestPoint = null;
        var minDistance = double.PositiveInfinity;
        
        // foreach (var track in tracks)
        // {
        //     for (int i = 0; i < track.Count - 1; i++)
        //     {
        //         var point1 = track[i];
        //         var point2 = track[i+1];
        //
        //         Tuple<double, double> point = PointUtils.ClosestPointOnLine(
        //             Tuple.Create(point1.Latitude, point1.Longitude),
        //             Tuple.Create(point2.Latitude, point2.Longitude),
        //             Tuple.Create(extrapolatedLatitude, extrapolatedLongitude));
        //         
        //         // var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point1.Latitude, point1.Longitude);
        //         var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point.Item1, point.Item2);
        //         if (distance < minDistance)
        //         {
        //             minDistance = distance;
        //             closestPoint = point;
        //         }
        //     }
        // }

        var radius = 0.01;
        
        var range = new BoundingBox(extrapolatedLatitude - radius, extrapolatedLongitude - radius, radius, radius);
        List<Point> pointsInRange = quadtree.QueryRange(range);

        foreach (var point in pointsInRange)
        {
            Tuple<double, double> p = PointUtils.ClosestPointOnLine(
                Tuple.Create(point.X, point.Y),
                Tuple.Create(point.Previous.X, point.Previous.Y),
                Tuple.Create(extrapolatedLatitude, extrapolatedLongitude));
            
            // var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point.X, point.Y);
            var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, p.Item1, p.Item2);
            
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = Tuple.Create(p.Item1, p.Item2);
            }
        }

        if (closestPoint == null)
        {
            return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        }
        
        // var distance1 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, closestPoint.Item1, closestPoint.Item2);
        // var distance2 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, extrapolatedLatitude, extrapolatedLongitude);
        //
        // if (distance1 > distance2 || Math.Abs(distance1 - distance2) < 0.0003)
        // {
        //     return Tuple.Create(closestPoint.Item1, closestPoint.Item2);
        // }
        //
        // return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        
        return Tuple.Create(closestPoint.Item1, closestPoint.Item2);
    }
    
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Calculate the distance between two latitude-longitude coordinates
        // You can use the Haversine formula or any other distance calculation method
        // Here's a simple example using the Euclidean distance
        return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2));
    }
    
}

