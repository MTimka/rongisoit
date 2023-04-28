namespace gRPC_Service.Utils;

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;

public class TrainLocationPredictor
{
    
    // Predict the train location at a given timestamp
    public Tuple<double, double, double> PredictLocation(double timestamp, List<Tuple<double, double, double>> trainLocations)
    {
        var A = CreateTransitionMatrix();
        var B = CreateControlMatrix();
        var H = CreateObservationMatrix();
        var Q = CreateProcessNoiseCovarianceMatrix();
        var R = CreateMeasurementNoiseCovarianceMatrix();
        
        // Initialize the state vector with the latest train location
        var latestLocation = trainLocations.Last();
        var state = Vector<double>.Build.DenseOfArray(new double[] { latestLocation.Item1, latestLocation.Item2, latestLocation.Item3 });

        // Initialize the error covariance matrix
        var P = Matrix<double>.Build.DenseIdentity(3);

        // Loop through the train location data and perform Kalman filtering
        foreach (var location in trainLocations)
        {
            // Calculate the time elapsed between the previous location and the current location
            var dt = location.Item3 - state[2];

            // Update the state transition matrix to account for the elapsed time
            A[0, 2] = dt;
            A[1, 2] = dt;

            // Predict the next state using the Kalman filter equations
            state = (A * state) + (B * Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 0 }));
            P = (A * P * A.Transpose()) + Q;
            var y = Vector<double>.Build.DenseOfArray(new double[] { location.Item1, location.Item2, location.Item3 }) - (H * state);
            var S = (H * P * H.Transpose()) + R;
            var K = P * H.Transpose() * S.Inverse();
            state = state + (K * y);
            P = (Matrix<double>.Build.DenseIdentity(3) - (K * H)) * P;
        }

        // Predict the location at the specified timestamp
        var dt2 = timestamp - state[2];
        A[0, 2] = dt2;
        A[1, 2] = dt2;
        state = (A * state) + (B * Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 0 }));
        return Tuple.Create(state[0], state[1], timestamp);
    }

    // Helper function to create the state transition matrix
    private Matrix<double> CreateTransitionMatrix()
    {
        var A = Matrix<double>.Build.DenseIdentity(3);
        A[0, 2] = 1; // update latitude with velocity
        A[1, 2] = 1; // update longitude with velocity
        return A;
    }

// Helper function to create the control matrix
    private Matrix<double> CreateControlMatrix()
    {
        return Matrix<double>.Build.DenseIdentity(3, 1);
    }

// Helper function to create the observation matrix
    private Matrix<double> CreateObservationMatrix()
    {
        return Matrix<double>.Build.DenseIdentity(3);
    }

// Helper function to create the process noise covariance matrix
    private Matrix<double> CreateProcessNoiseCovarianceMatrix()
    {
        var Q = Matrix<double>.Build.DenseIdentity(3);
        Q[0, 0] = 0.001; // process noise for latitude
        Q[1, 1] = 0.001; // process noise for longitude
        Q[2, 2] = 0.01; // process noise for timestamp
        return Q;
    }

// Helper function to create the measurement noise covariance matrix
    private Matrix<double> CreateMeasurementNoiseCovarianceMatrix()
    {
        var R = Matrix<double>.Build.DenseIdentity(3);
        R[0, 0] = 0.01; // measurement noise for latitude
        R[1, 1] = 0.01; // measurement noise for longitude
        R[2, 2] = 1; // measurement noise for timestamp
        return R;
    }
}

