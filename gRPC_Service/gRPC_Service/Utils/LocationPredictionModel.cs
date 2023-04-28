namespace gRPC_Service.Utils;

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearRegression;


public class LocationPredictionModel
{

    // List of previous train locations (at least 5)
    private List<TrainLocation> _previousLocations;

    public LocationPredictionModel(List<TrainLocation> previousLocations)
    {
        _previousLocations = previousLocations;
    }

    // Predict the next train location
    public TrainLocation PredictNextLocation(double timestamp)
    {
        // Create a matrix to hold the input data (latitude, longitude, timestamp)
        var inputMatrix = DenseMatrix.Build.Dense(_previousLocations.Count, 3);
        for (int i = 0; i < _previousLocations.Count; i++)
        {
            inputMatrix[i, 0] = _previousLocations[i].Latitude;
            inputMatrix[i, 1] = _previousLocations[i].Longitude;
            inputMatrix[i, 2] = _previousLocations[i].Timestamp;
        }

        // Create a vector to hold the output data (next latitude, next longitude, next timestamp)
        var outputVector = DenseVector.Build.Dense(_previousLocations.Count);
        for (int i = 0; i < _previousLocations.Count; i++)
        {
            outputVector[i] = i < _previousLocations.Count - 1 ? _previousLocations[i + 1].Latitude : 0;
        }

        // Use QR decomposition to solve the linear regression problem
        var qr = inputMatrix.QR();
        var beta = qr.Solve(outputVector);

        foreach (var it in beta)
        {
            Console.WriteLine(it);
        }

        // Predict the next location using the coefficients from the linear regression
        var lastLocation = _previousLocations[_previousLocations.Count - 1];
        // var nextTimestamp = lastLocation.Timestamp.AddSeconds(60); // predict one minute into the future
        var nextTimestamp = timestamp;  // predict one minute into the future
        var nextLatitude = beta[0] * lastLocation.Latitude + beta[1] * lastLocation.Longitude + beta[2] * nextTimestamp;
        var nextLongitude = beta[3] * lastLocation.Latitude + beta[4] * lastLocation.Longitude + beta[5] * nextTimestamp;

        return new TrainLocation()
        {
            Latitude = nextLatitude, 
            Longitude = nextLongitude, 
            Timestamp = nextTimestamp
        };
    }
}