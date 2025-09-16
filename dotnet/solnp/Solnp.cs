using System;
using MathNet.Numerics.LinearAlgebra;

namespace SolnpLib
{
    public struct SolveResult
    {
        public double SolveValue;
        public Vector<double> Optimum;
        public bool Converged;
        public Matrix<double> HessianMatrix;
    };

    public static class Solnp
    {
        public static SolveResult solnp(
            Func<Vector<double>, Vector<double>> functor,
            Matrix<double> parameterData,
            Matrix<double> inequalityConstraintData,
            Matrix<double> hessianMatrix,
            double rho = 1.0,
            int maximumMajorIterations = 400,
            int maximumMinorIterations = 800,
            double delta = 1e-7,
            double tolerance = 1e-8)
        {
            if (double.IsInfinity(tolerance * tolerance))
            {
                throw new ArgumentException("Tolerance set too low.");
            }

            Vector<double> parameters;
            Matrix<double> parameterBounds = null;

            var numberOfParameters = parameterData.RowCount;
            var parameterVectorWidth = parameterData.ColumnCount;

            (bool, bool) lagrangianParametersBounded = (true, false);

            if (parameterVectorWidth == 1)
            {
                parameters = parameterData.Column(0);
                lagrangianParametersBounded = (false, false);
            }
            else if (parameterVectorWidth == 2)
            {
                parameters = 0.5 * (parameterData.Column(0) + parameterData.Column(1));
                parameterBounds = parameterData.SubMatrix(0, numberOfParameters, 0, 2);
            }
            else if (parameterVectorWidth == 3)
            {
                parameters = parameterData.Column(0);
                parameterBounds = parameterData.SubMatrix(0, numberOfParameters, 1, 2);
            }
            else
            {
                throw new ArgumentException("Parameter array must have three columns or less.");
            }

            if (lagrangianParametersBounded.Item1)
            {
                if ((parameterData.Column(2) - parameterData.Column(1)).Min() <= 0)
                {
                    throw new ArgumentException("The lower bounds of the parameter constraints must be strictly less than the upper bounds.");
                }
                if ((parameters - parameterData.Column(1)).Min() <= 0 || (parameterData.Column(2) - parameters).Min() <= 0)
                {
                    throw new ArgumentException("Initial parameter values must be within the bounds.");
                }
            }

            int inequalityConstraintsVectorLength = inequalityConstraintData.RowCount;
            int inequalityConstraintsVectorWidth = inequalityConstraintData.ColumnCount;
            int numberOfInequalityConstraints = 0;

            if (inequalityConstraintsVectorWidth > 0)
            {
                numberOfInequalityConstraints = inequalityConstraintsVectorLength;
                Vector<double> temporaryInequalityGuess = null;
                Matrix<double> temporaryInequalityConstraints = null;

                if (inequalityConstraintsVectorWidth == 3)
                {
                    temporaryInequalityGuess = inequalityConstraintData.Column(0);
                    temporaryInequalityConstraints = inequalityConstraintData.SubMatrix(0, numberOfInequalityConstraints, 1, 2);
                    if ((temporaryInequalityGuess - temporaryInequalityConstraints.Column(0)).Min() <= 0 ||
                        (temporaryInequalityConstraints.Column(1) - temporaryInequalityGuess).Min() <= 0)
                    {
                        throw new ArgumentException("Initial inequalities must be within bounds.");
                    }
                }
                else if (inequalityConstraintsVectorWidth == 2)
                {
                    if ((inequalityConstraintData.Column(1) - inequalityConstraintData.Column(0)).Min() <= 0)
                    {
                        throw new ArgumentException("The lower bounds of the inequality constraints must be strictly less than the upper bounds.");
                    }
                    temporaryInequalityGuess = 0.5 * (inequalityConstraintData.Column(0) + inequalityConstraintData.Column(1));
                    temporaryInequalityConstraints = inequalityConstraintData;
                }
                else if (inequalityConstraintsVectorWidth == 1)
                {
                    numberOfInequalityConstraints = 0;
                }
                else
                {
                    throw new ArgumentException("Inequality constraints must have 2 or 3 columns.");
                }

                if (numberOfInequalityConstraints > 0)
                {
                    if (lagrangianParametersBounded.Item1)
                    {
                        parameterBounds = temporaryInequalityConstraints.Stack(parameterBounds);
                    }
                    else
                    {
                        parameterBounds = temporaryInequalityConstraints;
                    }
                    parameters = Vector<double>.Build.Dense(temporaryInequalityGuess.Concat(parameters).ToArray());
                }
            }

            if (hessianMatrix.RowCount != numberOfParameters + numberOfInequalityConstraints ||
                hessianMatrix.ColumnCount != numberOfParameters + numberOfInequalityConstraints)
            {
                throw new ArgumentException("The provided hessian matrix override was of invalid dimension.");
            }

            if (lagrangianParametersBounded.Item1 || numberOfInequalityConstraints > 0)
            {
                lagrangianParametersBounded = (lagrangianParametersBounded.Item1, true);
            }

            var costVector = functor(parameters.SubVector(numberOfInequalityConstraints, numberOfParameters));

            if (costVector.Count < numberOfInequalityConstraints + 1)
            {
                throw new ArgumentException("The number of constraints in the cost function does not match the call to solnp.");
            }

            var numberOfEqualityConstraints = costVector.Count - 1 - numberOfInequalityConstraints;
            var numberOfConstraints = costVector.Count - 1;

            double objectiveFunctionValue = costVector[0];
            var objectiveFunctionValueHistory = new System.Collections.Generic.List<double> { objectiveFunctionValue };
            var t = Vector<double>.Build.Dense(3);

            Vector<double> lagrangianMultipliers;
            Vector<double> constraints;

            if (numberOfConstraints != 0)
            {
                lagrangianMultipliers = Vector<double>.Build.Dense(numberOfConstraints);
                constraints = costVector.SubVector(1, numberOfConstraints);

                if (numberOfInequalityConstraints != 0)
                {
                    var constrEquality = constraints.SubVector(numberOfEqualityConstraints, numberOfInequalityConstraints);
                    var paramBoundsInequality = parameterBounds.SubMatrix(0, numberOfInequalityConstraints, 0, 2);

                    if ((constrEquality - paramBoundsInequality.Column(0)).Min() > 0 &&
                        (paramBoundsInequality.Column(1) - constrEquality).Min() > 0)
                    {
                        parameters.SetSubVector(0, numberOfInequalityConstraints, constrEquality);
                    }
                    constraints.SetSubVector(numberOfEqualityConstraints, numberOfInequalityConstraints, constrEquality - parameters.SubVector(0, numberOfInequalityConstraints));
                }

                t[1] = Utils.EuclideanNorm(constraints);
                if (Math.Max(t[1] - 10.0 * tolerance, numberOfInequalityConstraints) <= 0)
                {
                    rho = 0.0;
                }
            }
            else
            {
                lagrangianMultipliers = Vector<double>.Build.Dense(1);
            }

            double mu = numberOfParameters;
            int iteration = 0;

            var subProblem = new Subnp(
                functor,
                numberOfParameters,
                numberOfEqualityConstraints,
                numberOfInequalityConstraints,
                lagrangianParametersBounded);

            while (iteration < maximumMajorIterations)
            {
                iteration++;

                subProblem.Optimize(
                    ref parameters,
                    parameterBounds,
                    ref lagrangianMultipliers,
                    costVector,
                    ref hessianMatrix,
                    ref mu,
                    rho,
                    maximumMinorIterations,
                    delta,
                    tolerance);

                costVector = functor(parameters.SubVector(numberOfInequalityConstraints, numberOfParameters));

                t[0] = (objectiveFunctionValue - costVector[0]) / Math.Max(Math.Abs(costVector[0]), 1.0);
                objectiveFunctionValue = costVector[0];

                if (numberOfConstraints != 0)
                {
                    constraints = costVector.SubVector(1, numberOfConstraints);

                    if (numberOfInequalityConstraints != 0)
                    {
                        var constrEquality = constraints.SubVector(numberOfEqualityConstraints, numberOfInequalityConstraints);
                        var paramBoundsInequality = parameterBounds.SubMatrix(0, numberOfInequalityConstraints, 0, 2);

                        if ((constrEquality - paramBoundsInequality.Column(0)).Min() > 0.0 &&
                            (paramBoundsInequality.Column(1) - constrEquality).Min() > 0.0)
                        {
                            parameters.SetSubVector(0, numberOfInequalityConstraints, constrEquality);
                        }
                        constraints.SetSubVector(numberOfEqualityConstraints, numberOfInequalityConstraints, constrEquality - parameters.SubVector(0, numberOfInequalityConstraints));
                    }
                    t[2] = Utils.EuclideanNorm(constraints);

                    if (t[2] < 10.0 * tolerance)
                    {
                        rho = 0.0;
                        mu = Math.Min(mu, tolerance);
                    }

                    if (t[2] < 5.0 * t[1])
                    {
                        rho /= 5.0;
                    }
                    else if (t[2] > 10.0 * t[1])
                    {
                        rho = 5.0 * Math.Max(rho, Math.Sqrt(tolerance));
                    }

                    if (Math.Max(tolerance + t[0], t[1] - t[2]) <= 0.0)
                    {
                        lagrangianMultipliers.Clear();
                        hessianMatrix = Matrix<double>.Build.DenseOfDiagonalVector(hessianMatrix.Diagonal());
                    }
                    t[1] = t[2];
                }

                if (Math.Sqrt(t[0] * t[0] + t[1] * t[1]) <= tolerance)
                {
                    maximumMajorIterations = iteration;
                }
                objectiveFunctionValueHistory.Add(objectiveFunctionValue);
            }

            var optimalParameters = parameters.SubVector(numberOfInequalityConstraints, numberOfParameters);
            bool converged = Math.Sqrt(t[0] * t[0] + t[1] * t[1]) <= tolerance;

            return new SolveResult
            {
                SolveValue = objectiveFunctionValue,
                Optimum = optimalParameters,
                Converged = converged,
                HessianMatrix = hessianMatrix
            };
        }
    }
}
