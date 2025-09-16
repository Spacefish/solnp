using System;
using System.Runtime.InteropServices;
using Xunit;
using MathNet.Numerics.LinearAlgebra;
using SolnpLib;

namespace SolnpTests
{
    public class SolnpCrossImplementationTests
    {
        private const double Tolerance = 1e-6;

        // Test the simple quadratic function: f(x) = x^2
        [Fact]
        public void QuadraticFunction_CppAndCSharpProduceSameResults()
        {
            // C# Implementation
            Vector<double> QuadraticCs(Vector<double> x)
            {
                return Vector<double>.Build.Dense(new[] { x[0] * x[0] });
            }

            var initialParams = Matrix<double>.Build.DenseOfArray(new double[,] { { 1.0 } });
            var emptyBounds = Matrix<double>.Build.Dense(0, 0);
            var hessian = Matrix<double>.Build.DenseIdentity(1);

            var csResult = Solnp.solnp(QuadraticCs, initialParams, emptyBounds, hessian);

            // C++ Implementation (using P/Invoke)
            ObjectiveFunctionDelegate objectiveFunc = (IntPtr paramsPtr, int paramCount, IntPtr resultPtr) =>
            {
                double[] parameters = new double[paramCount];
                Marshal.Copy(paramsPtr, parameters, 0, paramCount);
                
                double result = parameters[0] * parameters[0];
                Marshal.Copy(new double[] { result }, 0, resultPtr, 1);
            };

            double[] initialParamsArray = { 1.0 };
            
            IntPtr cppResultPtr = CppSolnpInterop.solnp_solve(
                objectiveFunc, null, initialParamsArray, 1, null, null, 0,
                1.0, 400, 800, 1e-7, 1e-8);

            Assert.NotEqual(IntPtr.Zero, cppResultPtr);

            var cppResult = CppSolnpInterop.GetResult(cppResultPtr);
            var cppOptimum = CppSolnpInterop.GetOptimum(cppResult.optimum, cppResult.optimum_length);

            try
            {
                // Compare results
                Assert.Equal(csResult.Converged, cppResult.converged == 1);
                Assert.Equal(csResult.SolveValue, cppResult.solve_value, 5); // 5 decimal places
                Assert.Single(cppOptimum);
                Assert.Equal(csResult.Optimum[0], cppOptimum[0], 5);
            }
            finally
            {
                CppSolnpInterop.solnp_free_result(cppResultPtr);
            }
        }

        // Test the Box function from benchmark
        [Fact]
        public void BoxFunction_CppAndCSharpProduceSameResults()
        {
            // C# Implementation of Box function
            Vector<double> BoxCs(Vector<double> x)
            {
                double x1 = x[0], x2 = x[1], x3 = x[2];
                
                // Function value and equality constraint
                return Vector<double>.Build.Dense(new[] 
                { 
                    -1 * x1 * x2 * x3,  // Objective function
                    4 * x1 * x2 + 2 * x2 * x3 + 2 * x3 * x1 - 100  // Equality constraint
                });
            }

            // Initial parameters and bounds
            var initialParams = Matrix<double>.Build.DenseOfArray(new double[,] 
            { 
                { 1.0, 0.0, 10.0 }, 
                { 5.0, 0.0, 10.0 }, 
                { 5.0, 0.0, 10.0 } 
            });
            
            var emptyBounds = Matrix<double>.Build.Dense(0, 0);
            var hessian = Matrix<double>.Build.DenseIdentity(3);

            var csResult = Solnp.solnp(BoxCs, initialParams, emptyBounds, hessian);

            // C++ Implementation
            ObjectiveFunctionDelegate objectiveFunc = (IntPtr paramsPtr, int paramCount, IntPtr resultPtr) =>
            {
                double[] parameters = new double[paramCount];
                Marshal.Copy(paramsPtr, parameters, 0, paramCount);
                
                double x1 = parameters[0], x2 = parameters[1], x3 = parameters[2];
                double result = -1 * x1 * x2 * x3;
                Marshal.Copy(new double[] { result }, 0, resultPtr, 1);
            };

            ConstraintFunctionDelegate constraintFunc = (IntPtr paramsPtr, int paramCount, IntPtr constraintsPtr, int constraintCount) =>
            {
                double[] parameters = new double[paramCount];
                Marshal.Copy(paramsPtr, parameters, 0, paramCount);
                
                double x1 = parameters[0], x2 = parameters[1], x3 = parameters[2];
                double constraint = 4 * x1 * x2 + 2 * x2 * x3 + 2 * x3 * x1 - 100;
                Marshal.Copy(new double[] { constraint }, 0, constraintsPtr, 1);
            };

            double[] initialParamsArray = { 1.0, 5.0, 5.0 };
            double[] bounds = { 0.0, 0.0, 0.0, 10.0, 10.0, 10.0 };
            double[] constraintValues = { 0.0 };
            
            IntPtr cppResultPtr = CppSolnpInterop.solnp_solve(
                objectiveFunc, constraintFunc, initialParamsArray, 3, bounds, constraintValues, 1,
                1.0, 400, 800, 1e-7, 1e-8);

            Assert.NotEqual(IntPtr.Zero, cppResultPtr);

            var cppResult = CppSolnpInterop.GetResult(cppResultPtr);
            var cppOptimum = CppSolnpInterop.GetOptimum(cppResult.optimum, cppResult.optimum_length);

            try
            {
                // Compare results
                Assert.Equal(csResult.Converged, cppResult.converged == 1);
                
                // Allow for some numerical differences due to different implementations
                Assert.Equal(csResult.SolveValue, cppResult.solve_value, 3); // 3 decimal places
                
                Assert.Equal(3, cppOptimum.Length);
                for (int i = 0; i < 3; i++)
                {
                    Assert.Equal(csResult.Optimum[i], cppOptimum[i], 3);
                }
            }
            finally
            {
                CppSolnpInterop.solnp_free_result(cppResultPtr);
            }
        }

