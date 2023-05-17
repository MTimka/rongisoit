using System.Globalization;
using NetTopologySuite.Geometries;

namespace gRPC_Service.Utils;

using System;

public class BoundingBox {
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    
    public bool Intersects(double latitude, double longitude)
    {
        double minX = X;
        double minY = Y;
        double maxX = X + Width;
        double maxY = Y + Height;

        // Convert latitude and longitude to radians
        double latRad = latitude * Math.PI / 180.0;
        double lonRad = longitude * Math.PI / 180.0;

        // Earth radius in kilometers
        double earthRadiusKm = 6371.0;

        // Calculate the distance between the bounding box and the point
        double dx = earthRadiusKm * Math.Cos(latRad) * (Math.Cos(lonRad) - Math.Cos(minY * Math.PI / 180.0));
        double dy = earthRadiusKm * (Math.Sin(latRad) - Math.Sin(minX * Math.PI / 180.0));

        return (dx >= 0 && dx <= Width)
               && (dy >= 0 && dy <= Height);
    }
}

public class TrainLocationPredictor
{
    private List<List<Tuple<double, double>>> railways;
    private List<List<LatLng>> tracks;
    private List<Tuple<BoundingBox, List<LatLng>>> boxedTracks =  new List<Tuple<BoundingBox, List<LatLng>>>();
    
    private const double EarthRadiusKm = 6371.0;

    
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
        
        foreach (var track in tracks)
        {
            // get bounding box
            double minLat = double.MaxValue;
            double minLon = double.MaxValue;
            double maxLat = double.MinValue;
            double maxLon = double.MinValue;

            foreach (var point in track)
            {
                minLat = Math.Min(minLat, point.Latitude);
                minLon = Math.Min(minLon, point.Longitude);
                maxLat = Math.Max(maxLat, point.Latitude);
                maxLon = Math.Max(maxLon, point.Longitude);
            }

            // Convert the latitude and longitude to radians
            double minLatRad = minLat * Math.PI / 180.0;
            double maxLatRad = maxLat * Math.PI / 180.0;
            double minLonRad = minLon * Math.PI / 180.0;
            double maxLonRad = maxLon * Math.PI / 180.0;

            // Calculate the distance between two points using the Haversine formula
            double distanceLat = maxLatRad - minLatRad;
            double distanceLon = maxLonRad - minLonRad;
            double a = Math.Sin(distanceLat / 2) * Math.Sin(distanceLat / 2) +
                       Math.Cos(minLatRad) * Math.Cos(maxLatRad) *
                       Math.Sin(distanceLon / 2) * Math.Sin(distanceLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = EarthRadiusKm * c;

            // Calculate the bounding box dimensions
            double width = distance;
            double height = distance;

            // Calculate the center of the bounding box
            double centerLat = (minLat + maxLat) / 2.0;
            double centerLon = (minLon + maxLon) / 2.0;

            // Calculate the top-left corner of the bounding box
            double x = centerLat - (width / 2.0);
            double y = centerLon - (height / 2.0);

            BoundingBox boundingBox = new BoundingBox { X = x, Y = y, Width = width, Height = height };
            boxedTracks.Add(Tuple.Create(boundingBox, track));
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

        foreach (var boxedTrack in boxedTracks)
        {
            if (boxedTrack.Item1.Intersects(extrapolatedLatitude, extrapolatedLongitude))
            {
                Console.WriteLine("itersection");
                var track = boxedTrack.Item2;
                
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
        }

        if (closestPoint == null)
        {
            return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        }
        
        var distance1 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, closestPoint.Item1, closestPoint.Item2);
        var distance2 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, extrapolatedLatitude, extrapolatedLongitude);
        
        if (distance1 > distance2 || Math.Abs(distance1 - distance2) < 0.0003)
        {
            return Tuple.Create(closestPoint.Item1, closestPoint.Item2);
        }
        
        return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        
        // return Tuple.Create(closestPoint.Item1, closestPoint.Item2);
    }
    
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Calculate the distance between two latitude-longitude coordinates
        // You can use the Haversine formula or any other distance calculation method
        // Here's a simple example using the Euclidean distance
        return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2));
    }
    
}

