using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
using PuppeteerSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HotCookies
{
    public class BrowserManager
    {
        public static async Task<RemoteWebDriver> ConnectDriver(string profileId)
        {
            string? API_KEY = "f625413de27c27bade088cb4d44f736e";
            string baseUrl = "ws://";
            string launchUrl = $"http://local.adspower.com:50325/api/v1/browser/start?user_id={profileId}";
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(launchUrl);
            string responseString = await response.Content.ReadAsStringAsync();

            JObject? responseDataJson = null;
            try
            {
                responseDataJson = JObject.Parse(responseString);
            }
            catch (JsonReaderException ex)
            {
                // Handle the exception appropriately, e.g. log it or rethrow it
                Console.WriteLine($"Failed to parse response JSON: {ex.Message}");
                return null;
            }

            string? status = (string?)responseDataJson?["msg"];
            string? remoteAddressWithoutHttp = (string?)responseDataJson?["data"]?["ws"]?["selenium"];

            string? remoteAddressWithPuppeteer= (string?)responseDataJson?["data"]?["ws"]?["puppeteer"];

            var browser = await Puppeteer.ConnectAsync(new ConnectOptions
            {
                BrowserWSEndpoint = $"{remoteAddressWithPuppeteer}"
            });

            // Создание новой страницы в браузере
            var page = await browser.NewPageAsync();

            // Переход на заданный URL
            string targetUrl = "https://www.ya.ru";
            await page.GoToAsync(targetUrl);

            string fullRemoteAdress = Path.Combine(baseUrl, remoteAddressWithoutHttp ?? "");

            if (status == "failed") { return null; }

            var options = new ChromeOptions();
            options.AddArguments("start-maximized");

            ReadOnlyDesiredCapabilities? capabilities = options.ToCapabilities() as ReadOnlyDesiredCapabilities;
            if (capabilities == null)
            {
                throw new InvalidOperationException("Failed to create capabilities from options");
            }

            RemoteWebDriver? driver = null;
            int retries = 0;
            while (driver == null && retries < 10)
            {
                try
                {
                    driver = new RemoteWebDriver(new Uri(fullRemoteAdress), capabilities);
                }
                catch (WebDriverException)
                {
                    await Task.Delay(1000);
                    retries++;
                }
            }

            if (driver == null)
            {
                throw new InvalidOperationException("Failed to create driver after 10 retries");
            }

            return driver;
        }
    }
}
