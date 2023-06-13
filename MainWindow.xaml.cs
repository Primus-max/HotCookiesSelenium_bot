using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        }

        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
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

            // Сериализация объекта модели в JSON-строку
            string json = JsonSerializer.Serialize(configuration);

            // Запись JSON-строки в файл
            File.WriteAllText("config.json", json);
        }

        private void LoadConfiguration()
        {
            try
            {
                // Чтение JSON-файла
                string json = File.ReadAllText("config.json");

                // Десериализация JSON-строки в объект модели
                var configuration = JsonSerializer.Deserialize<ConfigurationModel>(json);

                // Заполнение полей интерфейса значениями из объекта модели
                repeatCountTextBox.Text = configuration.RepeatCount.ToString();
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

        private void SetTextToRichTextBox(RichTextBox richTextBox, string text)
        {
            var textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                textRange.Load(memoryStream, DataFormats.Text);
            }
        }


    }
}
