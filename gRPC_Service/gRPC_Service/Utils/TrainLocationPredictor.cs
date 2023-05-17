using System.Globalization;

namespace gRPC_Service.Utils;

using System;


public class TrainLocationPredictor
{
    private List<List<Tuple<double, double>>> railways;
    private List<List<dynamic>> tracks;
    
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

        // return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        
        // Find the closest point on any track to the extrapolated location
        dynamic closestPoint = null;
        var minDistance = double.PositiveInfinity;

        foreach (var track in tracks)
        {
            for (int i = 0; i < track.Count - 1; i++)
            {
                var point1 = track[i];
                var point2 = track[i+1];

                var point = PointUtils.ClosestPointOnLine(
                    Tuple.Create(point1.Latitude, point1.Longitude),
                    Tuple.Create(point2.Latitude, point2.Longitude),
                    Tuple.Create(extrapolatedLatitude, extrapolatedLongitude));
                
                // var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point1.Latitude, point1.Longitude);
                var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point.Latitude, point.Longitude);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPoint = point;
                }
            }
        }
        
        var distance1 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, closestPoint.Latitude, closestPoint.Longitude);
        var distance2 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, extrapolatedLatitude, extrapolatedLongitude);

        if (distance1 > distance2 || Math.Abs(distance1 - distance2) < 0.0003)
        {
            return Tuple.Create(closestPoint.Latitude, closestPoint.Longitude);
        }
        
        return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
    }
    
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Calculate the distance between two latitude-longitude coordinates
        // You can use the Haversine formula or any other distance calculation method
        // Here's a simple example using the Euclidean distance
        return Math.Sqrt(Math.Pow(lat2 - lat1, 2) + Math.Pow(lon2 - lon1, 2));
    }
    
}

