namespace gRPC_Service.Utils;

using MathNet.Numerics.LinearAlgebra;

public class KalmanFilter
{
    public Matrix<double> F { get; set; }
    public Matrix<double> H { get; set; }
    public Matrix<double> Q { get; set; }
    public Matrix<double> R { get; set; }
    public Matrix<double> P { get; set; }
    public Matrix<double> x { get; set; }

    private Matrix<double> I;

    public KalmanFilter(int dim_x, int dim_z)
    {
        F = Matrix<double>.Build.DenseIdentity(dim_x);
        H = Matrix<double>.Build.DenseIdentity(dim_z, dim_x);
        Q = Matrix<double>.Build.DenseIdentity(dim_x);
        R = Matrix<double>.Build.DenseIdentity(dim_z);
        P = Matrix<double>.Build.DenseIdentity(dim_x);
        x = Matrix<double>.Build.Dense(dim_x, 1);
        I = Matrix<double>.Build.DenseIdentity(dim_x);
    }

    public void Predict(double dt)
    {
        F[0, 2] = dt;
        F[1, 3] = dt;

        x = F * x;
        P = F * P * F.Transpose() + Q;
    }

    public void Update(Matrix<double> z)
    {
        var y = z - H * x;
        var S = H * P * H.Transpose() + R;
        var K = P * H.Transpose() * S.Inverse();

        x = x + K * y;
        P = (I - K * H) * P;
    }
}