using BazaRezystoryR;
using Npgsql;
using System.Data;
using System.Text;
using System.Windows;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using LiveCharts;
using LiveCharts.Wpf;
using System.Diagnostics;
using System.Windows.Input.Manipulations;

namespace Rezystory
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public SeriesCollection SeriesCollection { get; set; } // storing lin plot values

        //Postresql
        static NpgsqlConnection sqlConn = new NpgsqlConnection(BazaSQL.MyConnectionString);
        static String sqlQuery;
        static NpgsqlCommand sqlCmd;
        static NpgsqlCommand sqlCmd2;
        static NpgsqlDataReader sqlRd;
        static NpgsqlDataReader sqlRd2;
        //static DataTable sqlDt;
        static DataTable sqlDt_1;
        static DataTable sqlDt_2;
        public static float fiThreshold { get; set; } = 0;
        public static float sdThreshold { get; set; } = 0;

        List<float> pFi = new List<float> { };
        List<double> pSD = new List<double> { };

        float Fi;
        public MainWindow()
        {
            InitializeComponent();
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //'.' jako separtaor dziesiętny w konwersji na string i odwrotnie
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("pl");
            //sprawdzamy czy jest połączeni z bazą sql
            tbDate.Text = "Łączenie z bazą danych SQL. Czekaj...";

            bool isConnected = false;
            await Task.Run(() =>
            {
                //test połączenia sql
                isConnected = BazaSQL.CheckConnection();
            });

            if (isConnected)
            {
                //jest polaczenie 
                tbDate.Text = "Zeskanuj kod QR rezystora lub wpisz datę badania (dd.mm.rrrr) lub (mm.rrrr):";
                tbInput.IsEnabled = true;
                tbInput.Focus();
            }
        }
        private void dataGrid_Badania_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //przy tworzeniu datagrid trzeba pominąć ten krok bo nic nie jest wybrane
            var i = dataGrid_Badania.SelectedIndex;
            if (i == -1) return;
            try
            {
                //bierzemy wiersz który został wybrany 
                DataRow row = ((DataRowView)dataGrid_Badania.SelectedValue).Row;

                // Szukamy badania z badanieTest z taką samą datą
                try
                {
                    sqlConn.Open();

                    // Pierwsze zapytanie - rezystoryTest
                    sqlCmd = new NpgsqlCommand($"select * from rezystoryTest where date_time = '{row[1]}'", sqlConn);
                    sqlRd = sqlCmd.ExecuteReader();
                    sqlDt_1 = new DataTable();
                    sqlDt_1.Load(sqlRd); // Ładujemy dane do sqlDt_1
                    sqlRd.Close(); // Zamykamy pierwszy reader

                    // Drugie zapytanie - badaniaTest
                    sqlCmd2 = new NpgsqlCommand($"select * from rezystoryTest", sqlConn);
                    sqlRd2 = sqlCmd2.ExecuteReader();
                    sqlDt_2 = new DataTable();

                    sqlDt_2.Load(sqlRd2); // Ładujemy dane do sqlDt_2
                    sqlRd2.Close(); // Zamykamy drugi reader

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Błąd: " + ex.Message);
                }
                finally
                {
                    sqlConn.Close(); // Zamykamy połączenie po zakończeniu obu zapytań
                }
                //jeżeli są rezystory to wyświetl
                if (sqlDt_1.Rows.Count > 0)
                {
                    //znaleziono rekord
                    //pokazujemy wypełniamy dataGrid
                    dataGrid_Id.Visibility = Visibility.Visible;
                    dataGrid_Id.ItemsSource = null;
                    dataGrid_Id.ItemsSource = sqlDt_1.AsDataView();
                    dataGrid_Id.IsReadOnly = true;

                    int Sztuk = 0, AA = 0, A = 0, B = 0, C = 0, D = 0, E = 0, F = 0, G = 0;
                    int pSztuk = 0, pAA = 0, pA = 0, pB = 0, pC = 0, pD = 0, pE = 0, pF = 0, pG = 0;
                    float SumPPM = 0;
                    float pSumPPM = 0;
                    double ptempSD = 0;
                    double tempSD = 0;

                    List<double> pcoefList = new List<double>();
                    List<double> coefList = new List<double>();
                    List<double> coefListTEMP = new List<double>();


                    double temp_SD = 0;
                    int pSztuk_Temp = 0;
                    float pCoeff_Temp = 0;
                    int badania_Ilosc = 0;
                    string lastProcessedDate = null;

                    var lastRowIndex = sqlDt_2.Rows.Count - 1;
                    var lastRow = sqlDt_2.Rows[lastRowIndex];

                    pFi.Clear();
                    pSD.Clear();
                    // Iterate over the rows of the first DataGrid (dataGrid_Badania)
                    foreach (DataRowView item in dataGrid_Badania.Items)
                    {
                        string dateFromItem = item.Row[1].ToString();

                        foreach (DataRow row2 in sqlDt_2.Rows)
                        {
                            string dateFromRow2 = row2[2].ToString();
                            if (dateFromItem == dateFromRow2)
                            {
                                pSztuk++;
                                pSztuk_Temp++;

                                switch (row2[9].ToString())
                                {
                                    // tu jestesmy w badaniu i sumujemy wszystkie oporniki z danego okresu!!
                                    case "AA":
                                        pAA++; break;
                                    case "A":
                                        pA++; break;
                                    case "B":
                                        pB++; break;
                                    case "C":
                                        pC++; break;
                                    case "D":
                                        pD++; break;
                                    case "E":
                                        pE++; break;
                                    case "F":
                                        pF++; break;
                                    case "G":
                                        pG++; break;
                                    default:
                                        break;
                                }

                                //obliczanie średniej ppm i SD
                                if (float.TryParse(row2[8].ToString(), out float ptempCoef))
                                {
                                    if (!ptempCoef.Equals(float.NaN))
                                    {
                                        pSumPPM += ptempCoef;
                                        pCoeff_Temp += ptempCoef;
                                        pcoefList.Add(ptempCoef);
                                        coefListTEMP.Add(ptempCoef);
                                    }
                                }
                                ptempSD = GetStandardDeviation(pcoefList);
                                temp_SD = GetStandardDeviation(coefListTEMP);

                            }
                            if (dateFromItem != lastProcessedDate || row2 == lastRow)
                            {
                                float fi = pCoeff_Temp / pSztuk_Temp;
                                if (!fi.Equals(float.NaN))
                                {
                                    pFi.Add(fi);
                                }
                                if (!temp_SD.Equals(double.NaN) && temp_SD != 0)
                                {
                                    pSD.Add(temp_SD);
                                }
                                temp_SD = 0;
                                coefListTEMP.Clear();
                                pSztuk_Temp = 0;
                                pCoeff_Temp = 0;
                                badania_Ilosc += 1;
                                lastProcessedDate = dateFromItem;
                            }
                        }
                    }
                    foreach (DataRow r in sqlDt_1.Rows)
                    {
                        Sztuk++;

                        switch (r[9].ToString())
                        {
                            case "AA": AA++; break;
                            case "A": A++; break;
                            case "B": B++; break;
                            case "C": C++; break;
                            case "D": D++; break;
                            case "E": E++; break;
                            case "F": F++; break;
                            case "G": G++; break;
                            default: break;
                        }

                        //obiczanie średniej ppm i SD
                        if (float.TryParse(r[8].ToString(), out float tempCoef))
                        {
                            if (!tempCoef.Equals(float.NaN))
                            {
                                SumPPM += tempCoef;
                                coefList.Add(tempCoef);
                            }
                        }
                        tempSD = GetStandardDeviation(coefList);
                    }
                    // Update the text blocks with relevant data
                    tbPeriodSum.Text = $"Razem: {pSztuk}";
                    tbPeriodAA.Text = $"AA: {pAA}";
                    tbPeriodA.Text = $"A: {pA}";
                    tbPeriodB.Text = $"B: {pB}";
                    tbPeriodC.Text = $"C: {pC}";
                    tbPeriodD.Text = $"D: {pD}";
                    tbPeriodE.Text = $"E: {pE}";
                    tbPeriodF.Text = $"F: {pF}";
                    tbPeriodG.Text = $"G: {pG}";
                    tbPeriodFi.Text = $"Ø: {(pSumPPM / pSztuk):F2}";
                    tbPeriodSd.Text = $"SD: {ptempSD:F2}";


                    if ((float)(pSumPPM / pSztuk) > fiThreshold)
                    {
                        tbPeriodFi.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        tbPeriodFi.Foreground = new SolidColorBrush(Colors.Black);
                    }

                    if ((float)ptempSD > sdThreshold)
                    {
                        tbPeriodSd.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        tbPeriodSd.Foreground = new SolidColorBrush(Colors.Black);
                    }

                    // Update the text blocks with relevant data
                    tbSztuk.Text = $"Razem: {Sztuk}";
                    tbAA.Text = $"AA: {AA}";
                    tbA.Text = $"A: {A}";
                    tbB.Text = $"B: {B}";
                    tbC.Text = $"C: {C}";
                    tbD.Text = $"D: {D}";
                    tbE.Text = $"E: {E}";
                    tbF.Text = $"F: {F}";
                    tbG.Text = $"G: {G}";
                    Fi = SumPPM / Sztuk;
                    tbFi.Text = $"Ø: {(Fi):F2}";
                    tbSd.Text = $"SD: {tempSD:F2}";

                    if ((float)(Fi) > fiThreshold)
                    {
                        tbFi.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        tbFi.Foreground = new SolidColorBrush(Colors.Black);
                    }

                    if ((float)tempSD > sdThreshold)
                    {
                        tbSd.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    else
                    {
                        tbSd.Foreground = new SolidColorBrush(Colors.Black);
                    }

                    var pieData = new Dictionary<string, double>
                        {
                            { "Razem sztuk", Sztuk },
                            { "AA", AA },
                            { "A", A },
                            { "B", B },
                            { "C", C },
                            { "D", D },
                            { "E", E },
                            { "F", F },
                            { "G", G }
                        };

                    var p_pieData = new Dictionary<string, double>
                        {
                            { "Razem sztuk", pSztuk },
                            { "AA", pAA },
                            { "A", pA },
                            { "B", pB },
                            { "C", pC },
                            { "D", pD },
                            { "E", pE },
                            { "F", pF },
                            { "G", pG }
                        };

                    if (cbFiSD.Text == "∅")
                    {
                        if (cbLogLin.Text == "Liniowy")
                        {
                            fiChart.UpdatePlotLinear(pFi);
                        }
                        else if (cbLogLin.Text == "Logarytmiczny")
                        {
                            fiChart.UpdatePlotLogarithmic(pFi);
                        }
                    }
                    else if (cbFiSD.Text == "SD")
                    {
                        if (cbLogLin.Text == "Liniowy")
                        {
                            fiChart.UpdatePlotLinear(pSD);
                        }
                        else if (cbLogLin.Text == "Logarytmiczny")
                        {
                            fiChart.UpdatePlotLogarithmic(pSD);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }

        private void tbInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                bool dateEntered = false;
                if (tbInput.Text.Length > 5)
                {
                    if ((tbInput.Text[2].Equals('.') && tbInput.Text[5].Equals('.')) || //dd.mm.rrrr
                        (tbInput.Text[1].Equals('.') && tbInput.Text[4].Equals('.')) || //d.mm.rrrr
                        (tbInput.Text[2].Equals('.') && tbInput.Text.Length == 7) ||    //mm.rrrr
                        (tbInput.Text[1].Equals('.') && tbInput.Text.Length == 6))      //m.rrrr
                    {
                        if (IsValidDateTimeTest(tbInput.Text))
                        {
                            //wpisano porawną date ale jeszcze nie wiadomo czy jest w bazie
                            dateEntered = true;
                            //wyszukiwanie badania wg daty w bazie, jeżeli znaleziono to wyświetli
                            SearchFor_Badanie(tbInput.Text);

                            tbInput.Text = "";
                            tbInput.Focus();
                        }
                    }
                }

                if (!dateEntered)
                {
                    dataGrid_Badania.Visibility = Visibility.Hidden;
                    //wyszukiwanie R_id w bazie danych
                    SearchFor_Id(tbInput.Text);

                    tbInput.Text = "";
                    tbInput.Focus();
                }
            }
        }
        public double GetStandardDeviation(IEnumerable<double> values)
        {
            double standardDeviation = 0;
            double[] enumerable = values as double[] ?? values.ToArray();
            int count = enumerable.Count();
            if (count > 1)
            {
                double avg = enumerable.Average();
                double sum = enumerable.Sum(d => (d - avg) * (d - avg));
                standardDeviation = Math.Sqrt(sum / count);
            }
            return standardDeviation;
        }
        public bool IsValidDateTimeTest(string dateTime)
        {
            string[] formats = { "d.MM.yyyy", "M.yyyy" };
            DateTime parsedDateTime;
            return DateTime.TryParseExact(dateTime, formats, new CultureInfo("pl"),
                                           DateTimeStyles.None, out parsedDateTime);
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindVisualChildren<T>(ithChild)) yield return childOfChild;
            }
        }

        private void SearchFor_Id(string id)
        {
            //sqlConn = new NpgsqlConnection(BazaSQL.MyConnectionString);
            //tbStats.Text = "";

            try
            {
                sqlConn.Open();
                sqlCmd = new NpgsqlCommand($"select * from rezystoryTest where r_id = '{id}'", sqlConn);
                sqlRd = sqlCmd.ExecuteReader();
                sqlDt_1 = new DataTable();
                sqlDt_1.Load(sqlRd);
                sqlRd.Close();
                sqlConn.Close();

                //jeżeli są rezystory to wyświetl
                if (sqlDt_1.Rows.Count > 0)
                {
                    //znaleziono rekord
                    //pokazujemy wypełniamy dataGrid
                    dataGrid_Id.Visibility = Visibility.Visible;
                    dataGrid_Id.ItemsSource = null;
                    dataGrid_Id.ItemsSource = sqlDt_1.DefaultView;
                    dataGrid_Id.IsReadOnly = true;

                    /*foreach(var col in dataGrid_Id.Columns)
                    {
                        col.Width = 100;
                    }*/
                    //wybierz pierwszy wiersz z dataGrid_IdSearch żeby wywołać funkcję i wyświetlić wszystkie dane
                    dataGrid_Id.SelectedIndex = 0;//0 czyli defaultowo pierwszy element
                }
                else//nic nie znaleziono
                {
                    //ukryj kontorliki z danymi
                    var elements = FindVisualChildren<TextBlock>(this).Where(x => x.Tag != null);
                    foreach (var el in elements) el.Visibility = Visibility.Hidden;
                    dataGrid_Id.Visibility = Visibility.Hidden;
                    System.Windows.MessageBox.Show("Nie znaleziono rezystora o tym ID.");
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }
        private void SearchFor_Badanie(string strDate)
        {
            //schowaj kontroliki z danymi rezystora
            var elements = FindVisualChildren<TextBlock>(this).Where(x => x.Tag != null);
            foreach (var el in elements) el.Visibility = Visibility.Hidden;
            dataGrid_Id.Visibility = Visibility.Hidden;
            //sqlConn = new NpgsqlConnection(BazaSQL.MyConnectionString);
            //CultureInfo provider = CultureInfo.GetCultureInfo("en-IN");
            //DateTime date = DateTime.Parse("08/08/2020", provider, DateTimeStyles.AdjustToUniversal);
            try
            {
                sqlConn.Open();
                if (strDate.Length < 8) //Czyli wyszukiwanie z konkretnego miesiąca m.rrrr lub mm.rrrr
                {
                    string[] tempList = strDate.Split('.');
                    sqlCmd = new NpgsqlCommand($"select * from badaniaTest where extract(month from date_time) = '{tempList[0]}' and extract(year from date_time) = '{tempList[1]}' ", sqlConn);
                }
                else //Czyli wyszukiwanie z konkretnego dnia d.mm.rrrr lub dd.mm.rrrr
                {
                    sqlCmd = new NpgsqlCommand($"select * from badaniaTest where date_time::date = '{strDate}'", sqlConn);
                }

                sqlRd = sqlCmd.ExecuteReader();
                DataTable sqlDt = new DataTable();
                sqlDt.Load(sqlRd);
                sqlRd.Close();
                sqlConn.Close();

                //wykasowanie komunikatu o statystyce
                //tbStats.Text = "";

                //jeżeli są rezystory to wyświetl
                if (sqlDt.Rows.Count > 0)
                {
                    dataGrid_Badania.Visibility = Visibility.Visible;
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.ItemsSource = sqlDt.DefaultView;
                    dataGrid_Badania.IsReadOnly = true;

                    //wybierz pierwszy wiersz z dataGrid_Badanie i 
                    dataGrid_Badania.SelectedIndex = 0;//0 czyli pierwszy element żeby wywołać funkcję i wyświetlić wszysktie dane
                }
                else
                {
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.Visibility = Visibility.Hidden;
                    System.Windows.MessageBox.Show("Nie znaleziono badania z tą datą.");
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show(e.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.Show();
        }
        private void SaveDataGridToCsv(DataGrid dataGrid, string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    // Write the column headers
                    var columnHeaders = dataGrid.Columns
                        .Select(column => QuoteValue(column.Header?.ToString() ?? string.Empty))
                        .ToArray();
                    writer.WriteLine(string.Join(";", columnHeaders)); // Use semicolon delimiter
                    // Write the rows
                    foreach (var item in dataGrid.Items)
                    {
                        if (item is DataRowView rowView)
                        {
                            // If bound to a DataTable, use ItemArray
                            var rowValues = rowView.Row.ItemArray
                                .Select(value => QuoteValue(value?.ToString()))
                                .ToArray();
                            writer.WriteLine(string.Join(";", rowValues)); // Use semicolon delimiter
                        }
                        else
                        {
                            // For other types of data sources
                            var rowValues = dataGrid.Columns
                                .Select(column =>
                                {
                                    var cellContent = column.GetCellContent(item) as TextBlock;
                                    return QuoteValue(cellContent?.Text);
                                })
                                .ToArray();
                            writer.WriteLine(string.Join(";", rowValues)); // Use semicolon delimiter
                        }
                    }
                }

                System.Windows.MessageBox.Show("Zapisano do pliku csv", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Nie udało się zapisac do pliku: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Helper method to quote and escape values
        private string QuoteValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DateTime now = DateTime.Now;
            string date = now.ToString("yyyy-MM-dd_HH-mm-ss");  // Formatting the date to avoid invalid characters

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",
                DefaultExt = "csv",
                FileName = $"oporniki-{date}.csv"  // File name with a date format that uses hyphens and underscores
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveDataGridToCsv(dataGrid_Id, saveFileDialog.FileName);
            }
        }

        private void SearchFor_LastFourMonths()
        {
            // Ukryj kontrolki z danymi rezystora i wyczyść DataGrid
            var elements = FindVisualChildren<TextBlock>(this).Where(x => x.Tag != null);
            foreach (var el in elements) el.Visibility = Visibility.Hidden;
            dataGrid_Badania.Visibility = Visibility.Hidden;

            try
            {
                sqlConn.Open();

                // Pobranie aktualnej daty i godziny
                DateTime now = DateTime.Now;

                // Obliczenie daty sprzed 4 miesięcy
                DateTime lastFourMonths = now.AddMonths(-4);

                // Przygotowanie zapytania SQL dla danych z ostatnich 4 miesięcy
                sqlCmd = new NpgsqlCommand(
                    "SELECT * FROM badaniaTest WHERE date_time BETWEEN @lastFourMonths AND @now",
                    sqlConn);

                // Dodanie parametrów do zapytania
                sqlCmd.Parameters.AddWithValue("@lastFourMonths", lastFourMonths);
                sqlCmd.Parameters.AddWithValue("@now", now);

                sqlRd = sqlCmd.ExecuteReader();
                DataTable sqlDt = new DataTable();
                sqlDt.Load(sqlRd);
                sqlRd.Close();
                sqlConn.Close();

                // Jeśli są wyniki, wyświetl je w DataGrid
                if (sqlDt.Rows.Count > 0)
                {
                    dataGrid_Badania.Visibility = Visibility.Visible;
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.ItemsSource = sqlDt.DefaultView;
                    dataGrid_Badania.IsReadOnly = true;

                    // Wybierz pierwszy wiersz, aby wyświetlić szczegóły
                    dataGrid_Badania.SelectedIndex = 0;
                }
                else
                {
                    // Jeśli brak wyników, ukryj DataGrid i pokaż komunikat
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.Visibility = Visibility.Hidden;
                    System.Windows.MessageBox.Show("Brak wyników z ostatnich 4 miesięcy.");
                }
            }
            catch (Exception e)
            {
                // Obsługa błędów
                System.Windows.MessageBox.Show(e.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }




        private void ShowLastFourMonths_Click(object sender, RoutedEventArgs e)
        {
            SearchFor_LastFourMonths();
        }
        private void SearchFor_Last7Days()
        {
            // Ukryj kontrolki z danymi rezystora i wyczyść DataGrid
            var elements = FindVisualChildren<TextBlock>(this).Where(x => x.Tag != null);
            foreach (var el in elements) el.Visibility = Visibility.Hidden;
            dataGrid_Badania.Visibility = Visibility.Hidden;

            try
            {
                sqlConn.Open();

                // Pobranie aktualnej daty i godziny oraz obliczenie zakresu ostatnich 7 dni
                DateTime now = DateTime.Now;
                DateTime last7Days = now.AddDays(-7);

                // Przygotowanie zapytania SQL dla danych z ostatnich 7 dni
                sqlCmd = new NpgsqlCommand(
                    "SELECT * FROM badaniaTest WHERE date_time BETWEEN @last7Days AND @now",
                    sqlConn);

                // Dodanie parametrów do zapytania
                sqlCmd.Parameters.AddWithValue("@last7Days", last7Days);
                sqlCmd.Parameters.AddWithValue("@now", now);

                sqlRd = sqlCmd.ExecuteReader();
                DataTable sqlDt = new DataTable();
                sqlDt.Load(sqlRd);
                sqlRd.Close();
                sqlConn.Close();

                // Jeśli są wyniki, wyświetl je w DataGrid
                if (sqlDt.Rows.Count > 0)
                {
                    dataGrid_Badania.Visibility = Visibility.Visible;
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.ItemsSource = sqlDt.DefaultView;
                    dataGrid_Badania.IsReadOnly = true;

                    // Wybierz pierwszy wiersz, aby wyświetlić szczegóły
                    dataGrid_Badania.SelectedIndex = 0;
                }
                else
                {
                    // Jeśli brak wyników, ukryj DataGrid i pokaż komunikat
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.Visibility = Visibility.Hidden;
                    System.Windows.MessageBox.Show("Brak wyników z ostatnich 7 dni.");
                }
            }
            catch (Exception e)
            {
                // Obsługa błędów
                System.Windows.MessageBox.Show(e.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }
        private void SearchFor_CurrentMonth()
        {
            // Ukryj kontrolki z danymi rezystora i wyczyść DataGrid
            var elements = FindVisualChildren<TextBlock>(this).Where(x => x.Tag != null);
            foreach (var el in elements) el.Visibility = Visibility.Hidden;
            dataGrid_Badania.Visibility = Visibility.Hidden;

            try
            {
                sqlConn.Open();

                // Pobranie aktualnej daty
                DateTime now = DateTime.Now;

                // Ustalenie pierwszego dnia bieżącego miesiąca
                DateTime firstDayOfCurrentMonth = new DateTime(now.Year, now.Month, 1);

                // Przygotowanie zapytania SQL dla danych z bieżącego miesiąca
                sqlCmd = new NpgsqlCommand(
                    "SELECT * FROM badaniaTest WHERE date_time BETWEEN @firstDayOfCurrentMonth AND @now",
                    sqlConn);

                // Dodanie parametrów do zapytania
                sqlCmd.Parameters.AddWithValue("@firstDayOfCurrentMonth", firstDayOfCurrentMonth);
                sqlCmd.Parameters.AddWithValue("@now", now);

                sqlRd = sqlCmd.ExecuteReader();
                DataTable sqlDt = new DataTable();
                sqlDt.Load(sqlRd);
                sqlRd.Close();
                sqlConn.Close();

                // Jeśli są wyniki, wyświetl je w DataGrid
                if (sqlDt.Rows.Count > 0)
                {
                    dataGrid_Badania.Visibility = Visibility.Visible;
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.ItemsSource = sqlDt.DefaultView;
                    dataGrid_Badania.IsReadOnly = true;

                    // Wybierz pierwszy wiersz, aby wyświetlić szczegóły
                    dataGrid_Badania.SelectedIndex = 0;
                }
                else
                {
                    // Jeśli brak wyników, ukryj DataGrid i pokaż komunikat
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.Visibility = Visibility.Hidden;
                    System.Windows.MessageBox.Show("Brak wyników w bieżącym miesiącu.");
                }
            }
            catch (Exception e)
            {
                // Obsługa błędów
                System.Windows.MessageBox.Show(e.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }
        private void SearchFor_HalfYear()
        {
            // Ukryj kontrolki z danymi rezystora i wyczyść DataGrid
            var elements = FindVisualChildren<TextBlock>(this).Where(x => x.Tag != null);
            foreach (var el in elements) el.Visibility = Visibility.Hidden;
            dataGrid_Badania.Visibility = Visibility.Hidden;

            try
            {
                sqlConn.Open();

                // Pobranie aktualnej daty
                DateTime now = DateTime.Now;

                // Ustalenie daty początkowej dla przedziału 6 miesięcy
                DateTime sixMonthsAgo = now.AddMonths(-6);

                // Przygotowanie zapytania SQL dla danych z ostatnich 6 miesięcy
                sqlCmd = new NpgsqlCommand(
                    "SELECT * FROM badaniaTest WHERE date_time BETWEEN @sixMonthsAgo AND @now",
                    sqlConn);

                // Dodanie parametrów do zapytania
                sqlCmd.Parameters.AddWithValue("@sixMonthsAgo", sixMonthsAgo);
                sqlCmd.Parameters.AddWithValue("@now", now);

                sqlRd = sqlCmd.ExecuteReader();
                DataTable sqlDt = new DataTable();
                sqlDt.Load(sqlRd);
                sqlRd.Close();
                sqlConn.Close();

                // Jeśli są wyniki, wyświetl je w DataGrid
                if (sqlDt.Rows.Count > 0)
                {
                    dataGrid_Badania.Visibility = Visibility.Visible;
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.ItemsSource = sqlDt.DefaultView;
                    dataGrid_Badania.IsReadOnly = true;

                    // Wybierz pierwszy wiersz, aby wyświetlić szczegóły
                    dataGrid_Badania.SelectedIndex = 0;
                }
                else
                {
                    // Jeśli brak wyników, ukryj DataGrid i pokaż komunikat
                    dataGrid_Badania.ItemsSource = null;
                    dataGrid_Badania.Visibility = Visibility.Hidden;
                    System.Windows.MessageBox.Show("Brak wyników");
                }
            }
            catch (Exception e)
            {
                // Obsługa błędów
                System.Windows.MessageBox.Show(e.Message);
            }
            finally
            {
                sqlConn.Close();
            }
        }


        private void SearchFor_Last7Days_Click(object sender, RoutedEventArgs e)
        {
            SearchFor_Last7Days();
        }
        private void SearchFor_CurrentMonth_Click(object sender, RoutedEventArgs e)
        {
            SearchFor_CurrentMonth();
        }
        private void SearchFor_HalfYear_Click(object sender, RoutedEventArgs e)
        {
            SearchFor_HalfYear();
        }

        private void tbFiThreshold_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the Enter key was pressed
            if (e.Key == Key.Enter)
            {
                // Check if the TextBox is empty and set the default value
                if (string.IsNullOrWhiteSpace(tbFiThreshold.Text))
                {
                    tbFiThreshold.Text = "0,00";
                    MainWindow.fiThreshold = 0.00f; // Set the default value in MainWindow
                    return;
                }

                // Normalize the decimal separator to a period before parsing
                string normalizedText = tbFiThreshold.Text.Replace(',', '.');

                // Attempt to parse the normalized text
                if (float.TryParse(normalizedText, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedFiThreshold))
                {
                    MainWindow.fiThreshold = parsedFiThreshold; // Set the parsed value in MainWindow

                    // Example condition: Check if Fi exceeds fiThreshold
                    if ((float)(Fi) > parsedFiThreshold) // Ensure Fi and fiThreshold are defined elsewhere
                    {
                        tbFi.Foreground = new SolidColorBrush(Colors.Red); // Change text color
                    }
                    else
                    {
                        tbFi.Foreground = new SolidColorBrush(Colors.Black); // Reset text color
                    }
                }
                else
                {
                    // Show an error message if parsing fails
                    MessageBox.Show("Niepoprawna wartość progu ∅");
                }
            }
        }

        private void tbSdThreshold_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the Enter key was pressed
            if (e.Key == Key.Enter)
            {
                // Check if the TextBox is empty
                if (string.IsNullOrWhiteSpace(tbSdThreshold.Text))
                {
                    tbSdThreshold.Text = "0,00";
                    MainWindow.sdThreshold = 0.00f; // Update the default value in MainWindow
                    return;
                }

                // Normalize the decimal separator to a period before parsing
                string normalizedText = tbSdThreshold.Text.Replace(',', '.');

                if (float.TryParse(normalizedText, System.Globalization.NumberStyles.Float,
                                   System.Globalization.CultureInfo.InvariantCulture, out float parsedSDThreshold))
                {
                    MainWindow.sdThreshold = parsedSDThreshold;
                }
                else
                {
                    MessageBox.Show("Niepoprawna wartość progu SD");
                }
            }
        }

        private void cbFiSD_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0) // Ensure there's a new selection
            {
                var selectedItem = e.AddedItems[0] as ComboBoxItem; // Use as ComboBoxItem if ComboBox contains ComboBoxItem elements
                if (selectedItem != null)
                {
                    string selectedText = selectedItem.Content.ToString();

                    if (selectedText == "∅")
                    {
                        fiChart.UpdatePlotLinear(pFi);
                    }
                    else if (selectedText == "SD")
                    {
                        fiChart.UpdatePlotLinear(pSD);
                    }
                }
            }
        }

        private void cbLogLin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbFiSD != null)
            {
                if (e.AddedItems.Count > 0)
                {
                    var selectedItem = e.AddedItems[0] as ComboBoxItem;
                    if (selectedItem != null)
                    {
                        string selectedText = selectedItem.Content.ToString();
                        {
                            if (selectedText == "Liniowy")
                            {

                                if (cbFiSD.Text == "∅")
                                {
                                    fiChart.UpdatePlotLinear(pFi);
                                }
                                else if (cbFiSD.Text == "SD")
                                {
                                    fiChart.UpdatePlotLinear(pSD);
                                }
                            }
                            else if (selectedText == "Logarytmiczny")
                            {
                                if (cbFiSD.Text == "∅")
                                {
                                    fiChart.UpdatePlotLogarithmic(pFi);
                                }
                                else if (cbFiSD.Text == "SD")
                                {
                                    fiChart.UpdatePlotLogarithmic(pSD);
                                }
                            }
                        }
                    }
                }
            }
        }
        private void CalculateButton1_Click(object sender, RoutedEventArgs e)
        {
            int R1, R2;

            // Validate the input for R1
            if (!int.TryParse(tbR11value.Text, out R1) || R1 < 0)
            {
                MessageBox.Show("Podaj poprawną wartość rezystancji R1");
                return; // Exit the method if R1 is invalid
            }

            // Validate the input for R2
            if (!int.TryParse(tbR21value.Text, out R2) || R2 < 0)
            {
                MessageBox.Show("Podaj poprawną wartość rezystancji R2");
                return; // Exit the method if R2 is invalid
            }

            // Perform calculations if both R1 and R2 are valid
            float Rx = (float)(R1 * R2) / (R1 + R2);

            // Display the result
            tbRX1value.Text = "Rx=" + Rx.ToString();
        }

        private void CalculateButton2_Click(object sender, RoutedEventArgs e)
        {
            int R1, Rx;

            // Validate the input for R1
            if (!int.TryParse(tbR12value.Text, out R1) || R1 <= 0)
            {
                MessageBox.Show("Podaj poprawną wartość rezystancji R1 (musi być większa od 0)");
                return; // Exit the method if R1 is invalid
            }

            // Validate the input for Rx
            if (!int.TryParse(tbRx2value.Text, out Rx) || Rx <= 0)
            {
                MessageBox.Show("Podaj poprawną wartość rezystancji Rx (musi być większa od 0)");
                return; // Exit the method if Rx is invalid
            }

            // Check if Rx is greater than or equal to R1
            if (Rx >= R1)
            {
                MessageBox.Show("Wartość Rx nie może być większa lub równa wartości R1");
                return; // Exit the method if Rx is invalid
            }

            // Perform calculations if both R1 and Rx are valid
            float R2 = (float)(R1 * Rx) / (R1 - Rx);

            // Display the result
            tbR22value.Text = "R2 = " + R2.ToString("F2"); // Format to two decimal places
        }


        private void CalculateButton3_Click(object sender, RoutedEventArgs e)
        {
            // Zmienne dla rezystancji (int) i współczynników temperaturowych (float)
            int R1, R2, R3;
            float WtR1, WtR2, WtR3;

            // Walidacja i wczytanie wartości rezystancji
            if (!int.TryParse(tbR13value.Text, out R1) || R1 <= 0)
            {
                MessageBox.Show("Podaj poprawną wartość rezystancji R1");
                return;
            }

            if (!int.TryParse(tbR23value.Text, out R2) || R2 <= 0)
            {
                MessageBox.Show("Podaj poprawną wartość rezystancji R2");
                return;
            }

            if (!int.TryParse(tbR33value.Text, out R3))
            {
                MessageBox.Show("Podaj poprawną wartość rezystancji R3");
                return;
            }

            // Zamiana przecinków na kropki i walidacja współczynników temperaturowych
            if (!float.TryParse(tbWt1value.Text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out WtR1))
            {
                MessageBox.Show("Podaj poprawną wartość współczynnika temperaturowego dla R1");
                return;
            }

            if (!float.TryParse(tbWt2value.Text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out WtR2))
            {
                MessageBox.Show("Podaj poprawną wartość współczynnika temperaturowego dla R2");
                return;
            }

            if (!float.TryParse(tbWt3value.Text.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out WtR3))
            {
                MessageBox.Show("Podaj poprawną wartość współczynnika temperaturowego dla R3");
                return;
            }

            double WtRx = 0;
            // Obliczenie współczynnika temperaturowego zastępczej rezystancji
            if (R3 > 0)
            {
                double sumaRezystancji = R1 * R2 + R2 * R3 + R3 * R1;
                double czesc1 = (WtR1 / R1) * (R1 * R2 * R3) / sumaRezystancji;
                double czesc2 = (WtR2 / R2) * (R1 * R2 * R3) / sumaRezystancji;
                double czesc3 = (WtR3 / R3) * (R1 * R2 * R3) / sumaRezystancji;
                WtRx = czesc1 + czesc2 + czesc3;
            }
            else
            {
                // R3 = 0, uwzględniamy tylko R1 i R2
                double sumaRezystancji = R1 + R2;
                double czesc1 = (WtR1 / R1) * (R1 * R2) / sumaRezystancji;
                double czesc2 = (WtR2 / R2) * (R1 * R2) / sumaRezystancji;
                WtRx = czesc1 + czesc2;
            }

            // Wyświetlenie wyników
            tbWtXvalue.Text = "Wt(Rx)=" + WtRx.ToString("F6");
        }


    }
}