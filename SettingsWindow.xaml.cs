using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rezystory
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            this.tbThresholdSD.Text = MainWindow.sdThreshold.ToString();
            this.tbThresholdFi.Text = MainWindow.fiThreshold.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Attempt to parse the text from tbThresholdFi. If successful, set MainWindow.fiThreshold.
            if (float.TryParse(tbThresholdFi.Text, out float parsedFiThreshold))
            {
                MainWindow.fiThreshold = parsedFiThreshold;
            }
            else
            {
                // Handle the error or set a default value if parsing fails.
                MessageBox.Show("Niepoprawna wartość progu średniej");
            }

            // Parse the text from tbThresholdSD and set MainWindow.sdThreshold.
            if(float.TryParse(tbThresholdSD.Text, out float parsedSDThreshold))
            {
                MainWindow.sdThreshold = parsedSDThreshold;
            }
            else
            {
                MessageBox.Show("Niepoprawna wartość progu SD");
            }

            // Close the window.
            this.Close();
        }

    }

}
