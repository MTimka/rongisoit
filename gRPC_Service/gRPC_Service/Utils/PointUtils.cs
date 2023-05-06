namespace gRPC_Service.Utils;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static System.Math;

public class PointUtils
{
    public static bool g_bDebug = false;
    
    public static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
    
    public static Tuple<double, double> ClosestPointOnLine(Tuple<double, double> linePoint1, Tuple<double, double> linePoint2, Tuple<double, double> point)
    {
        double x1 = linePoint1.Item1;
        double y1 = linePoint1.Item2;
        double x2 = linePoint2.Item1;
        double y2 = linePoint2.Item2;
        double x3 = point.Item1;
        double y3 = point.Item2;

        // Calculate the slope of the line.
        double slope;
        if (Math.Abs(x2 - x1) < double.Epsilon)
        {
            slope = double.PositiveInfinity;
        }
        else
        {
            slope = (y2 - y1) / (x2 - x1);
        }

        // Calculate the y-intercept of the line.
        double yIntercept = y1 - slope * x1;

        // Calculate the x-coordinate of the closest point on the line.
        double x;
        if (x3 < Math.Min(x1, x2))
        {
            x = x1;
        }
        else if (x3 > Math.Max(x1, x2))
        {
            x = x2;
        }
        else
        {
            x = (slope * y3 + x3 - slope * yIntercept) / (slope * slope + 1);
        }

        // Calculate the y-coordinate of the closest point on the line.
        double y;
        if (y3 < Math.Min(y1, y2))
        {
            y = y1;
        }
        else if (y3 > Math.Max(y1, y2))
        {
            y = y2;
        }
        else
        {
            y = slope * x + yIntercept;
        }

        return Tuple.Create(x, y);
    }

    public static Tuple<double, double> GetPointOnTrack(List<dynamic> track, Tuple<double, double> point)
    {
        Tuple<double, double> closestPoint = null;
        double closestPointDif = 0;

        for (int i = 1; i < track.Count; i++)
        {
            var linePoint1 = Tuple.Create((double)track[i - 1].Latitude, (double)track[i - 1].Longitude);
            var linePoint2 = Tuple.Create((double)track[i].Latitude, (double)track[i].Longitude);

            var closestPointOnLine = ClosestPointOnLine(linePoint1, linePoint2, point);
            double x = closestPointOnLine.Item1;
            double y = closestPointOnLine.Item2;

            double dif = Math.Abs(x - point.Item1) + Math.Abs(y - point.Item2);

            if (closestPoint == null || dif < closestPointDif)
            {
                closestPoint = Tuple.Create(x, y);
                closestPointDif = dif;
            }
        }

        return closestPoint;
    }
    
    public static int GetClosestNodeIndexOnTrack(List<dynamic> track, Tuple<double, double> point)
    {
        int closestNodeIndex = -1;
        double closestDif = 0;

        for (int i = 0; i < track.Count; i++)
        {
            double dif = Math.Abs((double)track[i].Latitude - point.Item1) + Math.Abs((double)track[i].Longitude - point.Item2);

            if (closestNodeIndex == -1 || dif < closestDif)
            {
                closestNodeIndex = i;
                closestDif = dif;
            }
        }

        return closestNodeIndex;
    }

