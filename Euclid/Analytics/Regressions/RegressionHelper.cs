﻿using Euclid.IndexedSeries;
using System;
using System.Text;

namespace Euclid.Analytics.Regressions
{
    /// <summary>A helper for the regression</summary>
    public static class RegressionHelper
    {
        /// <summary>Displays the regression result as a string</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="dataFrame">the dataframe</param>
        /// <param name="series">the series</param>
        /// <param name="linearModel">the linear model</param>
        /// <param name="format">the format for the coefficients</param>
        /// <returns>a string</returns>
        public static string Text<T, V>(DataFrame<T, double, V> dataFrame, Series<T, double, V> series, LinearModel linearModel, string format)
            where T : IComparable<T>, IEquatable<T>
            where V : IEquatable<V>, IConvertible
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(string.Format("{0}={1}", series.Label.ToString(), string.Format(format, linearModel.Constant)));
            for (int i = 0; i < linearModel.Factors.Length; i++)
                sb.AppendFormat(string.Format("{0}*{1}", string.Format(format, linearModel.Factors[i]), dataFrame.GetLabel(i).ToString()));
            sb.Append(Environment.NewLine);
            sb.Append(string.Format("R²={0} Adj={1}", linearModel.R2, linearModel.AdjustedR2));
            return sb.ToString();
        }
    }
}
