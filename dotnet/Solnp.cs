using System;

namespace dotnet;

public struct SolveResult
{
    double SolveValue;
    double[] Optimum;
    bool Converged;
    double[,] HessianMatrix;
};

public static class Solnp
{
    /// <summary>
    /// Todo: migrate parameters and implementation from c library
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public static SolveResult solnp()
    {
        throw new NotImplementedException();
    }
}
