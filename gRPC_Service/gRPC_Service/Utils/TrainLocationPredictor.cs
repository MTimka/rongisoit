using System.Globalization;
using NetTopologySuite.Geometries;

namespace gRPC_Service.Utils;

using System;

public class TrainLocationPredictor
{
    private List<List<Tuple<double, double>>> railways;
    private List<List<LatLng>> tracks;
    private List<Tuple<BoundingBox, List<LatLng>>> boxedTracks =  new List<Tuple<BoundingBox, List<LatLng>>>();
    
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

        double sizeInGps = 0.01;
        foreach (var track in tracks)
        {
            // get bounding box
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;

            foreach (var point in track)
            {
                minX = Math.Min(minX, point.Latitude);
                minY = Math.Min(minY, point.Longitude);
                maxX = Math.Max(maxX, point.Latitude);
                maxY = Math.Max(maxY, point.Longitude);
            }

            double width = (maxX - minX) + sizeInGps;
            double height = (maxY - minY) + sizeInGps;
            double x = minX - sizeInGps;
            double y = minY - sizeInGps;

            var bb = new BoundingBox { X = x, Y = y, Width = width, Height = height };
            boxedTracks.Add(Tuple.Create(bb, track));
        }
        
    }

    public List<Dictionary<string, object>> RecursiveSegmentFinder(List<Dictionary<string, object>> bdict, double trainSpeed, TrainLocation lastPoint, TrainLocation secondLastPoint)
{
    if (bdict.Count == 0 || (double)bdict[^1]["timestamp_num"] < 30000)
    {
        Console.WriteLine("Need more segment(s)");
        List<LatLng> nextTrack = null;
        int segmentDirection = 0;
        
        // Find connecting segments
        foreach (var boxedTrack in boxedTracks)
        {
            if (boxedTrack.Item1.Intersects(secondLastPoint.Latitude, secondLastPoint.Longitude,
                    lastPoint.Latitude, lastPoint.Longitude))
            {
                var trackSegment = boxedTrack.Item2;
                
                if (Math.Abs(trackSegment[0].Latitude - (double)bdict[^1]["latitude"]) < 0.00001 &&
                    Math.Abs(trackSegment[0].Longitude - (double)bdict[^1]["longitude"]) < 0.00001)
                {
                    segmentDirection = 1;
                    nextTrack = trackSegment;
                    break;
                }
                else if (Math.Abs(trackSegment[^1].Latitude - (double)bdict[^1]["latitude"]) < 0.00001 &&
                         Math.Abs(trackSegment[^1].Longitude - (double)bdict[^1]["longitude"]) < 0.00001)
                {
                    segmentDirection = -1;
                    nextTrack = trackSegment;
                    break;
                }
            }
        }
        
        if (nextTrack != null)
        {
            Console.WriteLine("segment_direction: " + segmentDirection);
            Console.WriteLine("next_track: " + nextTrack);
            
            // List<double> xValues = nextTrack.Select(node => node[1]).ToList();
            // List<double> yValues = nextTrack.Select(node => node[0]).ToList();
            // plt.Scatter(xValues, yValues, "black", "Next Track Nodes");
            
            int i = segmentDirection == 1 ? 0 : nextTrack.Count - 1;
            while (i >= 0 && i < nextTrack.Count)
            {
                double dist;
                if (bdict.Count == 0)
                {
                    dist = PointUtils.CalculateDistance(nextTrack[i].Latitude, nextTrack[i].Longitude, lastPoint.Latitude, lastPoint.Longitude);
                }
                else
                {
                    dist = PointUtils.CalculateDistance(nextTrack[i].Latitude, nextTrack[i].Longitude, (double)bdict[^1]["latitude"], (double)bdict[^1]["longitude"]);
                }
                
                bdict.Add(new Dictionary<string, object>
                {
                    { "latitude", nextTrack[i].Latitude },
                    { "longitude", nextTrack[i].Longitude },
                    { "timestamp_num", ((bdict.Count > 0) ? (double)bdict[^1]["timestamp_num"] : 0) + dist * trainSpeed }
                });
                
                i += segmentDirection;
            }
        }
    }
    else
    {
        return bdict;
    }
    
    return RecursiveSegmentFinder(bdict, trainSpeed, lastPoint, secondLastPoint);
}
    
    public Tuple<double, double> PredictLocation2(List<TrainLocation> trainLocations, double millisecondsToPredict)
    {
        // Find the nearest track node to the predicted future position
        var lastPoint = trainLocations.Last();
        var secondLastPoint = trainLocations[^2];

        List<double> distances = new List<double>();
        List<int> distancesIndices = new List<int>();
        var tracks = new List<List<LatLng>>();

        foreach (var boxedTrack in boxedTracks)
        {
            if (boxedTrack.Item1.Intersects(secondLastPoint.Latitude, secondLastPoint.Longitude,
                    lastPoint.Latitude, lastPoint.Longitude))
            {
                var trackSegment = boxedTrack.Item2;
                List<double> nodeDistances = new List<double>();

                for (int i = 1; i < trackSegment.Count; i++)
                {
                    var point = PointUtils.ClosestPointOnLine(trackSegment[i - 1], trackSegment[i], lastPoint);
                    double dist = PointUtils.CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, point.Item1, point.Item2);
                    nodeDistances.Add(dist);
                }

                int nearestNodeIndex = nodeDistances.IndexOf(nodeDistances.Min());
                distancesIndices.Add(nearestNodeIndex);
                distances.Add(nodeDistances[nearestNodeIndex]);
                tracks.Add(trackSegment);
            }
        }
        

        int nearestSegmentIndex = distances.IndexOf(distances.Min());
        var nearestSegment = tracks[nearestSegmentIndex];
        // unused from python forumla
        // var nearestNode = nearestSegment[distancesIndices[nearestSegmentIndex]];
        // var nextNode = nearestSegment[distancesIndices[nearestSegmentIndex] - 1];

        Console.WriteLine($"nearestSegment {nearestSegment.First().Latitude} {nearestSegment.First().Longitude} ");
        Console.WriteLine($"lastPoint {lastPoint.Latitude} {lastPoint.Longitude} ");
        Console.WriteLine($"lastPoint {secondLastPoint.Latitude} {secondLastPoint.Longitude} ");

        int segmentDirection = PointUtils.DetermineMovementDirection(nearestSegment, lastPoint, secondLastPoint);
        Console.WriteLine($"segmentDirection {segmentDirection} ");

        var trainSpeed = (lastPoint.Timestamp - secondLastPoint.Timestamp) /
                         PointUtils.CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, secondLastPoint.Latitude,
                             secondLastPoint.Longitude);
        Console.WriteLine($"trainSpeed {trainSpeed} ");

        List<Dictionary<string, object>> bDict = new List<Dictionary<string, object>>();
        int ii = distancesIndices[nearestSegmentIndex];
        while (ii >= 0 && ii < nearestSegment.Count)
        {
            double dist;
            if (bDict.Count == 0)
            {
                dist = PointUtils.CalculateDistance(nearestSegment[ii].Latitude, nearestSegment[ii].Longitude, lastPoint.Latitude, lastPoint.Longitude);
            }
            else
            {
                dist = PointUtils.CalculateDistance(nearestSegment[ii].Latitude, nearestSegment[ii].Longitude, (double)bDict[^1]["latitude"], (double)bDict[^1]["longitude"]);
            }

            bDict.Add(new Dictionary<string, object>()
            {
                { "latitude", nearestSegment[ii].Latitude },
                { "longitude", nearestSegment[ii].Longitude },
                { "timestamp_num", ((bDict.Count > 0) ? (double)bDict[^1]["timestamp_num"] : 0) + dist * trainSpeed }
            });

            ii += segmentDirection;
        }

        bDict = RecursiveSegmentFinder(bDict, trainSpeed, lastPoint, secondLastPoint);

        foreach (var obj in bDict)
        {
            Console.WriteLine($"obj {obj["latitude"]} {obj["longitude"]} {obj["timestamp_num"]} ");
        }
        
        // List<double> timestampNums = bDict.Select(entry => (double)entry["timestamp_num"]).ToList();
        // List<List<double>> latitudesAndLongitudes = new List<List<double>>()
        // {
        //     bDict.Select(entry => (double)entry["latitude"]).ToList(),
        //     bDict.Select(entry => (double)entry["longitude"]).ToList()
        // };
        
        var interPoleData = bDict.Select(entry => new GPSDataPoint()
        {
            Latitude = (double)entry["latitude"],
            Longitude = (double)entry["longitude"],
            Timestamp = (double)entry["timestamp_num"]
        }).ToList();

        List<double> futurePosition = PointUtils.InterpolateGPSData(interPoleData, millisecondsToPredict);
        Console.WriteLine($"Future position: Latitude = {futurePosition[0]}, Longitude = {futurePosition[1]}");

        return Tuple.Create(futurePosition[0], futurePosition[1]);
    }


    public Tuple<double, double> PredictLocation(List<TrainLocation> trainLocations, double targetTimestamp, bool deep = true)
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

        if (deep == false)
        {
            return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        }
        
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

        // foreach (var boxedTrack in boxedTracks)
        // {
        //     if (boxedTrack.Item1.Intersects(
        //             lastPoint.Latitude, lastPoint.Longitude,
        //             extrapolatedLatitude, extrapolatedLongitude))
        //     {
        //         var track = boxedTrack.Item2;
        //         
        //         for (int i = 0; i < track.Count - 1; i++)
        //         {
        //             var point1 = track[i];
        //             var point2 = track[i+1];
        //         
        //             Tuple<double, double> point = PointUtils.ClosestPointOnLine(
        //                 point1, point2,
        //                 Tuple.Create(extrapolatedLatitude, extrapolatedLongitude));
        //             
        //             // var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point1.Latitude, point1.Longitude);
        //             var distance = CalculateDistance(extrapolatedLatitude, extrapolatedLongitude, point.Item1, point.Item2);
        //             if (distance < minDistance)
        //             {
        //                 minDistance = distance;
        //                 closestPoint = point;
        //             }
        //         }
        //     }
        // }

        if (closestPoint == null)
        {
            return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
        }
        
        var distance1 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, closestPoint.Item1, closestPoint.Item2);
        var distance2 = CalculateDistance(lastPoint.Latitude, lastPoint.Longitude, extrapolatedLatitude, extrapolatedLongitude);
        
        if (Math.Abs(distance1 - distance2) < 0.0003)
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

    public static void Test1()
    {
        var locations = new List<TrainLocation>()
        {
            new TrainLocation
            {
                TrainId = "63",
                Latitude = 59.291303,
                Longitude = 25.534314,
                Timestamp = DateTimeOffset.ParseExact("2023-06-04 20:42:53", "yyyy-MM-dd' 'HH:mm:ss", CultureInfo.InvariantCulture).ToUnixTimeMilliseconds()
            },
            new TrainLocation
            {
                TrainId = "63",
                Latitude = 59.291348,
                Longitude = 25.533150,
                Timestamp = DateTimeOffset.ParseExact("2023-06-04 20:42:55", "yyyy-MM-dd' 'HH:mm:ss", CultureInfo.InvariantCulture).ToUnixTimeMilliseconds()
            },
            new TrainLocation
            {
                TrainId = "63",
                Latitude = 59.291393,
                Longitude = 25.531987,
                Timestamp = DateTimeOffset.ParseExact("2023-06-04 20:42:57", "yyyy-MM-dd' 'HH:mm:ss", CultureInfo.InvariantCulture).ToUnixTimeMilliseconds()
            },
        };

        TrainLocationPredictor predictor = new TrainLocationPredictor();
        Task.Run(() =>
        {
            Thread.Sleep(2000);
            predictor.PredictLocation2(locations, 30000);
        });

    }
    
}

