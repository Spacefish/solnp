using MathNet.Numerics.LinearAlgebra;
using System;
using System.Linq;

namespace SolnpLib
{
    public class Subnp
    {
        private readonly Func<Vector<double>, Vector<double>> _objectiveFunction;
        private readonly int _numberOfParameters;
        private readonly int _numberOfEqualityConstraints;
        private readonly int _numberOfInequalityConstraints;
        private readonly int _numberOfTotalConstraints;
        private readonly int _numberOfParametersAndInequalityConstraints;
        private readonly (bool, bool) _lagrangianParametersBounded;

        private Vector<double> _alpha;

        public Subnp(
            Func<Vector<double>, Vector<double>> objectiveFunction,
            int numberOfParameters,
            int numberOfEqualityConstraints,
            int numberOfInequalityConstraints,
            (bool, bool) lagrangianParametersBounded)
        {
            _objectiveFunction = objectiveFunction;
            _numberOfParameters = numberOfParameters;
            _numberOfEqualityConstraints = numberOfEqualityConstraints;
            _numberOfInequalityConstraints = numberOfInequalityConstraints;
            _numberOfTotalConstraints = numberOfEqualityConstraints + numberOfInequalityConstraints;
            _numberOfParametersAndInequalityConstraints = numberOfParameters + numberOfInequalityConstraints;
            _lagrangianParametersBounded = lagrangianParametersBounded;

            _alpha = Vector<double>.Build.Dense(3);
        }

        public void Optimize(
            ref Vector<double> parameter,
            Matrix<double> parameterBounds,
            ref Vector<double> lagrangianMultipliers,
            Vector<double> costVector,
            ref Matrix<double> hessian,
            ref double mu,
            double rho,
            int maxIterations,
            double delta,
            double tolerance)
        {
            _alpha.Clear();
            bool positiveChange = true;

            var parameter0 = parameter.Clone();
            var lagrangianMultipliers0 = lagrangianMultipliers.Clone();

            var scale = Vector<double>.Build.Dense(1 + _numberOfEqualityConstraints);
            if (_numberOfEqualityConstraints != 0)
            {
                var costVectorEquality = costVector.SubVector(1, _numberOfEqualityConstraints);
                var infinityNorm = Utils.InfinityNorm(costVectorEquality);
                var ones = Vector<double>.Build.Dense(_numberOfEqualityConstraints, 1.0);
                scale = Vector<double>.Build.Dense(new[] { costVector[0] }.Concat(ones.Multiply(infinityNorm)).ToArray());
            }
            else
            {
                scale = Vector<double>.Build.Dense(new[] { 1.0 });
            }

            if (!_lagrangianParametersBounded.Item2)
            {
                scale = Vector<double>.Build.Dense(scale.Concat(parameter0).ToArray());
            }
            else
            {
                var ones = Vector<double>.Build.Dense(parameter0.Count, 1.0);
                scale = Vector<double>.Build.Dense(scale.Concat(ones).ToArray());
            }

            scale = scale.PointwiseMaximum(tolerance).PointwiseMinimum(1.0 / tolerance);

            costVector = costVector.PointwiseDivide(scale.SubVector(0, _numberOfTotalConstraints + 1));
            parameter0 = parameter0.PointwiseDivide(scale.SubVector(_numberOfEqualityConstraints + 1, _numberOfTotalConstraints + _numberOfParameters + 1 - (_numberOfEqualityConstraints + 1)));

            int mm = 0;
            if (_lagrangianParametersBounded.Item2)
            {
                mm = _lagrangianParametersBounded.Item1 ? _numberOfParametersAndInequalityConstraints : _numberOfInequalityConstraints;
                var paramBoundsScale = scale.SubVector(_numberOfEqualityConstraints + 1, mm).ToRowMatrix().Transpose().Append(scale.SubVector(_numberOfEqualityConstraints + 1, mm).ToRowMatrix().Transpose());
                parameterBounds = parameterBounds.PointwiseDivide(paramBoundsScale);
            }

            if (_numberOfTotalConstraints != 0)
            {
                lagrangianMultipliers0 = (scale.SubVector(1, _numberOfTotalConstraints).PointwiseMultiply(lagrangianMultipliers0)).Divide(scale[0]);
            }

            var scaleSub = scale.SubVector(_numberOfEqualityConstraints + 1, _numberOfTotalConstraints + _numberOfParameters - _numberOfEqualityConstraints);
            hessian = hessian.PointwiseMultiply(scaleSub.ToRowMatrix().Transpose() * scaleSub.ToRowMatrix()) / scale[0];

            double objectFunctionValue = costVector[0];
            var a = Matrix<double>.Build.Dense(_numberOfEqualityConstraints + _numberOfInequalityConstraints, _numberOfInequalityConstraints + _numberOfParameters);

            if (_numberOfInequalityConstraints > 0)
            {
                var identity = Matrix<double>.Build.DenseIdentity(_numberOfInequalityConstraints);
                a.SetSubMatrix(0, _numberOfEqualityConstraints, Matrix<double>.Build.Dense(_numberOfEqualityConstraints, _numberOfInequalityConstraints));
                a.SetSubMatrix(_numberOfEqualityConstraints, 0, -1 * identity);
            }

            var gradient = Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints);
            var b = Vector<double>.Build.Dense(_numberOfTotalConstraints);
            var constraints = Vector<double>.Build.Dense(_numberOfTotalConstraints);

