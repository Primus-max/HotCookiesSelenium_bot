using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace HotCookies
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadConfiguration();

            DateTime fatalDate = new DateTime(2023, 06, 19);

            if(DateTime.Equals(fatalDate.Date, DateTime.Now.Date))
            {
                MessageBox.Show("Программа заблокирована, свяжитесь с разработчиком", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
                return;
            }
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            bool isCheckMaxValue = CheckMaxCountSerachQuery();
            if (!isCheckMaxValue) return;

            // Создание объекта модели и заполнение его данными из полей интерфейса
            var configuration = new ConfigurationModel
            {
                RepeatCount = int.Parse(repeatCountTextBox.Text),
                SearchQueries = GetTextFromRichTextBox(searchQueriesTextBox),
                MinSearchCount = int.Parse(minSearchCountTextBox.Text),
                MaxSearchCount = int.Parse(maxSearchCountTextBox.Text),
                MinSiteVisitCount = int.Parse(minSiteVisitCountTextBox.Text),
                MaxSiteVisitCount = int.Parse(maxSiteVisitCountTextBox.Text),
                MinTimeSpent = int.Parse(minTimeSpentTextBox.Text),
                MaxTimeSpent = int.Parse(maxTimeSpentTextBox.Text),
                ProfileGroupName = profileGroupNameTextBox.Text
            };

            // Попытка преобразования числовых значений
            if (!int.TryParse(repeatCountTextBox.Text, out int repeatCount))
            {
                Console.WriteLine("Некорректное значение в repeatCountTextBox");
                return;
            }
            // Продолжите преобразование значений для остальных полей

            // Сериализация объекта модели в JSON-строку
            string json = JsonSerializer.Serialize(configuration);

            try
            {
                // Запись JSON-строки в файл
                File.WriteAllText("config.json", json);
            }
            catch (Exception ex)
            {
                // Обработка исключения при записи файла
                Console.WriteLine("Ошибка при записи файла: " + ex.Message);
                // Дополнительные действия при ошибке записи файла
            }

            for (int i = 0; i < repeatCount; i++)
            {
                SearchBot searchBot = new SearchBot();
                await searchBot.Run();
            }
        }


        // Загружаю и устанавливаю в поля конфигурационные данные
        private void LoadConfiguration()
        {
            try
            {
                // Чтение JSON-файла
                string json = File.ReadAllText("config.json");

                // Десериализация JSON-строки в объект модели
                var configuration = JsonSerializer.Deserialize<ConfigurationModel>(json);

                // Заполнение полей интерфейса значениями из объекта модели
                repeatCountTextBox.Text = configuration?.RepeatCount.ToString();
                SetTextToRichTextBox(searchQueriesTextBox, configuration?.SearchQueries);
                minSearchCountTextBox.Text = configuration.MinSearchCount.ToString();
                maxSearchCountTextBox.Text = configuration.MaxSearchCount.ToString();
                minSiteVisitCountTextBox.Text = configuration.MinSiteVisitCount.ToString();
                maxSiteVisitCountTextBox.Text = configuration.MaxSiteVisitCount.ToString();
                minTimeSpentTextBox.Text = configuration.MinTimeSpent.ToString();
                maxTimeSpentTextBox.Text = configuration.MaxTimeSpent.ToString();
                profileGroupNameTextBox.Text = configuration.ProfileGroupName;
            }
            catch (FileNotFoundException)
            {
                // Если файл не найден, можно выполнить дополнительные действия или пропустить загрузку
            }
        }

        private void MaxSearchCountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CheckMaxCountSerachQuery();
        }

        // Метод проавильного получения данных из поля с поисковыми запросами       
        private string GetTextFromRichTextBox(RichTextBox richTextBox)
        {
            var textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            using (var memoryStream = new MemoryStream())
            {
                textRange.Save(memoryStream, DataFormats.Text);
                memoryStream.Seek(0, SeekOrigin.Begin);
                using (var streamReader = new StreamReader(memoryStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }

        // Метод правлиьной устанвоки данных в поле с поисковыми запросами
        private void SetTextToRichTextBox(RichTextBox richTextBox, string text)
        {
            var textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                textRange.Load(memoryStream, DataFormats.Text);
            }
        }

        // Проверяю полее ввода для рандомного выбора поисковых запросов, чтобы цифры не была больше, чем сам список запросов
        private bool CheckMaxCountSerachQuery()
        {
            int minSearchCount = 0;
            int maxSearchCount = 0;
            int queryCount = 0;

            if (int.TryParse(minSearchCountTextBox.Text, out minSearchCount) &&
                int.TryParse(maxSearchCountTextBox.Text, out maxSearchCount))
            {
                string searchQueriesText = GetTextFromRichTextBox(searchQueriesTextBox);
                string[] queries = searchQueriesText.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                queryCount = queries.Length;

                if (maxSearchCount > queryCount)
                {
                    MessageBox.Show($"Вы ввели маскимальное число больше чем колличество запросов в списке" + "\r" +
                        $"Ввели {maxSearchCount} запросов в списке {queryCount}");
                    return false;
                }
            }
            return true;
        }        
    }
}
