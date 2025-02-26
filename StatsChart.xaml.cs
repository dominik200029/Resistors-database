using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;

namespace Rezystory
{
    public partial class StatsChart : System.Windows.Controls.UserControl
    {
        public StatsChart()
        {
            InitializeComponent();

            // Initialize the SeriesCollection for PieChart
            SeriesCollection = new SeriesCollection
            {
                new PieSeries
                {
                    Title = "Sztuk",
                    Values = new ChartValues<double> { 50 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "AA",
                    Values = new ChartValues<double> { 20 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "A",
                    Values = new ChartValues<double> { 30 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "B",
                    Values = new ChartValues<double> { 40 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "C",
                    Values = new ChartValues<double> { 40 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "D",
                    Values = new ChartValues<double> { 40 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "E",
                    Values = new ChartValues<double> { 40 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "F",
                    Values = new ChartValues<double> { 40 },
                    DataLabels = true
                },
                new PieSeries
                {
                    Title = "G",
                    Values = new ChartValues<double> { 40 },
                    DataLabels = true
                }
                // Add more PieSeries as needed
            };

            DataContext = this;
        }
        public void SetPieChartData(Dictionary<string, double> data)
        {
            SeriesCollection.Clear();
            foreach (var item in data)
            {
                SeriesCollection.Add(new PieSeries
                {
                    Title = item.Key,
                    Values = new ChartValues<double> { item.Value },
                    DataLabels = true
                });
            }
        }

        public SeriesCollection SeriesCollection { get; set; }
    }
}