            if (_numberOfTotalConstraints != 0)
            {
                constraints = costVector.SubVector(1, _numberOfTotalConstraints);

                for (int i = 0; i < _numberOfParameters; i++)
                {
                    parameter0[_numberOfInequalityConstraints + i] += delta;

                    var scaledParams = parameter0.SubVector(_numberOfInequalityConstraints, _numberOfParameters)
                        .PointwiseMultiply(scale.SubVector(_numberOfTotalConstraints + 1, _numberOfParameters));

                    var newCostVector = _objectiveFunction(scaledParams)
                        .PointwiseDivide(scale.SubVector(0, _numberOfTotalConstraints + 1));

                    gradient[_numberOfInequalityConstraints + i] = (newCostVector[0] - objectFunctionValue) / delta;

                    a.SetColumn(_numberOfInequalityConstraints + i, (newCostVector.SubVector(1, _numberOfTotalConstraints) - constraints) / delta);

                    parameter0[_numberOfInequalityConstraints + i] -= delta;
                }

                if (_numberOfInequalityConstraints > 0)
                {
                    var constrEquality = constraints.SubVector(_numberOfEqualityConstraints, _numberOfInequalityConstraints);
                    var paramInequality = parameter0.SubVector(0, _numberOfInequalityConstraints);
                    constraints.SetSubVector(_numberOfEqualityConstraints, _numberOfInequalityConstraints, constrEquality - paramInequality);
                }

                if (Utils.ConditionalNumber(a) > 1 / double.Epsilon)
                {
                    // Log warning: Redundant constraints
                }

                b = a * parameter0 - constraints;
            }

