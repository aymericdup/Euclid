﻿using Euclid.Helpers;
using Euclid.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Euclid.IndexedSeries
{
    /// <summary>
    /// Class representing a DataFrame of synchronized data
    /// </summary>
    /// <typeparam name="T">the legend type</typeparam>
    /// <typeparam name="U">the data type</typeparam>
    /// <typeparam name="V">the label type</typeparam>
    public class DataFrame<T, U, V> : IIndexedSeries<T, U, V> where T : IComparable<T>, IEquatable<T> where V : IEquatable<V>, IConvertible
    {
        #region Declarations
        private V[] _labels;
        private U[,] _data;
        private T[] _legends;
        #endregion

        #region Constructors
        /// <summary>
        /// Builds an empty <c>DataFrame</c>
        /// </summary>
        /// <param name="rows">the number of rows</param>
        /// <param name="columns">the number of columns</param>
        public DataFrame(int rows, int columns)
        {
            _data = new U[rows, columns];
            _labels = new V[columns];
            _legends = new T[rows];
        }

        /// <summary>
        /// Builds a <c>DataFrame</c>
        /// </summary>
        /// <param name="labels">the labels</param>
        /// <param name="legends">the legends</param>
        /// <param name="data">the data</param>
        public DataFrame(IEnumerable<V> labels, IEnumerable<T> legends, U[,] data)
        {
            _data = Arrays.Clone(data);
            _labels = labels.ToArray();
            _legends = legends.ToArray();
        }

        /// <summary>
        /// Builds a <c>DataFrame</c> from its serialized form
        /// </summary>
        /// <param name="dataFrameNode">the <c>XmlNode</c></param>
        public DataFrame(XmlNode dataFrameNode)
        {
            FromXml(dataFrameNode);
        }
        #endregion

        #region Accessors
        /// <summary>
        /// Gets the legends
        /// </summary>
        public T[] Legends
        {
            get { return _legends; }
        }

        /// <summary>
        /// Gets the labels
        /// </summary>
        public V[] Labels
        {
            get { return _labels; }
        }

        /// <summary>
        /// Gets the number of columns
        /// </summary>
        public int Columns
        {
            get { return _labels.Length; }
        }

        /// <summary>
        /// Returns the number of rows
        /// </summary>
        public int Rows
        {
            get { return _legends.Length; }
        }

        /// <summary>Gets the data</summary>
        public U[,] Data
        {
            get { return _data; }
        }
        #endregion

        #region Methods
        /// <summary>Clones the <c>DataFrame</c></summary>
        /// <returns>a <c>DataFrame</c></returns>
        public DataFrame<T, U, V> Clone()
        {
            return new DataFrame<T, U, V>(Arrays.Clone(_labels), Arrays.Clone(_legends), Arrays.Clone(_data));
        }

        /// <summary>Remove the data for a given label</summary>
        /// <param name="label">the label</param>
        public void RemoveColumnAt(V label)
        {
            int indexToRemove = Array.IndexOf<V>(_labels, label);
            if (indexToRemove == -1 || _labels.Length == 1) return;
            U[,] newData = new U[_legends.Length, _data.Length - 1];
            V[] newLabels = new V[_labels.Length - 1];
            for (int j = 0; j < _labels.Length; j++)
            {
                if (j == indexToRemove) continue;
                int k = j - (j < indexToRemove ? 0 : 1);
                newLabels[k] = _labels[j];

                for (int i = 0; i < _legends.Length; i++)
                    newData[i, k] = _data[i, j];
            }
            _labels = newLabels;
            _data = newData;
        }

        /// <summary>Removes the row for a given legend</summary>
        /// <param name="t">the legend</param>
        public void RemoveRowAt(T t)
        {
            int indexToRemove = Array.IndexOf<T>(_legends, t);
            if (indexToRemove == -1 || _legends.Length == 1) return;
            U[,] newData = new U[_legends.Length - 1, _labels.Length];
            T[] newLegends = new T[_legends.Length - 1];
            for (int i = 0; i < _legends.Length; i++)
            {
                if (i == indexToRemove) continue;
                int k = i - (i < indexToRemove ? 0 : 1);
                newLegends[k] = _legends[i];

                for (int j = 0; j < _labels.Length; j++)
                    newData[k, j] = _data[i, j];
            }
            _legends = newLegends;
            _data = newData;
        }

        /// <summary>Removes all the rows containing a data-point that fits a predicate</summary>
        /// <param name="predicate">the predicate</param>
        public void Remove(Func<T, U, V, bool> predicate)
        {
            #region Kept Indices
            List<int> keptIndices = new List<int>();
            for (int i = 0; i < _legends.Length; i++)
            {
                bool lineShouldBeRemoved = false;
                for (int j = 0; j < _labels.Length; j++)
                    if (predicate(_legends[i], _data[i, j], _labels[j]))
                    {
                        lineShouldBeRemoved = true;
                        break;
                    }
                if (!lineShouldBeRemoved)
                    keptIndices.Add(i);
            }
            #endregion

            #region Extraction
            U[,] newData = new U[keptIndices.Count, _labels.Length];
            T[] newLegends = new T[keptIndices.Count];
            for (int i = 0; i < keptIndices.Count; i++)
            {
                newLegends[i] = _legends[keptIndices[i]];

                for (int j = 0; j < _labels.Length; j++)
                    newData[i, j] = _data[keptIndices[i], j];
            }
            _legends = newLegends;
            _data = newData;
            #endregion
        }

        /// <summary>Gets and sets the data for the i-th row and j-th column of the <c>DataFrame</c></summary>
        /// <param name="i">the row index</param>
        /// <param name="j">the column index</param>
        /// <returns>a data point</returns>
        public U this[int i, int j]
        {
            get { return _data[i, j]; }
            set { _data[i, j] = value; }
        }

        /// <summary>
        /// Gets and sets the data for a given legend and a given label
        /// </summary>
        /// <param name="t">the legend</param>
        /// <param name="v">the label</param>
        /// <returns>a data point</returns>
        public U this[T t, V v]
        {
            get
            {
                int i = Array.IndexOf<T>(_legends, t),
                    j = Array.IndexOf<V>(_labels, v);
                if (i == -1 || j == -1) throw new ArgumentException(string.Format("Point [{0}, {1}] was not found", t.ToString(), v.ToString()));
                return _data[i, j];
            }
            set
            {
                int i = Array.IndexOf<T>(_legends, t),
                    j = Array.IndexOf<V>(_labels, v);
                if (i == -1 || j == -1) throw new ArgumentException(string.Format("Point [{0}, {1}] was not found", t.ToString(), v.ToString()));
                _data[i, j] = value;
            }
        }

        /// <summary>
        /// Gets the data-point row of the given legend
        /// </summary>
        /// <param name="t">the legend</param>
        /// <returns>a <c>Slice</c></returns>
        public Slice<T, U, V> GetSliceAt(T t)
        {
            int index = Array.IndexOf<T>(_legends, t);
            if (index == -1) throw new ArgumentException(string.Format("Legend [{0}] was not found", t.ToString()));
            U[] result = new U[_labels.Length];
            for (int j = 0; j < _labels.Length; j++)
                result[j] = _data[index, j];
            return new Slice<T, U, V>(_labels, t, result);
        }

        /// <summary>
        /// Gets the data-point column of the given label
        /// </summary>
        /// <param name="v">the label</param>
        /// <returns> a <c>Series</c></returns>
        public Series<T, U, V> GetSeriesAt(V v)
        {
            int index = Array.IndexOf<V>(_labels, v);
            if (index == -1) throw new ArgumentException("Label [" + v.ToString() + "] was not found");
            U[] result = new U[_legends.Length];
            for (int i = 0; i < _legends.Length; i++)
                result[i] = _data[i, index];
            return new Series<T, U, V>(v, _legends, result);
        }

        /// <summary>
        /// Gets all the data as an array of <c>Slice</c>
        /// </summary>
        /// <returns>an array of <c>Slice</c></returns>
        public Slice<T, U, V>[] GetRows()
        {
            Slice<T, U, V>[] result = new Slice<T, U, V>[_legends.Length];
            for (int i = 0; i < _legends.Length; i++)
            {
                U[] data = new U[_labels.Length];
                for (int j = 0; j < _labels.Length; j++)
                    data[j] = _data[i, j];

                result[i] = new Slice<T, U, V>(_labels, _legends[i], data);
            }
            return result;
        }

        /// <summary>
        /// Gets all the data as an array of <c>Series</c>
        /// </summary>
        /// <returns>an array of <c>Series</c></returns>
        public Series<T, U, V>[] GetColumns()
        {
            Series<T, U, V>[] result = new Series<T, U, V>[_labels.Length];
            for (int j = 0; j < _labels.Length; j++)
            {
                U[] data = new U[_legends.Length];
                for (int i = 0; i < _legends.Length; i++)
                    data[i] = _data[i, j];

                result[j] = new Series<T, U, V>(_labels[j], _legends, data);
            }
            return result;
        }

        /// <summary>Gets the i-th column of the dataframe</summary>
        /// <param name="column">the target column</param>
        /// <returns>a <c>Series</c></returns>
        public Series<T, U, V> GetColumn(int column)
        {
            Series<T, U, V> result = new Series<T, U, V>(_legends.Length);

            U[] data = new U[_legends.Length];
            for (int i = 0; i < _legends.Length; i++)
                data[i] = _data[i, column];

            return new Series<T, U, V>(_labels[column], _legends, data);
        }

        /// <summary>
        /// Adds a column to the <c>DataFrame</c>
        /// </summary>
        /// <param name="label">the new column's label</param>
        /// <param name="column">the new column's data</param>
        public void Add(V label, U[] column)
        {
            U[,] newData = new U[_legends.Length, _labels.Length + 1];
            V[] newLabels = new V[_labels.Length + 1];
            for (int j = 0; j < _labels.Length; j++)
            {
                newLabels[j] = _labels[j];
                for (int i = 0; i < _legends.Length; i++)
                    newData[i, j] = _data[i, j];
            }

            newLabels[_labels.Length] = label;
            for (int i = 0; i < _legends.Length; i++)
                newData[i, _labels.Length] = column[i];

            _labels = newLabels;
            _data = newData;
        }

        /// <summary>
        /// Adds an empty column to the <c>DataFrame</c>
        /// </summary>
        /// <param name="label">the new column's label</param>
        public void Add(V label)
        {
            U[,] newData = new U[_legends.Length, _data.Length + 1];
            V[] newLabels = new V[_labels.Length + 1];
            for (int j = 0; j < _labels.Length; j++)
            {
                newLabels[j] = _labels[j];
                for (int i = 0; i < _legends.Length; i++)
                    newData[i, j] = _data[i, j];
            }

            newLabels[_labels.Length] = label;
            for (int i = 0; i < _legends.Length; i++)
                newData[i, _labels.Length] = default(U);

            _labels = newLabels;
            _data = newData;
        }

        /// <summary>Applies a function to all the data</summary>
        /// <param name="function">the function</param>
        public void ApplyOnData(Func<U, U> function)
        {
            for (int i = 0; i < _legends.Length; i++)
                for (int j = 0; j < _labels.Length; j++)
                    _data[i, j] = function(_data[i, j]);
        }

        /// <summary>Applies a function to all the legends</summary>
        /// <param name="function">the function</param>
        public void ApplyOnLegends(Func<T, T> function)
        {
            for (int i = 0; i < _legends.Length; i++)
                _legends[i] = function(_legends[i]);
        }

        /// <summary>Gets the i-th legend value</summary>
        /// <param name="index">the index</param>
        /// <returns>a legend value</returns>
        public T GetLegend(int index)
        {
            return _legends[index];
        }

        /// <summary>Sets the i-th legend value </summary>
        /// <param name="index">the index</param>
        /// <param name="value">the new legend value</param>
        public void SetLegend(int index, T value)
        {
            _legends[index] = value;
        }

        /// <summary>Gets the i-th label's value</summary>
        /// <param name="i">the index</param>
        /// <returns>a label</returns>
        public V GetLabel(int i)
        {
            return _labels[i];
        }

        /// <summary>Sets the i-th label's value</summary>
        /// <param name="i">the index</param>
        /// <param name="value">the new value</param>
        public void SetLabel(int i, V value)
        {
            _labels[i] = value;
        }
        #endregion

        #region IXmlable
        /// <summary>Serializes the <c>DataFrame</c> to Xml </summary>
        /// <param name="writer">the <c>XmlWriter</c></param>
        public void ToXml(XmlWriter writer)
        {
            writer.WriteStartElement("dataFrame");

            #region Labels
            for (int j = 0; j < _labels.Length; j++)
            {
                writer.WriteStartElement("label");
                writer.WriteAttributeString("value", _labels[j].ToString());
                writer.WriteAttributeString("index", j.ToString());
                writer.WriteEndElement();
            }
            #endregion

            #region Legends
            for (int i = 0; i < _legends.Length; i++)
            {
                writer.WriteStartElement("legend");
                writer.WriteAttributeString("value", _legends[i].ToString());
                writer.WriteAttributeString("index", i.ToString());
                writer.WriteEndElement();
            }
            #endregion

            #region Data
            for (int i = 0; i < _legends.Length; i++)
                for (int j = 0; j < _labels.Length; j++)
                {
                    writer.WriteStartElement("point");
                    writer.WriteAttributeString("row", i.ToString());
                    writer.WriteAttributeString("col", j.ToString());
                    writer.WriteAttributeString("value", _data[i, j].ToString());
                    writer.WriteEndElement();
                }
            #endregion

            writer.WriteEndElement();
        }

        /// <summary>De-serializes the <c>DataFrame</c> from a Xml node</summary>
        /// <param name="node">the <c>XmlNode</c></param>
        public void FromXml(XmlNode node)
        {
            {
                XmlNodeList labelNodes = node.SelectNodes("timeDataFrame/label"),
                    legendNodes = node.SelectNodes("timeDataFrame/legend"),
                    dataNodes = node.SelectNodes("timeDataFrame/point");

                #region Labels
                _labels = new V[labelNodes.Count];
                foreach (XmlNode label in labelNodes)
                {
                    int index = int.Parse(label.Attributes["index"].Value);
                    _labels[index] = label.Attributes["value"].Value.Parse<V>();
                }
                #endregion

                #region Legends
                _legends = new T[legendNodes.Count];
                foreach (XmlNode legend in legendNodes)
                {
                    int index = int.Parse(legend.Attributes["index"].Value);
                    _legends[index] = legend.Attributes["value"].Value.Parse<T>();
                }
                #endregion

                #region Data
                _data = new U[_legends.Length, _labels.Length];
                foreach (XmlNode point in dataNodes)
                {
                    int row = int.Parse(point.Attributes["row"].Value),
                        col = int.Parse(point.Attributes["col"].Value);
                    U value = point.Attributes["value"].Value.Parse<U>();
                    _data[row, col] = value;
                }
                #endregion
            }
        }
        #endregion

        #region ICSVable
        /// <summary>Builds a string representation of the content of the <c>DataFrame</c> </summary>
        /// <returns>a <c>String</c></returns>
        public string ToCSV()
        {

            string[] lines = new string[1 + _legends.Length];
            lines[0] = string.Format("x{0}{1}", CSVHelper.Separator, string.Join(CSVHelper.Separator.ToString(), _labels));
            for (int i = 0; i < _legends.Length; i++)
            {
                U[] row = new U[_labels.Length];
                for (int j = 0; j < _labels.Length; j++) row[j] = _data[i, j];
                lines[i + 1] = string.Format("{0}{1}{2}", _legends[i].ToString(), CSVHelper.Separator, string.Join(CSVHelper.Separator.ToString(), row));
            }
            return string.Join(Environment.NewLine, lines);
        }

        /// <summary>Fills a <c>DataFrame</c> from a string</summary>
        /// <param name="text">the <c>String</c> content</param>
        public void FromCSV(string text)
        {
            //TODO
            throw new NotImplementedException();
        }
        #endregion
    }
}
