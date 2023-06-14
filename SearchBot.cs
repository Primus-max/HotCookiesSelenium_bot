using HotCookies;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using PuppeteerSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

public class SearchBot
{
    private static readonly Random random = new Random();

    private ConfigurationModel configuration;

    public async Task Run()
    {
        LoadConfiguration();

        List<Profile> profiles = await ProfileManager.GetProfiles();

        // Поиск профилей по группе
        List<Profile> selectedProfiles = profiles.Where(p => p.GroupName == configuration.ProfileGroupName).ToList();
        if (selectedProfiles.Count == 0)
        {
            Console.WriteLine($"No profiles found in group {configuration.ProfileGroupName}.");
            return;
        }

        foreach (Profile profile in selectedProfiles)
        {
            using (var browser = await BrowserManager.ConnectBrowser(profile.UserId))
            {
                if (browser == null)
                {
                    Console.WriteLine($"Failed to connect browser for profile {profile.UserId}.");
                    continue;
                }

                var page = await browser.NewPageAsync();
                //await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080 });
                await page.GoToAsync("https://www.google.com");

                for (int i = 0; i < configuration.RepeatCount; i++)
                {
                    string searchQuery = GetRandomSearchQuery();
                    await PerformSearch(page, searchQuery);
                    await SpendRandomTime();
                    await GetSearchResultLinks(page);

                    await ScrollPageSmoothly(page);
                    await ScrollPageSmoothly(page, true);
                    await ReturnToGoogleSearch(page);
                }
            }
        }
    }

    private async Task PerformSearch(IPage page, string searchQuery)
    {
        //await page.GoToAsync("https://www.google.com");
        await page.WaitForSelectorAsync("input[name='q']");
        await page.TypeAsync("input[name='q']", searchQuery);
        await page.Keyboard.PressAsync("Enter");

        // Добавьте код для ожидания загрузки страницы результатов поиска, если необходимо
    }

    private async Task<List<string>> GetSearchResultLinks(IPage page)
    {
        .await page.WaitForSelectorAsync("h3.LC20lb.MBeuO.DKV0Md");
        var links = await page.EvaluateExpressionAsync<string[]>(@"
        Array.from(document.querySelectorAll('h3.LC20lb.MBeuO.DKV0Md'))
             .map(link => link.textContent.trim())
    ");

        return links.ToList();
    }


    private async Task ScrollPageSmoothly(IPage page, bool scrollUp = false)
    {
        int scrollDistance = scrollUp ? -1000 : 1000;
        await page.EvaluateExpressionAsync($"window.scrollBy(0, {scrollDistance});");
        await page.WaitForTimeoutAsync(1000);
    }

    private async Task ReturnToGoogleSearch(IPage page)
    {
        await page.GoToAsync("https://www.google.com");
        await page.WaitForTimeoutAsync(2000);
    }

    private void LoadConfiguration()
    {
        string json = File.ReadAllText("config.json");
        configuration = JsonConvert.DeserializeObject<ConfigurationModel>(json);
    }

    private string GetRandomSearchQuery()
    {        
        string[] queries = configuration.SearchQueries?.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (queries.Length == 0)
        {
            Console.WriteLine("No search queries found in configuration.");
            return string.Empty;
        }

        int randomIndex = random.Next(queries.Length);
        return queries[randomIndex];
    }

    private async Task PerformSearch(ChromeDriver driver, string searchQuery)
    {
        driver.Navigate().GoToUrl("https://www.google.com");
        IWebElement searchBox = driver.FindElement(By.CssSelector("input[name='q']"));
        searchBox.Clear();
        searchBox.SendKeys(searchQuery);
        searchBox.Submit();

        // Добавьте код для ожидания загрузки страницы результатов поиска, если необходимо
    }

    private async Task SpendRandomTime()
    {
        int time = random.Next(configuration.MinTimeSpent, configuration.MaxTimeSpent + 1);
        await Task.Delay(time * 1000);
    }

    private async Task ScrollPageSmoothly(ChromeDriver driver, bool scrollUp = false)
    {
        Actions actions = new Actions(driver);
        int scrollDistance = scrollUp ? -1000 : 1000;
        actions.MoveByOffset(0, scrollDistance);
        actions.Perform();
        await Task.Delay(1000);
    }

    private async Task ReturnToGoogleSearch(ChromeDriver driver)
    {
        driver.Navigate().GoToUrl("https://www.google.com");
        await Task.Delay(2000);
    }
}