            var c = Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints + 1);
            var dx = Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints + 1);
            double go;
            int minorIteration = 0;

            if (_numberOfTotalConstraints != 0)
            {
                positiveChange = false;
                _alpha[0] = tolerance - constraints.PointwiseAbs().Max();

                if (_alpha[0] <= 0)
                {
                    positiveChange = true;
                    if (!_lagrangianParametersBounded.Item2)
                    {
                        var qr = (a * a.Transpose()).QR();
                        if (qr.R.Diagonal().Exists(v => v == 0))
                        {
                            throw new InvalidOperationException("Encountered Singular matrix when trying to solve.");
                        }
                        parameter0 -= a.Transpose() * qr.Solve(constraints);
                        _alpha[0] = 1;
                    }
                }

                if (_alpha[0] <= 0)
                {
                    parameter0 = Vector<double>.Build.Dense(parameter0.Concat(new[] { 1.0 }).ToArray());
                    a = a.InsertColumn(a.ColumnCount, -1 * constraints);
                    c = Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints + 1);
                    c[_numberOfParametersAndInequalityConstraints] = 1.0;
                    dx = Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints + 1, 1.0);
                    go = 1.0;
                    minorIteration = 0;

                    while (go >= tolerance)
                    {
                        minorIteration++;
                        var gap = (parameter0.SubVector(0, mm) - parameterBounds.Column(0)).ToColumnMatrix()
                            .Append((parameterBounds.Column(1) - parameter0.SubVector(0, mm)).ToColumnMatrix());

                        var minGap = Utils.LeftVectorMinRightVectorMax(gap);
                        dx.SetSubVector(0, mm, minGap.Column(0));
                        dx[_numberOfParametersAndInequalityConstraints] = parameter0[_numberOfParametersAndInequalityConstraints];

                        if (!_lagrangianParametersBounded.Item1)
                        {
                            var maxDx = dx.SubVector(0, mm).Max();
                            dx.SetSubVector(mm, _numberOfParametersAndInequalityConstraints - mm, Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints - mm, Math.Max(maxDx, 100.0)));
                        }

                        var qr = (a * Matrix<double>.Build.DenseOfDiagonalVector(dx)).Transpose().QR();
                        lagrangianMultipliers = qr.Solve(dx.PointwiseMultiply(c));

                        var tempVector = dx.PointwiseMultiply(dx.PointwiseMultiply(c - a.Transpose() * lagrangianMultipliers));

                        if (tempVector[_numberOfParametersAndInequalityConstraints] > 0)
                        {
                            double tempScalar = parameter0[_numberOfParametersAndInequalityConstraints] / tempVector[_numberOfParametersAndInequalityConstraints];

                            for (int k = 0; k < mm; k++)
                            {
                                if (tempVector[k] < 0)
                                {
                                    tempScalar = Math.Min(tempScalar, -1 * (parameterBounds[k, 1] - parameter0[k]) / tempVector[k]);
                                }
                                else if (tempVector[k] > 0)
                                {
                                    tempScalar = Math.Min(tempScalar, (parameter0[k] - parameterBounds[k, 0]) / tempVector[k]);
                                }
                            }

                            if (tempScalar >= parameter0[_numberOfParametersAndInequalityConstraints] / tempVector[_numberOfParametersAndInequalityConstraints])
                            {
                                parameter0 -= tempScalar * tempVector;
                            }
                            else
                            {
                                parameter0 -= 0.9 * tempScalar * tempVector;
                            }

                            go = parameter0[_numberOfParametersAndInequalityConstraints];
                            if (minorIteration >= 10)
                            {
                                go = 0.0;
                            }
                        }
                        else
                        {
                            go = 0.0;
                            minorIteration = 10;
                        }
                    }

                    if (minorIteration >= 10)
                    {
                       // Log warning
                    }
                    a = a.SubMatrix(0, a.RowCount, 0, _numberOfParametersAndInequalityConstraints);
                    b = a * parameter0.SubVector(0, _numberOfParametersAndInequalityConstraints);
                }
            }

            parameter = parameter0.SubVector(0, _numberOfParametersAndInequalityConstraints);
            lagrangianMultipliers.Clear();

            if (positiveChange)
            {
                var scaledFuncValues = parameter.SubVector(_numberOfInequalityConstraints, _numberOfParameters)
                    .PointwiseMultiply(scale.SubVector(_numberOfTotalConstraints + 1, _numberOfParameters));

                costVector = _objectiveFunction(scaledFuncValues).PointwiseDivide(scale.SubVector(0, _numberOfTotalConstraints + 1));
            }

            objectFunctionValue = costVector[0];

            if (_numberOfInequalityConstraints > 0)
            {
                var costVectorInequality = costVector.SubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints);
                var paramInequality = parameter.SubVector(0, _numberOfInequalityConstraints);
                costVector.SetSubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints, costVectorInequality - paramInequality);
            }

            if (_numberOfTotalConstraints != 0)
            {
                var costVectorSub = costVector.SubVector(1, _numberOfTotalConstraints);
                costVector.SetSubVector(1, _numberOfTotalConstraints, costVectorSub - a * parameter + b);

                objectFunctionValue = costVector[0] - lagrangianMultipliers0.DotProduct(costVector.SubVector(1, _numberOfTotalConstraints)) +
                                      rho * Math.Pow(Utils.EuclideanNorm(costVector.SubVector(1, _numberOfTotalConstraints)), 2);
            }

            minorIteration = 0;
            Vector<double> tempGradient = null;
            Vector<double> tempParameter = null;
            double reduction = 0.0;

            while(minorIteration < maxIterations)
            {
                minorIteration++;
                if (positiveChange)
                {
                    for (int i = 0; i < _numberOfParameters; i++)
                    {
                        parameter[_numberOfInequalityConstraints + i] += delta;

                        var scaledParams = parameter.SubVector(_numberOfInequalityConstraints, _numberOfParameters)
                            .PointwiseMultiply(scale.SubVector(_numberOfTotalConstraints + 1, _numberOfParameters));

                        var modifiedCostVector = _objectiveFunction(scaledParams).PointwiseDivide(scale.SubVector(0, _numberOfTotalConstraints + 1));

                        if (_numberOfInequalityConstraints > 0)
                        {
                            var modCostVectorInequality = modifiedCostVector.SubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints);
                            var paramInequality = parameter.SubVector(0, _numberOfInequalityConstraints);
                            modifiedCostVector.SetSubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints, modCostVectorInequality - paramInequality);
                        }

                        double modifiedObjectFunctionValue;
                        if (_numberOfTotalConstraints != 0)
                        {
                            var modCostVectorSub = modifiedCostVector.SubVector(1, _numberOfTotalConstraints);
                            modifiedCostVector.SetSubVector(1, _numberOfTotalConstraints, modCostVectorSub - a * parameter + b);
                            modifiedObjectFunctionValue = modifiedCostVector[0] - lagrangianMultipliers0.DotProduct(modifiedCostVector.SubVector(1, _numberOfTotalConstraints)) +
                                                          rho * Math.Pow(Utils.EuclideanNorm(modifiedCostVector.SubVector(1, _numberOfTotalConstraints)), 2);
                        }
                        else
                        {
                            modifiedObjectFunctionValue = modifiedCostVector[0];
                        }

                        gradient[_numberOfInequalityConstraints + i] = (modifiedObjectFunctionValue - objectFunctionValue) / delta;
                        parameter[_numberOfInequalityConstraints + i] -= delta;
                    }

                    if (_numberOfInequalityConstraints > 0)
                    {
                        gradient.SetSubVector(0, _numberOfInequalityConstraints, Vector<double>.Build.Dense(_numberOfInequalityConstraints));
                    }
                }

                if (minorIteration > 1)
                {
                    var gradDiff = gradient - tempGradient;
                    var paramDiff = parameter - tempParameter;

                    double sc0 = paramDiff.DotProduct(hessian * paramDiff);
                    double sc1 = paramDiff.DotProduct(gradDiff);

                    if (sc0 * sc1 > 0)
                    {
                        var hessianUpdate = hessian * paramDiff;
                        hessian = hessian - (hessianUpdate.ToColumnMatrix() * hessianUpdate.ToRowMatrix()) / sc0 + (gradDiff.ToColumnMatrix() * gradDiff.ToRowMatrix()) / sc1;
                    }
                }

                dx = Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints, 0.01);

                if (_lagrangianParametersBounded.Item2)
                {
                    var gap = parameter.SubVector(0, mm).ToColumnMatrix() - parameterBounds.Column(0).ToColumnMatrix();
                    gap = gap.Append(parameterBounds.Column(1).ToColumnMatrix() - parameter.SubVector(0, mm).ToColumnMatrix());

                    var minGap = Utils.LeftVectorMinRightVectorMax(gap).Column(0) + Math.Sqrt(double.Epsilon);
                    dx.SetSubVector(0, mm, Vector<double>.Build.Dense(mm, 1.0).PointwiseDivide(minGap));

                    if (!_lagrangianParametersBounded.Item1)
                    {
                        var minDx = dx.SubVector(0, mm).Min();
                        dx.SetSubVector(mm, _numberOfParametersAndInequalityConstraints - mm, Vector<double>.Build.Dense(_numberOfParametersAndInequalityConstraints - mm, Math.Min(minDx, 0.01)));
                    }
                }

                go = -1.0;
                mu = mu / 10.0;

                while (go <= 0)
                {
                    var cholesky = (hessian + mu * Matrix<double>.Build.DenseOfDiagonalVector(dx.PointwisePower(2))).Cholesky();
                    var choleskyFactor = cholesky.Factor;

                    tempGradient = choleskyFactor.Transpose().Solve(gradient);

                    Vector<double> u;
                    if (_numberOfTotalConstraints == 0)
                    {
                        u = -1 * choleskyFactor.Solve(tempGradient);
                    }
                    else
                    {
                        var qr = (choleskyFactor.Transpose().Solve(a.Transpose())).QR();
                        lagrangianMultipliers = qr.Solve(tempGradient);
                        u = -1 * choleskyFactor.Solve(tempGradient - choleskyFactor.Transpose().Solve(a.Transpose()) * lagrangianMultipliers);
                    }

                    parameter0 = u.SubVector(0, _numberOfParametersAndInequalityConstraints) + parameter;

                    if (!_lagrangianParametersBounded.Item2)
                    {
                        go = 1.0;
                    }
                    else
                    {
                        var check = (parameter0.SubVector(0, mm) - parameterBounds.Column(0)).ToColumnMatrix()
                            .Append((parameterBounds.Column(1) - parameter0.SubVector(0, mm)).ToColumnMatrix());
                        go = check.Enumerate().Min();
                        mu *= 3.0;
                    }
                }

                var pt = Matrix<double>.Build.Dense(parameter.Count, 3);
                var sob = Vector<double>.Build.Dense(3);

                _alpha[0] = 0;
                pt.SetColumn(0, parameter);
                pt.SetColumn(1, parameter);
                sob[0] = objectFunctionValue;
                sob[1] = objectFunctionValue;
                _alpha[2] = 1.0;
                pt.SetColumn(2, parameter0);

                var scaledPt = pt.SubMatrix( _numberOfInequalityConstraints, _numberOfParameters, 2, 1).Column(0)
                    .PointwiseMultiply(scale.SubVector(_numberOfTotalConstraints + 1, _numberOfParameters));
                var costVector3 = _objectiveFunction(scaledPt).PointwiseDivide(scale.SubVector(0, _numberOfTotalConstraints + 1));
                sob[2] = costVector3[0];

                if (_numberOfInequalityConstraints > 0)
                {
                    var costVector3Inequality = costVector3.SubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints);
                    var ptInequality = pt.SubMatrix(0, _numberOfInequalityConstraints, 2, 1).Column(0);
                    costVector3.SetSubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints, costVector3Inequality - ptInequality);
                }

                if (_numberOfTotalConstraints != 0)
                {
                    var costVector3Sub = costVector3.SubVector(1, _numberOfTotalConstraints);
                    costVector3.SetSubVector(1, _numberOfTotalConstraints, costVector3Sub - a * pt.Column(2) + b);
                    sob[2] = costVector3[0] - lagrangianMultipliers0.DotProduct(costVector3.SubVector(1, _numberOfTotalConstraints)) +
                             rho * Math.Pow(Utils.EuclideanNorm(costVector3.SubVector(1, _numberOfTotalConstraints)), 2);
                }

                go = 1.0;
                while (go > tolerance)
                {
                    _alpha[1] = 0.5 * (_alpha[0] + _alpha[2]);
                    pt.SetColumn(1, (1 - _alpha[1]) * parameter + _alpha[1] * parameter0);

                    var scaledPt1 = pt.SubMatrix(_numberOfInequalityConstraints, _numberOfParameters, 1, 1).Column(0)
                        .PointwiseMultiply(scale.SubVector(_numberOfTotalConstraints + 1, _numberOfParameters));
                    var costVector2 = _objectiveFunction(scaledPt1).PointwiseDivide(scale.SubVector(0, _numberOfTotalConstraints + 1));
                    sob[1] = costVector2[0];

                    if (_numberOfInequalityConstraints > 0)
                    {
                        var costVector2Inequality = costVector2.SubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints);
                        var ptInequality = pt.SubMatrix(0, _numberOfInequalityConstraints, 1, 1).Column(0);
                        costVector2.SetSubVector(_numberOfEqualityConstraints + 1, _numberOfInequalityConstraints, costVector2Inequality - ptInequality);
                    }

                    if (_numberOfTotalConstraints != 0)
                    {
                        var costVector2Sub = costVector2.SubVector(1, _numberOfTotalConstraints);
                        costVector2.SetSubVector(1, _numberOfTotalConstraints, costVector2Sub - a * pt.Column(1) + b);
                        sob[1] = costVector2[0] - lagrangianMultipliers0.DotProduct(costVector2.SubVector(1, _numberOfTotalConstraints)) +
                                 rho * Math.Pow(Utils.EuclideanNorm(costVector2.SubVector(1, _numberOfTotalConstraints)), 2);
                    }

                    double obm = sob.Max();
                    if (obm < objectFunctionValue)
                    {
                        double obn = sob.Min();
                        go = tolerance * (obm - obn) / (objectFunctionValue - obm);
                    }

                    if (sob[1] >= sob[0])
                    {
                        sob[2] = sob[1];
                        _alpha[2] = _alpha[1];
                        pt.SetColumn(2, pt.Column(1));
                    }
                    else if(sob[0] <= sob[2])
                    {
                        sob[2] = sob[1];
                        _alpha[2] = _alpha[1];
                        pt.SetColumn(2, pt.Column(1));
                    }
                    else
                    {
                        sob[0] = sob[1];
                        _alpha[0] = _alpha[1];
                        pt.SetColumn(0, pt.Column(1));
                    }

                    if (go >= tolerance)
                    {
                        go = _alpha[2] - _alpha[0];
                    }
                }

                tempParameter = parameter;
                tempGradient = gradient;
                positiveChange = true;
                double obn_val = sob.Min();

                if (objectFunctionValue <= obn_val)
                {
                    maxIterations = minorIteration;
                }

                reduction = (objectFunctionValue - obn_val) / (1 + Math.Abs(objectFunctionValue));

                if (reduction < tolerance)
                {
                    maxIterations = minorIteration;
                }

                if (sob[0] < sob[1])
                {
                    objectFunctionValue = sob[0];
                    parameter = pt.Column(0);
                }
                else if (sob[2] < sob[1])
                {
                    objectFunctionValue = sob[2];
                    parameter = pt.Column(2);
                }
                else
                {
                    objectFunctionValue = sob[1];
                    parameter = pt.Column(1);
                }
            }

            parameter = parameter.PointwiseMultiply(scale.SubVector(_numberOfEqualityConstraints + 1, _numberOfTotalConstraints + _numberOfParameters - _numberOfEqualityConstraints));

            if (_numberOfTotalConstraints != 0)
            {
                lagrangianMultipliers = (scale[0] * lagrangianMultipliers).PointwiseDivide(scale.SubVector(1, _numberOfTotalConstraints));
            }

            var scaleSub2 = scale.SubVector(_numberOfEqualityConstraints + 1, _numberOfTotalConstraints + _numberOfParameters - _numberOfEqualityConstraints);
            hessian = scale[0] * hessian.PointwiseDivide(scaleSub2.ToRowMatrix().Transpose() * scaleSub2.ToRowMatrix());
            hessian = (hessian + hessian.Transpose()) / 2.0;

            if (reduction > tolerance)
            {
                // Log warning
            }
        }
    }
}
