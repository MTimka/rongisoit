namespace gRPC_Service.Utils;

public class SimplePredictor
{
    public static Tuple<double, double> PredictLocation(List<TrainLocation> trainLocations, double timestamp)
    {
        // Convert timestamp to DateTime
        var targetTime = timestamp;

        // Prepare data for linear regression
        List<double> timestamps = new List<double>();
        List<double> latitudes = new List<double>();
        List<double> longitudes = new List<double>();

        foreach (var location in trainLocations)
        {
            var locationTime = location.Timestamp;

            // Calculate time difference in seconds
            double timeDiff = targetTime - locationTime;

            timestamps.Add(timeDiff);
            latitudes.Add((double)location.Latitude);
            longitudes.Add((double)location.Longitude);
        }

        // Perform linear regression
        double latitude = LinearRegressionModel(latitudes, timestamps);
        double longitude = LinearRegressionModel(longitudes, timestamps);

        // Create and return the predicted location
        var predictedLocation = Tuple.Create(latitude, longitude);

        return predictedLocation;
    }

    private static double LinearRegressionModel(List<double> y, List<double> x)
    {
        double sumX = 0.0;
        double sumY = 0.0;
        double sumXY = 0.0;
        double sumX2 = 0.0;

        int n = y.Count;

        for (int i = 0; i < n; i++)
        {
            sumX += x[i];
            sumY += y[i];
            sumXY += x[i] * y[i];
            sumX2 += x[i] * x[i];
        }

        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;

        return slope * x[n - 1] + intercept; // Predict the value at the last timestamp
    }
}