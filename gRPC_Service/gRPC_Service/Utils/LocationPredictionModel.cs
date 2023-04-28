namespace gRPC_Service.Utils;

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearRegression;

public class LocationPredictionModel
{
    private List<Tuple<double, double, double>> _trainLocations;

    public LocationPredictionModel()
    {
        _trainLocations = new List<Tuple<double, double, double>>();
    }
    
    public LocationPredictionModel(List<Tuple<double, double, double>> locations)
    {
        _trainLocations = locations;
    }

    public void AddTrainLocation(double latitude, double longitude, double timestamp)
    {
        _trainLocations.Add(new Tuple<double, double, double>(latitude, longitude, timestamp));
    }

    public Tuple<double, double> PredictNextLocation()
    {
        // if (_trainLocations.Count < 5)
        // {
            // throw new InvalidOperationException("Not enough train locations to make a prediction");
        // }

        // Extract the last five train locations
        var lastLocations = _trainLocations;
        var n = lastLocations.Count;
        var x = new double[n][];
        var y = new double[n];
        for (int i = 0; i < n; i++)
        {
            x[i] = new double[] { lastLocations[i].Item1, lastLocations[i].Item2, lastLocations[i].Item3 };
            y[i] = i == n - 1 ? 1 : 0; // Only predict the next location
        }

        // Train the linear regression model
        var regression = MultipleRegression.QR(x, y);

        // Predict the next location
        var lastLocation = lastLocations[n - 1];
        var nextTimestamp = lastLocation.Item3 + 1; // Predict next location at the next timestamp
        var nextLatitude = regression[0] * lastLocation.Item1 + regression[1] * lastLocation.Item2 + regression[2] * nextTimestamp;
        var nextLongitude = regression[3] * lastLocation.Item1 + regression[4] * lastLocation.Item2 + regression[5] * nextTimestamp;
        var nextLocation = new Tuple<double, double>(nextLatitude, nextLongitude);

        return nextLocation;
    }
}