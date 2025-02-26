using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using LiveCharts;
using LiveCharts.Definitions.Charts;
using LiveCharts.Wpf;

namespace Rezystory
{
    public partial class LinChart : UserControl
    {
        public LinChart()
        {
            InitializeComponent();

            // Initialize the series collection with a LineSeries
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Values = new ChartValues<double> {}
                }
            };
           // var axisY = myChart.AxisY[0];
            YFormatter = value => value.ToString("0.##");
            // Example data to demonstrate initialization
            SeriesCollection[0].Values.Add(1d);

            // Set the DataContext for binding
            DataContext = this;
        }

        public SeriesCollection SeriesCollection { get; set; }
        public LogarithmicAxis logAxis { get; set; }

        public Func<double, string> YFormatter { get; set; }        // Function to update the chart with logarithmic Y-data
        public void UpdatePlotLogarithmic<T>(List<T> yData)
        {
            // Clear the current values
            SeriesCollection[0].Values.Clear();

            // Add the transformed Y data (logarithmic scale) to the series
            foreach (var y in yData)
            {
                double value = Convert.ToDouble(y) + 3;

                if (value > 0)
                {
                    // For positive values, use log10
                    SeriesCollection[0].Values.Add(Math.Log10(value));
                }
                else if (value < 0)
                {
                    // Handle negative values using symmetric log scale
                    SeriesCollection[0].Values.Add(-Math.Log10(Math.Abs(value)));
                }
                else
                {
                    // Handle zero (logarithm is undefined for zero)
                    SeriesCollection[0].Values.Add(double.NaN); // Use NaN or handle zero differently
                }
            }
        }

        // Function to update the chart with linear Y-data
        public void UpdatePlotLinear<T>(List<T> yData)
        {
            // Clear the current values
            SeriesCollection[0].Values.Clear();
            // Add the linear Y data to the series
            foreach (var y in yData)
            {
                double value = Convert.ToDouble(y);

                // Directly add the value without transformation
                SeriesCollection[0].Values.Add(value);
            }
        }
    }
}