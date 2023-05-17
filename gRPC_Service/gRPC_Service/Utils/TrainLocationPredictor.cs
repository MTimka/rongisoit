using System.Globalization;
using NetTopologySuite.Geometries;

namespace gRPC_Service.Utils;

using System;


public class BoundingBox
{
    public double MinLatitude { get; }
    public double MaxLatitude { get; }
    public double MinLongitude { get; }
    public double MaxLongitude { get; }

    public BoundingBox(double minLat, double maxLat, double minLon, double maxLon)
    {
        MinLatitude = minLat;
        MaxLatitude = maxLat;
        MinLongitude = minLon;
        MaxLongitude = maxLon;
    }

    public bool Intersects(BoundingBox other)
    {
        return !(other.MinLongitude > MaxLongitude ||
                 other.MaxLongitude < MinLongitude ||
                 other.MinLatitude > MaxLatitude ||
                 other.MaxLatitude < MinLatitude);
    }
}

public class QuadTreeNode
{
    public double MinLatitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLongitude { get; set; }
    public QuadTreeNode[] Children { get; set; }
    public List<List<LatLng>> Tracks { get; set; }

    public bool IsLeaf => Children == null;

    // New property to store the bounding box of the node
    public BoundingBox BoundingBox { get; }

    public QuadTreeNode(double minLat, double maxLat, double minLon, double maxLon)
    {
        MinLatitude = minLat;
        MaxLatitude = maxLat;
        MinLongitude = minLon;
        MaxLongitude = maxLon;
        Children = null;
        Tracks = new List<List<LatLng>>();
        BoundingBox = new BoundingBox(minLat, maxLat, minLon, maxLon);
    }

    // New method to check if a bounding box intersects with the node's bounding box
    public bool Intersects(BoundingBox boundingBox)
    {
        return BoundingBox.Intersects(boundingBox);
    }
}

public class QuadTree
{
    private QuadTreeNode root;
    private const int MaxTracksPerNode = 10;

    public QuadTree(double minLat, double maxLat, double minLon, double maxLon)
    {
        root = new QuadTreeNode(minLat, maxLat, minLon, maxLon);
    }

    public void Insert(List<LatLng> track)
    {
        InsertRecursive(root, track, root.BoundingBox);
    }

    private void InsertRecursive(QuadTreeNode node, List<LatLng> track, BoundingBox boundingBox)
    {
        if (!node.Intersects(boundingBox))
            return;

        if (node.IsLeaf)
        {
            node.Tracks.Add(track);

            // Check if the node needs to split
            if (node.Tracks.Count > MaxTracksPerNode)
            {
                SplitNode(node);

                // Re-insert the tracks into the new child nodes
                foreach (var t in node.Tracks)
                {
                    foreach (var child in node.Children)
                    {
                        InsertRecursive(child, t, child.BoundingBox);
                    }
                }

                node.Tracks.Clear(); // Clear the tracks in the parent node
            }

            return;
        }

        foreach (var child in node.Children)
        {
            InsertRecursive(child, track, child.BoundingBox);
        }
    }

    public List<List<LatLng>> FindTracksInRange(double centerLat, double centerLon, double range)
    {
        List<List<LatLng>> tracksInRange = new List<List<LatLng>>();
        double rangeSquared = range * range;
        BoundingBox rangeBoundingBox = new BoundingBox(centerLat - range, centerLat + range, centerLon - range, centerLon + range);
        FindTracksRecursive(root, rangeBoundingBox, centerLat, centerLon, rangeSquared, tracksInRange);
        return tracksInRange;
    }

    private void FindTracksRecursive(QuadTreeNode node, BoundingBox rangeBoundingBox, double centerLat, double centerLon, double rangeSquared, List<List<LatLng>> result)
    {
        if (!node.Intersects(rangeBoundingBox))
            return;

        if (node.IsLeaf)
        {
            foreach (var track in node.Tracks)
            {
                if (IsTrackWithinRange(track, centerLat, centerLon, rangeSquared))
                {
                    result.Add(track);
                }
            }

            return;
        }

        foreach (var child in node.Children)
        {
            FindTracksRecursive(child, rangeBoundingBox, centerLat, centerLon, rangeSquared, result);
        }
    }

