using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace HotCookies
{
    public class BrowserManager
    {
        public static async Task<Browser> ConnectBrowser(string profileId)
        {
            string launchUrl = $"http://local.adspower.com:50325/api/v1/browser/start?user_id={profileId}";
            var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(launchUrl);
            string responseString = await response.Content.ReadAsStringAsync();

            JObject responseDataJson = null;
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

            string status = (string)responseDataJson["msg"];
            string remoteAddressWithPuppeteer = (string)responseDataJson["data"]["ws"]["puppeteer"];

            var browser = await Puppeteer.ConnectAsync(new ConnectOptions
            {
                BrowserWSEndpoint = remoteAddressWithPuppeteer
            });

            if (status == "failed")
            {
                await browser.CloseAsync();
                return null;
            }

            return (Browser)browser;
        }
    }
}
