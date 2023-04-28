
namespace gRPC_Service.Utils;

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.LinearRegression;
using MathNet.Numerics;


public class LocationPredictionModel
{

    // List of previous train locations (at least 5)
    private List<TrainLocation> _previousLocations;

    public LocationPredictionModel(List<TrainLocation> previousLocations)
    {
        _previousLocations = previousLocations;
    }

    public (double, double) PredictNextLocation(double timestamp)
    {
        // Extract the timestamps and convert them to seconds
        double[] timestamps = _previousLocations.Select(l => (l.Timestamp - _previousLocations[0].Timestamp)).ToArray();

        // Extract the latitudes and longitudes
        double[] latitudes = _previousLocations.Select(l => l.Latitude).ToArray();
        double[] longitudes = _previousLocations.Select(l => l.Longitude).ToArray();

        // Perform linear regression on the latitudes and longitudes separately
        var latitudeCoeffs = Fit.Line(timestamps, latitudes);
        var longitudeCoeffs = Fit.Line(timestamps, longitudes);

        // Predict the location at the next timestamp (10:50:00)
        // double nextTimestamp = (_previousLocations.Last().Timestamp - _previousLocations[0].Timestamp) + 600; // 600 seconds = 10 minutes
        double nextTimestamp = timestamp;
        double nextLatitude = latitudeCoeffs.Item1 * nextTimestamp + latitudeCoeffs.Item2;
        double nextLongitude = longitudeCoeffs.Item1 * nextTimestamp + longitudeCoeffs.Item2;

        return (nextLatitude, nextLongitude);
    }
    
    // Predict the next train location
    // public TrainLocation PredictNextLocation(double timestamp)
    // {
    //     // Create a matrix to hold the input data (latitude, longitude, timestamp)
    //     var inputMatrix = DenseMatrix.Build.Dense(_previousLocations.Count, 3);
    //     for (int i = 0; i < _previousLocations.Count; i++)
    //     {
    //         inputMatrix[i, 0] = _previousLocations[i].Latitude;
    //         inputMatrix[i, 1] = _previousLocations[i].Longitude;
    //         inputMatrix[i, 2] = _previousLocations[i].Timestamp;
    //     }
    //
    //     // Create a matrix to hold the output data (next latitude, next longitude)
    //     var outputMatrix = DenseMatrix.Build.Dense(_previousLocations.Count, 2);
    //     for (int i = 0; i < _previousLocations.Count; i++)
    //     {
    //         outputMatrix[i, 0] = i < _previousLocations.Count - 1 ? _previousLocations[i + 1].Latitude : 0;
    //         outputMatrix[i, 1] = i < _previousLocations.Count - 1 ? _previousLocations[i + 1].Longitude : 0;
    //     }
    //
    //     // Use QR decomposition to solve the linear regression problem
    //     var qr = inputMatrix.QR();
    //     var beta = qr.Solve(outputMatrix);
    //
    //     // for (int i = 0; i < beta.RowCount; i++)
    //     // {
    //     //     for (int j = 0; j < beta.ColumnCount; j++)
    //     //     {
    //     //         Console.Write(beta[i, j] + " ");
    //     //     }
    //     //     Console.WriteLine();
    //     // }
    //
    //     // Predict the next location using the coefficients from the linear regression
    //     var lastLocation = _previousLocations[_previousLocations.Count - 1];
    //     // var nextTimestamp = lastLocation.Timestamp.AddSeconds(60); // predict one minute into the future
    //     var nextTimestamp = timestamp;  // predict one minute into the future
    //     var nextLatitude = beta[0, 0] * lastLocation.Latitude + beta[1, 0] * lastLocation.Longitude + beta[2, 0] * nextTimestamp;
    //     var nextLongitude = beta[0, 1] * lastLocation.Latitude + beta[1, 1] * lastLocation.Longitude + beta[2, 1] * nextTimestamp;
    //
    //     return new TrainLocation()
    //     {
    //         Latitude = nextLatitude, 
    //         Longitude = nextLongitude, 
    //         Timestamp = nextTimestamp
    //     };
    // }
}