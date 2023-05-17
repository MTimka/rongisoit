namespace gRPC_Service.Utils;

public class SimplePredictor
{
    public static Tuple<double, double> PredictLocation(List<TrainLocation> trainLocations, double targetTimestamp)
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

        return Tuple.Create(extrapolatedLatitude, extrapolatedLongitude);
    }

}