        // Test Rosenbrock function (same as in TestApp)
        [Fact]
        public void RosenbrockFunction_CppAndCSharpProduceSameResults()
        {
            // C# Implementation
            Vector<double> RosenbrockCs(Vector<double> x)
            {
                double a = 1.0;
                double b = 100.0;
                double f = Math.Pow(a - x[0], 2) + b * Math.Pow(x[1] - x[0] * x[0], 2);
                return Vector<double>.Build.Dense(new[] { f });
            }

            var initialParams = Matrix<double>.Build.DenseOfArray(new double[,] { { -2 }, { -2 } });
            var emptyBounds = Matrix<double>.Build.Dense(0, 0);
            var hessian = Matrix<double>.Build.DenseIdentity(2);

            var csResult = Solnp.solnp(RosenbrockCs, initialParams, emptyBounds, hessian);

            // C++ Implementation
            ObjectiveFunctionDelegate objectiveFunc = (IntPtr paramsPtr, int paramCount, IntPtr resultPtr) =>
            {
                double[] parameters = new double[paramCount];
                Marshal.Copy(paramsPtr, parameters, 0, paramCount);
                
                double a = 1.0;
                double b = 100.0;
                double f = Math.Pow(a - parameters[0], 2) + b * Math.Pow(parameters[1] - parameters[0] * parameters[0], 2);
                Marshal.Copy(new double[] { f }, 0, resultPtr, 1);
            };

            double[] initialParamsArray = { -2.0, -2.0 };
            
            IntPtr cppResultPtr = CppSolnpInterop.solnp_solve(
                objectiveFunc, null, initialParamsArray, 2, null, null, 0,
                1.0, 400, 800, 1e-7, 1e-8);

            Assert.NotEqual(IntPtr.Zero, cppResultPtr);

            var cppResult = CppSolnpInterop.GetResult(cppResultPtr);
            var cppOptimum = CppSolnpInterop.GetOptimum(cppResult.optimum, cppResult.optimum_length);

            try
            {
                // Compare results - Rosenbrock is known to have optimum at (1,1)
                Assert.Equal(csResult.Converged, cppResult.converged == 1);
                Assert.Equal(csResult.SolveValue, cppResult.solve_value, 2); // 2 decimal places - both very close to 0
                
                Assert.Equal(2, cppOptimum.Length);
                
                // For Rosenbrock, both should converge close to (1,1), but small differences are acceptable
                // Verify both converged to approximately (1, 1)
                Assert.Equal(1.0, csResult.Optimum[0], 2);
                Assert.Equal(1.0, csResult.Optimum[1], 2);
                Assert.Equal(1.0, cppOptimum[0], 2);
                Assert.Equal(1.0, cppOptimum[1], 2);
                
                // Also verify the difference between implementations is small
                for (int i = 0; i < 2; i++)
                {
                    Assert.True(Math.Abs(csResult.Optimum[i] - cppOptimum[i]) < 0.01, 
                        $"Parameter {i}: C# result {csResult.Optimum[i]}, C++ result {cppOptimum[i]}, difference too large");
                }
            }
            finally
            {
                CppSolnpInterop.solnp_free_result(cppResultPtr);
            }
        }

