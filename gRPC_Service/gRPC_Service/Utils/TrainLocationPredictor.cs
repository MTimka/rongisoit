namespace gRPC_Service.Utils;

using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;


public class TrainLocationPredictor
{
    
    public static double[] PredictTrainLocationAtTimestamp(double[][] trainLocations, double timestamp)
    {
        // Create a Kalman filter to track the train's location
        var kf = new KalmanFilter(4, 2)
        {
            F = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { 1, 0, 1, 0 },
                { 0, 1, 0, 1 },
                { 0, 0, 1, 0 },
                { 0, 0, 0, 1 }
            }),
            H = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { 1, 0, 0, 0 },
                { 0, 1, 0, 0 }
            }),
            R = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { 10, 0 },
                { 0, 10 }
            }),
            Q = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { 0.0025, 0, 0.005, 0 },
                { 0, 0.0025, 0, 0.005 },
                { 0.005, 0, 0.01, 0 },
                { 0, 0.005, 0, 0.01 }
            }),
            x = Matrix<double>.Build.DenseOfArray(new double[,]
            {
                { trainLocations[trainLocations.Length - 1][0] }, 
                { trainLocations[trainLocations.Length - 1][1] }, 
                { 0 }, 
                { 0 }
            }),
            P = Matrix<double>.Build.DenseIdentity(4) * 500
        };

        // Predict the location of the train at the desired timestamp
        var ldt1 = DateTimeOffset.FromUnixTimeSeconds((long)trainLocations[trainLocations.Length - 1][2]);
        var ldt2 = DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
        
        var timeDelta = (ldt2 - ldt1).TotalSeconds;
        kf.Predict(timeDelta);

        Matrix<double> predictedLocationMatrix = kf.x.SubMatrix(0, 2, 0, 1);
        Vector<double> predictedLocationVector = predictedLocationMatrix.Column(0);
        double latitude = predictedLocationVector[0];
        double longitude = predictedLocationVector[1];
        
        return new[] {latitude, longitude };
    }
}

