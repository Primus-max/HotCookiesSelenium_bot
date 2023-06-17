using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Net.Http;
using System.Threading.Tasks;
using System;

public class BrowserManager
{
    public static async Task<IWebDriver> ConnectBrowserAsync(string profileId)
    {
        string launchUrl = $"http://local.adspower.com:50325/api/v1/browser/start?user_id={profileId}";
        using var httpClient = new HttpClient();
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

        string status = string.Empty;
        string remoteAddressWithSelenium = string.Empty;
        try
        {
            status = (string)responseDataJson["msg"];
            remoteAddressWithSelenium = (string)responseDataJson?["data"]?["ws"]?["selenium"];
        }
        catch (Exception)
        {
            // Handle the exception appropriately
        }

        if (status == "failed")
        {
            return null;
        }

        var options = new ChromeOptions();
        options.AddArgument("--silent");
        options.DebuggerAddress = remoteAddressWithSelenium;

        var driver = new ChromeDriver(options);
        return driver;
    }
}