    public static Tuple<double, double> TrackWalker(List<List<dynamic>> tracks, List<dynamic> trainLocations, int closestTrackIndex, long timestamp)
    {
        // DateTime timestampDt = DateTime.ParseExact(timestamp, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
        // DateTime lastTrainLocationDt = DateTime.ParseExact(trainLocations.Last().timestamp, "yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

        var timestampDt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
        var lastTrainLocationDt = DateTimeOffset.FromUnixTimeSeconds(trainLocations.Last().timestamp);
        
        double secondsToWalk = (timestampDt - lastTrainLocationDt).TotalSeconds;
        if (g_bDebug == true)
        { Console.WriteLine("seconds_to_walk: " + secondsToWalk); }

        double totalDistance = 0;
        DateTimeOffset? previousTime = null;
        dynamic previousLocation = null;
        foreach (dynamic location in trainLocations)
        {
            double latitude1 = ToRadians(location.Latitude);
            double longitude1 = ToRadians(location.Longitude);
            var time = DateTimeOffset.FromUnixTimeSeconds(location.timestamp);

            if (previousTime != null)
            {
                double timeDifference = (time - previousTime.Value).TotalSeconds;
                double latitude2 = ToRadians(previousLocation.Latitude);
                double longitude2 = ToRadians(previousLocation.Longitude);

                double distance = 2 * 6371 * Asin(Sqrt(Sin((latitude2 - latitude1) / 2) * Sin((latitude2 - latitude1) / 2) + Cos(latitude1) * Cos(latitude2) * Sin((longitude2 - longitude1) / 2) * Sin((longitude2 - longitude1) / 2)));
                totalDistance += distance;

                double speed = distance / timeDifference;
                
                if (g_bDebug == true)
                { Console.WriteLine("Speed: " + speed + " km/s"); }
            }

            previousTime = time;
            previousLocation = location;
        }

        double averageDistancePerSecond = totalDistance / (trainLocations.Count - 1);
        
        if (g_bDebug == true)
        { Console.WriteLine("Average distance traveled per second: " + averageDistancePerSecond + " km/s"); } 

        dynamic loc = trainLocations.Last();
        Tuple<double, double> point = new Tuple<double, double>(loc.Latitude, loc.Longitude);
        dynamic closestTrack = tracks[closestTrackIndex];
        Tuple<double, double> closestPoint = GetPointOnTrack(closestTrack, point);
        
        if (g_bDebug == true)
        { Console.WriteLine("closest_point: " + closestPoint); }

        dynamic loc2 = trainLocations[trainLocations.Count - 2];
        Tuple<double, double> point2 = new Tuple<double, double>(loc2.Latitude, loc2.Longitude);
        Tuple<double, double> closestPoint2 = GetPointOnTrack(closestTrack, point2);
        
        if (g_bDebug == true)
        { Console.WriteLine("closest_point2: " + closestPoint2); }

        int closestNodeIndex = GetClosestNodeIndexOnTrack(closestTrack, point);
        if (g_bDebug == true)
        { Console.WriteLine("closest_node_index: " + closestNodeIndex); }
        dynamic closestNode = closestTrack[closestNodeIndex];
        dynamic lastNode = null;
        dynamic nextNode = null;

        double closestNodeDifToClosestPoint1 = Abs(closestPoint.Item1 - closestNode.Latitude) + Abs(closestPoint.Item2 - closestNode.Longitude);
        double closestNodeDifToClosestPoint2 = Abs(closestPoint2.Item1 - closestNode.Latitude) + Abs(closestPoint2.Item2 - closestNode.Longitude);
        double? lastNodeDifToClosestPoint1 = null;
        double? lastNodeDifToClosestPoint2 = null;
        double? nextNodeDifToClosestPoint1 = null;
        double? nextNodeDifToClosestPoint2 = null;

        if (closestNodeIndex > 0)
        {
            lastNode = closestTrack[closestNodeIndex - 1];
            lastNodeDifToClosestPoint1 = Abs(closestPoint.Item1 - lastNode.Latitude) + Abs(closestPoint.Item2 - lastNode.Longitude);
            lastNodeDifToClosestPoint2 = Abs(closestPoint2.Item1 - lastNode.Latitude) + Abs(closestPoint2.Item2 - lastNode.Longitude);
        }

        if (closestNodeIndex < closestTrack.Count - 1)
        {
            nextNode = closestTrack[closestNodeIndex + 1];
            nextNodeDifToClosestPoint1 = Abs(closestPoint.Item1 - nextNode.Latitude) + Abs(closestPoint.Item2 - nextNode.Longitude);
            nextNodeDifToClosestPoint2 = Abs(closestPoint2.Item1 - nextNode.Latitude) + Abs(closestPoint2.Item2 - nextNode.Longitude);
        }

        if (g_bDebug == true)
        {
            Console.WriteLine("closest_node_dif_to_closest_point1: " + closestNodeDifToClosestPoint1);
            Console.WriteLine("closest_node_dif_to_closest_point2: " + closestNodeDifToClosestPoint2);
            Console.WriteLine("last_node_dif_to_closest_point1: " + lastNodeDifToClosestPoint1);
            Console.WriteLine("last_node_dif_to_closest_point2: " + lastNodeDifToClosestPoint2);
            Console.WriteLine("next_node_dif_to_closest_point1: " + nextNodeDifToClosestPoint1);
            Console.WriteLine("next_node_dif_to_closest_point2: " + nextNodeDifToClosestPoint2);
        }

        bool movingNextOnTrack = true;

        if (lastNodeDifToClosestPoint1 == null || nextNodeDifToClosestPoint1 == null)
        {
            if (lastNodeDifToClosestPoint1 == null)
            {
                if (nextNodeDifToClosestPoint1 < nextNodeDifToClosestPoint2)
                {
                    movingNextOnTrack = true;
                }
                else if (closestNodeDifToClosestPoint1 < closestNodeDifToClosestPoint2)
                {
                    movingNextOnTrack = false;
                }
            }
            else if (nextNodeDifToClosestPoint1 == null)
            {
                if (lastNodeDifToClosestPoint1 < lastNodeDifToClosestPoint2)
                {
                    movingNextOnTrack = false;
                }
                else if (closestNodeDifToClosestPoint1 < closestNodeDifToClosestPoint2)
                {
                    movingNextOnTrack = true;
                }
            }
        }
        else
        {
            if (nextNodeDifToClosestPoint1 < nextNodeDifToClosestPoint2)
            {
                movingNextOnTrack = true;
            }
            else if (lastNodeDifToClosestPoint1 < lastNodeDifToClosestPoint2)
            {
                movingNextOnTrack = false;
            }
        }

        if (g_bDebug == true)
        { Console.WriteLine("moving_next_on_track: " + movingNextOnTrack); }

        int nextNodeIndex;

        if (movingNextOnTrack && (nextNodeDifToClosestPoint1 == null || closestNodeDifToClosestPoint1 < nextNodeDifToClosestPoint1))
        {
            nextNodeIndex = closestNodeIndex;
        }
        else if (movingNextOnTrack)
        {
            nextNodeIndex = closestNodeIndex + 1;
        }
        else if (!movingNextOnTrack && (lastNodeDifToClosestPoint1 == null || closestNodeDifToClosestPoint1 < lastNodeDifToClosestPoint1))
        {
            nextNodeIndex = closestNodeIndex;
        }
        else
        {
            nextNodeIndex = closestNodeIndex - 1;
        }

        if (g_bDebug == true)
        { Console.WriteLine("next_node_index: " + nextNodeIndex); }

        var currentLoc = closestPoint;
        var currentTrackIndex = closestTrackIndex;
        var trackIndexCache = new List<int>() { currentTrackIndex };
        var currentTrack = tracks[closestTrackIndex];
        var secondsLeftToWalk = secondsToWalk;

        while (secondsLeftToWalk > 0)
        {
            if (g_bDebug == true)
            { Console.WriteLine("walker index: " + nextNodeIndex); }

            var latitude1 = ToRadians(currentLoc.Item1);
            var longitude1 = ToRadians(currentLoc.Item2);

            var latlon = currentTrack[nextNodeIndex];

            var latitude2 = ToRadians(latlon.Latitude);
            var longitude2 = ToRadians(latlon.Longitude);
            var distance = 2 * 6371 * Asin(Sqrt(Sin((latitude2 - latitude1) / 2) * Sin((latitude2 - latitude1) / 2) + Cos(latitude1) * Cos(latitude2) * Sin((longitude2 - longitude1) / 2) * Sin((longitude2 - longitude1) / 2)));

            var timeToTravelDistance = distance * 1 / averageDistancePerSecond;
            
            if (g_bDebug == true)
            { Console.WriteLine("time_to_travel_distance: " + timeToTravelDistance); }

            secondsLeftToWalk -= timeToTravelDistance;
            currentLoc = Tuple.Create(latlon.Latitude, latlon.Longitude);

            if (movingNextOnTrack)
            {
                nextNodeIndex += 1;

                // find next track
                if (nextNodeIndex >= currentTrack.Count)
                {
                    var foundTrack = false;
                    for (var i = 0; i < tracks.Count; i++)
                    {
                        if (i == currentTrackIndex)
                            continue;

                        for (var j = 0; j < tracks[i].Count; j++)
                        {
                            var node = tracks[i][j];
                            if (Abs(node.Latitude - currentTrack[^1].Latitude) < 0.0000001 && Abs(node.Longitude - currentTrack[^1].Longitude) < 0.0000001)
                            {
                                if (g_bDebug == true)
                                { Console.WriteLine("new track continuation 1: " + i); }
                                
                                currentTrack = tracks[i];
                                currentTrackIndex = i;
                                
                                // check if we r looping somehow, then break out
                                if (trackIndexCache.Contains(currentTrackIndex))
                                { break; }
                                
                                trackIndexCache.Add(currentTrackIndex);
                                nextNodeIndex = j;
                                foundTrack = true;
                                break;
                            }
                        }
                        if (foundTrack)
                            break;
                    }
                    if (!foundTrack)
                        break;
                }
            }
            else
            {
                nextNodeIndex -= 1;

                // find next track
                if (nextNodeIndex < 0)
                {
                    var foundTrack = false;
                    for (var i = 0; i < tracks.Count; i++)
                    {
                        if (i == currentTrackIndex)
                            continue;

                        for (var j = 0; j < tracks[i].Count; j++)
                        {
                            var node = tracks[i][j];
                            if (Abs(node.Latitude - currentTrack[0].Latitude) < 0.0000001 && Abs(node.Longitude - currentTrack[0].Longitude) < 0.0000001)
                            {
                                if (g_bDebug == true)
                                { Console.WriteLine("new track continuation 2: " + i); }
                                
                                currentTrack = tracks[i];
                                currentTrackIndex = i;
                                
                                // check if we r looping somehow, then break out
                                if (trackIndexCache.Contains(currentTrackIndex))
                                { break; }
                                
                                trackIndexCache.Add(currentTrackIndex);
                                nextNodeIndex = j;
                                foundTrack = true;
                                break;
                            }
                        }
                        if (foundTrack)
                            break;
                    }
                    if (!foundTrack)
                        break;
                }
            }
        }

        return currentLoc;
    }
    
