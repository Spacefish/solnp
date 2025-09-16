using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace SolnpLib
{
    public static class Utils
    {
        /// <summary>
        /// Calculates the conditional number of a matrix.
        /// </summary>
        public static double ConditionalNumber(Matrix<double> matrix)
        {
            return matrix.ConditionNumber();
        }

        /// <summary>
        /// Calculates the Euclidean norm of a vector.
        /// </summary>
        public static double EuclideanNorm(Vector<double> vector)
        {
            return vector.L2Norm();
        }

        /// <summary>
        /// Calculates the infinity norm of a vector.
        /// </summary>
        public static double InfinityNorm(Vector<double> vector)
        {
            return vector.InfinityNorm();
        }

        /// <summary>
        /// Divides two matrices element-wise.
        /// </summary>
        public static Matrix<double> PointwiseDivide(Matrix<double> numerator, Matrix<double> denominator)
        {
            return numerator.PointwiseDivide(denominator);
        }

        /// <summary>
        /// Returns a new matrix with the element-wise maximum between the input matrix and a scalar.
        /// </summary>
        public static Matrix<double> ElementwiseMax(Matrix<double> matrix, double scalar)
        {
            return matrix.Map(x => Math.Max(x, scalar));
        }

        /// <summary>
        /// Returns a new matrix with the element-wise minimum between the input matrix and a scalar.
        /// </summary>
        public static Matrix<double> ElementwiseMin(Matrix<double> matrix, double scalar)
        {
            return matrix.Map(x => Math.Min(x, scalar));
        }

        /// <summary>
        /// For a 2-column matrix, ensures the left column contains the minimum and the right column contains the maximum of each row.
        /// </summary>
        public static Matrix<double> LeftVectorMinRightVectorMax(Matrix<double> matrix)
        {
            if (matrix.ColumnCount != 2)
            {
                throw new ArgumentException("Input matrix must have two columns.");
            }

            var result = Matrix<double>.Build.Dense(matrix.RowCount, 2);
            for (int i = 0; i < matrix.RowCount; i++)
            {
                var row = matrix.Row(i);
                result.SetRow(i, new double[] { Math.Min(row[0], row[1]), Math.Max(row[0], row[1]) });
            }
            return result;
        }

        /// <summary>
        /// Returns a vector containing the maximum value of each row in a matrix.
        /// </summary>
        public static Vector<double> RowwiseMax(Matrix<double> matrix)
        {
            var maxValues = new double[matrix.RowCount];
            for (int i = 0; i < matrix.RowCount; i++)
            {
                maxValues[i] = matrix.Row(i).Max();
            }
            return Vector<double>.Build.Dense(maxValues);
        }
    }
}
