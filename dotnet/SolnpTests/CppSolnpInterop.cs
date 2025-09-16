using System;
using System.Runtime.InteropServices;

namespace SolnpTests
{
    // Structure matching the C struct
    [StructLayout(LayoutKind.Sequential)]
    public struct SolveResultC
    {
        public double solve_value;
        public IntPtr optimum;
        public int optimum_length;
        public int converged;
    }

    // Delegate types for callbacks
    public delegate void ObjectiveFunctionDelegate(IntPtr parameters, int paramCount, IntPtr result);
    public delegate void ConstraintFunctionDelegate(IntPtr parameters, int paramCount, IntPtr constraints, int constraintCount);

    public static class CppSolnpInterop
    {
        private const string LibraryName = "./libsolnp_native.so";

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr solnp_solve(
            ObjectiveFunctionDelegate objectiveFunc,
            ConstraintFunctionDelegate constraintFunc,
            double[] initialParameters,
            int paramCount,
            double[] parameterBounds,
            double[] constraintValues,
            int constraintCount,
            double rho,
            int maxMajorIterations,
            int maxMinorIterations,
            double delta,
            double tolerance);

        [DllImport(LibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void solnp_free_result(IntPtr result);

        public static SolveResultC GetResult(IntPtr resultPtr)
        {
            return Marshal.PtrToStructure<SolveResultC>(resultPtr);
        }

        public static double[] GetOptimum(IntPtr optimumPtr, int length)
        {
            double[] result = new double[length];
            Marshal.Copy(optimumPtr, result, 0, length);
            return result;
        }
    }
}