    public static Tuple<List<Tuple<double, double>>, int> GetClosestTract(dynamic loc, List<List<Tuple<double, double>>> railways)
    {
        List<Tuple<double, double>> closestTrack = null;
        Tuple<double, double> closestNode = null;
        double closestDif = 0.0;
        int closestTrackIndex = 0;

        for (int trackIndex = 0; trackIndex < railways.Count; trackIndex++)
        {
            var track = railways[trackIndex];

            for (int i = 0; i < track.Count; i++)
            {
                Tuple<double, double> lastNode = null;
                Tuple<double, double> currentNode = track[i];
                Tuple<double, double> nextNode = null;

                double? dif1 = null;
                double? dif2 = null;

                if (i > 0)
                {
                    lastNode = track[i - 1];
                    var closestPoint1 = ClosestPointOnLine(lastNode, currentNode, Tuple.Create(loc.Latitude, loc.Longitude));
                    dif1 = Math.Abs(loc.Latitude - closestPoint1.Item1) + Math.Abs(loc.Longitude - closestPoint1.Item2);
                }

                if (i < track.Count - 1)
                {
                    nextNode = track[i + 1];
                    var closestPoint2 = ClosestPointOnLine(nextNode, currentNode, Tuple.Create(loc.Latitude, loc.Longitude));
                    dif2 = Math.Abs(loc.Latitude - closestPoint2.Item1) + Math.Abs(loc.Longitude - closestPoint2.Item2);
                }

                double dif;
                if (dif1 == null)
                {
                    dif = dif2.Value;
                }
                else if (dif2 == null)
                {
                    dif = dif1.Value;
                }
                else
                {
                    dif = dif1.Value < dif2.Value ? dif1.Value : dif2.Value;
                }

                if (closestNode == null || dif < closestDif)
                {
                    closestDif = dif;
                    closestNode = currentNode;
                    closestTrack = track;
                    closestTrackIndex = trackIndex;
                }
            }
        }

        return Tuple.Create(closestTrack, closestTrackIndex);
    }

    public static double CalculateDifference(double lat1, double lon1, double lat2, double lon2)
    {
        return Math.Abs(lat1 - lat2) + Math.Abs(lon1 - lon2);
    }
}