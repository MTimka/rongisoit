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
    
    public static Tuple<double, double> ClosestPointOnLine(LatLng linePoint1, LatLng linePoint2, TrainLocation point)
    {
        double x1 = linePoint1.Latitude;
        double y1 = linePoint1.Longitude;
        double x2 = linePoint2.Latitude;
        double y2 = linePoint2.Longitude;
        double x3 = point.Latitude;
        double y3 = point.Longitude;

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
    
    public static double GetBearing(double p1Lat, double p1Lon, double p2Lat, double p2Lon)
    {
        double lat1 = p1Lat * (Math.PI / 180.0);
        double lon1 = p1Lon * (Math.PI / 180.0);
        double lat2 = p2Lat * (Math.PI / 180.0);
        double lon2 = p2Lon * (Math.PI / 180.0);

        double dlon = lon2 - lon1;

        double y = Math.Sin(dlon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) - Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dlon);

        double bearing = Math.Atan2(y, x);
        bearing = bearing * (180.0 / Math.PI);
        bearing = (bearing + 360) % 360;

        return bearing;
    }

    public static int DetermineMovementDirection(List<LatLng> trackNodes, TrainLocation lastPosition, TrainLocation secondLastPosition)
    {
        double trainBearing = GetBearing(secondLastPosition.Latitude, secondLastPosition.Longitude, lastPosition.Latitude, lastPosition.Longitude);

        var closestLastPosition = trackNodes.OrderBy(node => CalculateDistance(lastPosition.Latitude, lastPosition.Longitude, node.Latitude, node.Longitude)).First();
        int lastIndex = trackNodes.IndexOf(closestLastPosition);

        if (lastIndex == 0 || lastIndex < trackNodes.Count - 1)
        {
            int nextIndex = (lastIndex + 1) % trackNodes.Count;
            var nextNode = trackNodes[nextIndex];
            double trackBearing = GetBearing(trackNodes[lastIndex].Latitude, trackNodes[lastIndex].Longitude, nextNode.Latitude, nextNode.Longitude);

            if (g_bDebug) { Console.WriteLine("track_bearing next: " + trackBearing + ", train_bearing: " + trainBearing); }

            if (Math.Abs(trackBearing - trainBearing) > 90)
            {
                return -1; // Backward (-1)
            }
            else
            {
                return 1; // Forward (+1)
            }
        }
        else
        {
            int previousIndex = (lastIndex - 1) % trackNodes.Count;
            var previousNode = trackNodes[previousIndex];
            double trackBearing = GetBearing(trackNodes[lastIndex].Latitude, trackNodes[lastIndex].Longitude, previousNode.Latitude, previousNode.Longitude);

            if (g_bDebug) { Console.WriteLine("track_bearing previous: " + trackBearing + ", train_bearing: " + trainBearing); }

            if (Math.Abs(trackBearing - trainBearing) > 90)
            {
                return 1; // Forward (+1)
            }
            else
            {
                return -1; // Backward (-1)
            }
        }
    }

    public static double CalculateDifference(double lat1, double lon1, double lat2, double lon2)
    {
        return Math.Abs(lat1 - lat2) + Math.Abs(lon1 - lon2);
    }
    
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Convert latitude and longitude to radians
        double lat1Rad = lat1 * Math.PI / 180.0;
        double lon1Rad = lon1 * Math.PI / 180.0;
        double lat2Rad = lat2 * Math.PI / 180.0;
        double lon2Rad = lon2 * Math.PI / 180.0;

        // Earth radius in kilometers
        double earthRadiusKm = 6371.0;

        // Calculate the differences in latitude and longitude
        double dLat = lat2Rad - lat1Rad;
        double dLon = lon2Rad - lon1Rad;

        // Apply the Haversine formula
        double a = Math.Sin(dLat / 2.0) * Math.Sin(dLat / 2.0) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2.0) * Math.Sin(dLon / 2.0);
        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        double distance = earthRadiusKm * c;

        return distance;
    }
    
    public static List<double> InterpolateGPSData(List<GPSDataPoint> gpsData, double targetTimestamp)
    {
        List<double> targetPosition = new List<double>();
        List<double> timestamps = gpsData.Select(data => data.Timestamp).ToList();
        List<double> latitudes = gpsData.Select(data => data.Latitude).ToList();
        List<double> longitudes = gpsData.Select(data => data.Longitude).ToList();

        Func<double, double> interpolateLatitude = Interpolate(timestamps, latitudes);
        Func<double, double> interpolateLongitude = Interpolate(timestamps, longitudes);

        double interpolatedLatitude = interpolateLatitude(targetTimestamp);
        double interpolatedLongitude = interpolateLongitude(targetTimestamp);

        targetPosition.Add(interpolatedLatitude);
        targetPosition.Add(interpolatedLongitude);

        return targetPosition;
    }

    public static Func<double, double> Interpolate(List<double> xValues, List<double> yValues)
    {
        if (xValues.Count != yValues.Count)
        {
            throw new ArgumentException("The number of x-values must be equal to the number of y-values.");
        }

        List<double> sortedXValues = new List<double>(xValues);
        List<double> sortedYValues = new List<double>(yValues);
        sortedXValues.Sort();

        return x =>
        {
            if (x < sortedXValues[0])
            {
                return Extrapolate(x, sortedXValues[0], sortedXValues[1], sortedYValues[0], sortedYValues[1]);
            }
            else if (x > sortedXValues[sortedXValues.Count - 1])
            {
                return Extrapolate(x, sortedXValues[sortedXValues.Count - 2], sortedXValues[sortedXValues.Count - 1],
                    sortedYValues[sortedYValues.Count - 2], sortedYValues[sortedYValues.Count - 1]);
            }
            else
            {
                int index = sortedXValues.BinarySearch(x);
                if (index < 0)
                {
                    index = ~index;
                }
                if (index == 0)
                {
                    return Extrapolate(x, sortedXValues[0], sortedXValues[1], sortedYValues[0], sortedYValues[1]);
                }
                else if (index == sortedXValues.Count)
                {
                    return Extrapolate(x, sortedXValues[sortedXValues.Count - 2], sortedXValues[sortedXValues.Count - 1],
                        sortedYValues[sortedYValues.Count - 2], sortedYValues[sortedYValues.Count - 1]);
                }
                else
                {
                    return Interpolate(x, sortedXValues[index - 1], sortedXValues[index], sortedYValues[index - 1], sortedYValues[index]);
                }
            }
        };
    }

    public static double Extrapolate(double x, double x0, double x1, double y0, double y1)
    {
        double slope = (y1 - y0) / (x1 - x0);
        return y0 + slope * (x - x0);
    }

    public static double Interpolate(double x, double x0, double x1, double y0, double y1)
    {
        double t = (x - x0) / (x1 - x0);
        return y0 + t * (y1 - y0);
    }
}

public class GPSDataPoint
{
    public double Timestamp { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
