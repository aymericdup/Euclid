﻿using Euclid.Arithmetics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Euclid.IndexedSeries.Analytics.Regressions
{
    public class LazyPartialLeastSquaresLinearRegression<T, V> where T : IEquatable<T>, IComparable<T> where V : IEquatable<V>, IConvertible
    {
        #region Declarations
        private bool _returnAverageIfFailed;
        private bool _withConstant;
        private RegressionStatus _status;
        private LinearModel _linearModel = null;
        private DataFrame<T, double, V> _x;
        private Series<T, double, V> _y;
        #endregion

        public LazyPartialLeastSquaresLinearRegression(DataFrame<T, double, V> x, Series<T, double, V> y)
        {
            if (x == null || y == null) throw new ArgumentNullException("the x and y should not be null");
            if (x.Columns == 0 || x.Rows != y.Rows) throw new ArgumentException("the data is not consistent");

            _x = x.Clone();
            _y = y.Clone();
            _returnAverageIfFailed = false;
            _withConstant = true;
            _status = RegressionStatus.NotRan;
        }

        #region  Accessors

        #region Settables
        public bool ReturnAverageIfFailed
        {
            get { return _returnAverageIfFailed; }
            set { _returnAverageIfFailed = value; }
        }
        public bool WithConstant
        {
            get { return _withConstant; }
            set { _withConstant = value; }
        }
        #endregion

        #region Get
        public LinearModel LinearModel
        {
            get { return _linearModel; }
        }
        public RegressionStatus Status
        {
            get { return _status; }
        }
        #endregion

        #endregion

        public void Regress()
        {
            #region Matrices

            #region Load data
            int n = _y.Rows, p = _x.Columns;
            Matrix X = new Matrix(n, p + (_withConstant ? 1 : 0)), Y = new Matrix(n, 1);
            for (int i = 0; i < n; i++)
            {
                Y[i] = _y[i];
                if (_withConstant)
                {
                    X[i * (p + 1)] = 1;
                    for (int j = 0; j < p; j++) X[i * (p + 1) + j + 1] = _x[i, j];
                }
                else
                    for (int j = 0; j < p; j++) X[i * p + j] = _x[i, j];
            }
            #endregion

            double yb = Y.Sum / n,
                sst = Y.SumOfSquares - n * yb * yb;

            #region Perform calculations
            Matrix tX = X.FastTranspose,
                tXX = Matrix.FastTransposeBySelf(X),
                intm = tXX.FastInverse;
            if (intm == null)
            {
                if (_returnAverageIfFailed && !_withConstant)
                {
                    _linearModel = new LinearModel(yb, n, sst);
                    _status = RegressionStatus.Normal;
                }
                else
                    _status = RegressionStatus.BadData;
                return;
            }

            Matrix radix = intm ^ tX,
                A = radix ^ Y;
            #endregion

            Matrix H = X ^ radix,
                I = Matrix.IdentityMatrix(H.Rows, H.Columns),
                cov = tX ^ Y;

            #region Correlations
            double[] correls = new double[p];
            for (int i = 0; i < p; i++)
            {
                double xb = X.Column(1 + i).Sum / n,
                    sX = tXX[1 + i, 1 + i] / n - xb * xb,
                    cXY = cov[1 + i] / n - yb * xb;
                correls[i] = cXY / Math.Sqrt(sX * sst);
            }
            #endregion

            #region Subsets
            List<int> indices = new List<int>();
            for (int i = 0; i < p; i++)
                indices.Add(i);
            IEnumerable<IEnumerable<int>> subsets = Subsets.SubSets(indices);
            List<double> adjR2 = new List<double>();
            for (int i = 0; i < subsets.Count(); i++)
            {
                IEnumerable<int> subset = subsets.ElementAt(i);
                Matrix D = BuildDiagonalMatrix(A.Size, _withConstant, subset),
                    XDR = X ^ (D ^ radix),
                    ei = Y.FastTranspose ^ (I + (-2 * XDR) + Matrix.FastTransposeBySelf(XDR)) ^ Y;
                double r2i = 1 - (ei[0] * (n - 1)) / (sst * (n - 1 - subset.Count()));
                adjR2.Add(r2i);
            }
            double bestAdj = adjR2.Max();
            int best = adjR2.IndexOf(bestAdj);
            IEnumerable<int> bestSubset = subsets.ElementAt(best);
            Matrix bestD = BuildDiagonalMatrix(A.Size, _withConstant, bestSubset);
            Matrix bestXDR = X ^ (bestD ^ radix),
                e = Y.FastTranspose ^ (I + (-2 * bestXDR) + Matrix.FastTransposeBySelf(bestXDR)) ^ Y;
            double sse = e[0];
            A = bestD * A;
            #endregion



            #endregion

            #region Output
            List<double> beta = new List<double>();
            double beta0 = _withConstant ? A[0] : 0;
            for (int i = (_withConstant ? 1 : 0); i < A.Rows; i++) beta.Add(A[i * A.Columns]);
            #endregion

            _linearModel = new LinearModel(beta0, beta.ToArray(), correls, n, sse, sst - sse);
            _status = RegressionStatus.Normal;
        }

        private static Matrix BuildDiagonalMatrix(int size, bool withConstant, IEnumerable<int> subset)
        {
            Matrix result = new Matrix(size);
            if (withConstant) result[0, 0] = 1;

            foreach (int c in subset)
            {
                int col = c + (withConstant ? 1 : 0);
                result[col, col] = 1;
            }
            return result;
        }
    }
}