        // Test a quadratic fit for a set of points in a two-dimensional space.
        [Theory]
        [InlineData(new double[] { -1, 2, 0, 1, 1, 2, 2, 5 })] // y = x^2 + 1
        [InlineData(new double[] { -2, 4, -1, 1, 1, 1, 2, 4 })] // y = x^2
        [InlineData(new double[] { -1, -2, 0, -1, 1, 4, 2, 13 })] // y = 2x^2 + 3x - 1
        [InlineData(new double[] { -1, -2, 0, 0, 1, 0, 2, -2 })] // y = -x^2 + x
        [InlineData(new double[] { -1, 1.1, 0, 0.1, 1, 1.2, 2, 4.3 })] // Noisy data around y = x^2
        [InlineData(new double[] { 0, 0, 1, 1, 2, 2, 3, 3 })] // Collinear points
        [InlineData(new double[] { 0, 1, 1, 3, 2, 5, 3, 7 })] // Collinear points
        public void QuadraticFit_CppAndCSharpProduceSameResults(double[] points)
        {
            // The quadratic function is y = ax^2 + bx + c
            // The parameters to optimize are a, b, c.
            // The objective is to minimize the sum of squared errors.

            // C# Implementation
            Vector<double> QuadraticFitCs(Vector<double> p)
            {
                double a = p[0], b = p[1], c = p[2];
                double err = 0.0;
                for (int i = 0; i < points.Length; i += 2)
                {
                    double x = points[i];
                    double y = points[i + 1];
                    err += Math.Pow(y - (a * x * x + b * x + c), 2);
                }
                return Vector<double>.Build.Dense(new[] { err });
            }

            var initialParams = Matrix<double>.Build.DenseOfArray(new double[,] { { 0 }, { 0 }, { 0 } });
            var emptyBounds = Matrix<double>.Build.Dense(0, 0);
            var hessian = Matrix<double>.Build.DenseIdentity(3);

            var csResult = Solnp.solnp(QuadraticFitCs, initialParams, emptyBounds, hessian);

            // C++ Implementation
            ObjectiveFunctionDelegate objectiveFunc = (IntPtr paramsPtr, int paramCount, IntPtr resultPtr) =>
            {
                double[] p = new double[paramCount];
                Marshal.Copy(paramsPtr, p, 0, paramCount);

                double a = p[0], b = p[1], c = p[2];
                double err = 0.0;
                for (int i = 0; i < points.Length; i += 2)
                {
                    double x = points[i];
                    double y = points[i + 1];
                    err += Math.Pow(y - (a * x * x + b * x + c), 2);
                }
                Marshal.Copy(new double[] { err }, 0, resultPtr, 1);
            };

            double[] initialParamsArray = { 0.0, 0.0, 0.0 };

            IntPtr cppResultPtr = CppSolnpInterop.solnp_solve(
                objectiveFunc, null, initialParamsArray, 3, null, null, 0,
                1.0, 400, 800, 1e-7, 1e-8);

            Assert.NotEqual(IntPtr.Zero, cppResultPtr);

            var cppResult = CppSolnpInterop.GetResult(cppResultPtr);
            var cppOptimum = CppSolnpInterop.GetOptimum(cppResult.optimum, cppResult.optimum_length);

            try
            {
                // Check for collinearity
                bool isCollinear = false;
                if (points.Length >= 6)
                {
                    double x1 = points[0], y1 = points[1];
                    double x2 = points[2], y2 = points[3];

                    if (Math.Abs(x1 - x2) < 1e-9) // vertical line
                    {
                        isCollinear = true;
                        for (int i = 4; i < points.Length; i+=2)
                        {
                            if (Math.Abs(points[i] - x1) > 1e-9)
                            {
                                isCollinear = false;
                                break;
                            }
                        }
                    }
                    else
                    {
                        double slope = (y2 - y1) / (x2 - x1);
                        isCollinear = true;
                        for (int i = 4; i < points.Length; i += 2)
                        {
                            double xi = points[i];
                            double yi = points[i + 1];
                            if (Math.Abs((yi - y1) - slope * (xi - x1)) > 1e-6)
                            {
                                isCollinear = false;
                                break;
                            }
                        }
                    }
                }

                if (isCollinear)
                {
                    // For collinear points, 'a' should be close to 0
                    Assert.Equal(0, csResult.Optimum[0], 3);
                    Assert.Equal(0, cppOptimum[0], 3);
                }
                else
                {
                    // Compare results for non-collinear points
                    Assert.Equal(csResult.Converged, cppResult.converged == 1);
                    Assert.Equal(csResult.SolveValue, cppResult.solve_value, 3);
                    Assert.Equal(3, cppOptimum.Length);
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.Equal(csResult.Optimum[i], cppOptimum[i], 3);
                    }
                }
            }
            finally
            {
                CppSolnpInterop.solnp_free_result(cppResultPtr);
            }
        }
    }
}