    private void SplitNode(QuadTreeNode node)
    {
        double midLat = (node.MinLatitude + node.MaxLatitude) / 2;
        double midLon = (node.MinLongitude + node.MaxLongitude) / 2;

        node.Children = new QuadTreeNode[4];
        node.Children[0] = new QuadTreeNode(node.MinLatitude, midLat, node.MinLongitude, midLon);
        node.Children[1] = new QuadTreeNode(node.MinLatitude, midLat, midLon, node.MaxLongitude);
        node.Children[2] = new QuadTreeNode(midLat, node.MaxLatitude, node.MinLongitude, midLon);
        node.Children[3] = new QuadTreeNode(midLat, node.MaxLatitude, midLon, node.MaxLongitude);
    }
    
    private bool IsTrackWithinRange(List<LatLng> track, double centerLat, double centerLon, double rangeSquared)
    {
        foreach (var point in track)
        {
            double lat = point.Latitude;
            double lon = point.Longitude;
            double distanceSquared = (lat - centerLat) * (lat - centerLat) + (lon - centerLon) * (lon - centerLon);
            if (distanceSquared <= rangeSquared)
            {
                return true;
            }
        }

        return false;
    }
}

public class TrainLocationPredictor
{
    private List<List<Tuple<double, double>>> railways;
    private List<List<LatLng>> tracks;
    private QuadTree quadtree;
    
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
        
        tracks = new List<List<LatLng>>();
        foreach (List<Tuple<double, double>> track in railways)
        {
            var res = new List<LatLng>();
            foreach (Tuple<double, double> point in track)
            {
                var pointDict = new LatLng(point.Item1, point.Item2);
                res.Add(pointDict);
            }
            
            tracks.Add(res);
        }
        
        Console.WriteLine("build quadtree ");

        
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
        
        quadtree = new QuadTree(x, width, y, height);
        
        foreach (var track in tracks)
        {
            quadtree.Insert(track);
        }
        
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
        
        // var range = new BoundingBox(extrapolatedLatitude - radius, extrapolatedLongitude - radius, 2 * radius, 2 * radius);
        // List<Point> pointsInRange = quadtree.QueryRange(range);

        List<List<LatLng>> tracksInRange = quadtree.FindTracksInRange(extrapolatedLatitude, extrapolatedLongitude, radius);
        
        // foreach (var point in tracksInRange)
        // {
        //     Tuple<double, double> p = PointUtils.ClosestPointOnLine(
        //         Tuple.Create(lastPoint.Latitude, lastPoint.Longitude),
        //         Tuple.Create(point.X, point.Y),
        //         Tuple.Create(extrapolatedLatitude, extrapolatedLongitude));
        //     
        //     // var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point.X, point.Y);
        //     var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, p.Item1, p.Item2);
        //     
        //     if (distance < minDistance)
        //     {
        //         minDistance = distance;
        //         closestPoint = Tuple.Create(p.Item1, p.Item2);
        //     }
        // }
        
        foreach (var track in tracksInRange)
        {
            for (int i = 0; i < track.Count - 1; i++)
            {
                var point1 = track[i];
                var point2 = track[i+1];
        
                Tuple<double, double> point = PointUtils.ClosestPointOnLine(
                    Tuple.Create(point1.Latitude, point1.Longitude),
                    Tuple.Create(point2.Latitude, point2.Longitude),
                    Tuple.Create(extrapolatedLatitude, extrapolatedLongitude));
                
                // var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point1.Latitude, point1.Longitude);
                var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point.Item1, point.Item2);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = point;
                }
            }
        }

        if (closestPoint == null)
        {
            return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        }
        //
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

