using System;
using MathNet.Numerics.LinearAlgebra;
using SolnpLib;

// Objective function (Rosenbrock)
static Vector<double> Rosenbrock(Vector<double> x)
{
    double a = 1.0;
    double b = 100.0;
    double f = Math.Pow(a - x[0], 2) + b * Math.Pow(x[1] - x[0] * x[0], 2);
    return Vector<double>.Build.Dense(new[] { f });
}

// Initial parameters
var p = Matrix<double>.Build.DenseOfArray(new double[,] { { -2 }, { -2 } });

// No inequality constraints
var ib = Matrix<double>.Build.Dense(0, 0);

// Initial Hessian
var h = Matrix<double>.Build.DenseIdentity(p.RowCount);

// Call the solver
var result = Solnp.solnp(Rosenbrock, p, ib, h);

// Print the results
Console.WriteLine($"Converged: {result.Converged}");
Console.WriteLine($"Optimum: {result.Optimum}");
Console.WriteLine($"Solve Value: {result.SolveValue}");