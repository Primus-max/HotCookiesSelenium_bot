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
using System.Windows.Controls;
using System.Windows.Input;

public class SearchBot
{
    private static readonly Random random = new Random();

    private ConfigurationModel? configuration;

    public async Task Run()
    {
        LoadConfiguration();

        List<Profile> profiles = await ProfileManager.GetProfiles();

        // Поиск профилей по группе
        List<Profile> selectedProfiles = profiles.Where(p => p.GroupName == configuration?.ProfileGroupName).ToList();
        if (selectedProfiles.Count == 0)
        {
            Console.WriteLine($"No profiles found in group {configuration.ProfileGroupName}.");
            return;
        }

        foreach (Profile profile in selectedProfiles)
        {
            if (profile is null) continue;

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
                               

                for (int i = 0; i < configuration?.RepeatCount; i++)
                {
                    string searchQuery = GetRandomSearchQuery();
                    await PerformSearch(page, searchQuery);
                    await SpendRandomTime();

                    await ClickRandomLink(page);




                    //await ScrollPageSmoothly(page);
                    //await ScrollPageSmoothly(page, true);
                    //await ReturnToGoogleSearch(page);
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

    private async Task ClickRandomLink(IPage page)
    {
        // Ждём загрузки DOM
       // await page.WaitForNavigationAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle0 } });      

        var linkElements = await page.QuerySelectorAllAsync(".A9xod.ynAwRc.ClLRCd.q8U8x.MBeuO.oewGkc.LeUQr");
        var allLinks = linkElements.Cast<ElementHandle>().ToList();

        int minSiteVisitCount = configuration.MinSiteVisitCount;
        int maxSiteVisitCount = configuration.MaxSiteVisitCount;

        while (allLinks.Count < maxSiteVisitCount + 1)
        {
            await page.EvaluateFunctionAsync(@"() => {
            window.scrollBy(0, 200); // Прокрутка страницы на 200 пикселей вниз
        }");
            await page.WaitForTimeoutAsync(1000); // Добавьте задержку для плавной прокрутки

            linkElements = await page.QuerySelectorAllAsync(".A9xod.ynAwRc.ClLRCd.q8U8x.MBeuO.oewGkc.LeUQr");
            allLinks.AddRange(linkElements.Cast<ElementHandle>());
        }

        var siteVisitCount = Math.Min(new Random().Next(minSiteVisitCount, maxSiteVisitCount + 1), allLinks.Count);
        for (var i = 0; i < siteVisitCount; i++)
        {
            var randomLinkIndex = new Random().Next(0, allLinks.Count);
            var randomLink = allLinks[randomLinkIndex];

            await page.EvaluateFunctionAsync(@"(element) => {
            element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        }", randomLink);

            await page.WaitForTimeoutAsync(1000);

            await randomLink.ClickAsync();

            await SimulateUserBehavior(page);
        }
    }

    private async Task SimulateUserBehavior(IPage page)
    {
        await page.WaitForNavigationAsync();

        int minTimeSpent = configuration.MinTimeSpent;
        int maxTimeSpent = configuration.MaxTimeSpent;

        var randomTime = new Random().Next(minTimeSpent, maxTimeSpent + 1) * 1000; // Преобразуем время в миллисекунды
        var endTime = DateTime.UtcNow.AddMilliseconds(randomTime);

        while (DateTime.UtcNow < endTime)
        {
            var scrollHeight = await page.EvaluateExpressionAsync<int>("document.body.scrollHeight");
            var windowHeight = await page.EvaluateExpressionAsync<int>("window.innerHeight");
            var currentScroll = await page.EvaluateExpressionAsync<int>("window.scrollY");

            if (currentScroll + windowHeight >= scrollHeight)
            {
                // Достигнут нижний конец страницы, прокручиваем вверх
                await ScrollPageSmoothly(page, ScrollDirection.Up);
                await page.WaitForTimeoutAsync(500); // Добавляем небольшую задержку между прокрутками
            }
            else
            {
                // Продолжаем прокручивать вниз
                await ScrollPageSmoothly(page, ScrollDirection.Down);
                await page.WaitForTimeoutAsync(500); // Добавляем небольшую задержку между прокрутками
            }
        }
    }

    private async Task ScrollPageSmoothly(IPage page, ScrollDirection direction)
    {
        if (page.IsClosed)
        {
            return; // Прекращаем выполнение, если страница закрыта
        }

        var scrollHeight = await page.EvaluateExpressionAsync<int>("document.body.scrollHeight");
        var windowHeight = await page.EvaluateExpressionAsync<int>("window.innerHeight");
        var currentScroll = await page.EvaluateExpressionAsync<int>("window.scrollY");

        var scrollStep = 200;
        var scrollDelay = 200; // Задержка между прокруткой

        if (direction == ScrollDirection.Down)
        {
            while (currentScroll + windowHeight < scrollHeight)
            {
                currentScroll += scrollStep;
                await page.EvaluateFunctionAsync(@"(scrollStep) => {
                window.scrollBy(0, scrollStep);
            }", scrollStep);

                await page.WaitForTimeoutAsync(scrollDelay);
            }
        }
        else if (direction == ScrollDirection.Up)
        {
            while (currentScroll > 0)
            {
                currentScroll -= scrollStep;
                await page.EvaluateFunctionAsync(@"(scrollStep) => {
                window.scrollBy(0, -scrollStep);
            }", scrollStep);

                await page.WaitForTimeoutAsync(scrollDelay);
            }
        }
    }



    private enum ScrollDirection
    {
        Up,
        Down
    }


    //private async Task ScrollPageSmoothly(IPage page, bool scrollUp = false)
    //{
    //    int scrollDistance = scrollUp ? -1000 : 1000;
    //    await page.EvaluateExpressionAsync($"window.scrollBy(0, {scrollDistance});");
    //    await page.WaitForTimeoutAsync(1000);
    //}

    //private async Task ReturnToGoogleSearch(IPage page)
    //{
    //    await page.GoToAsync("https://www.google.com");
    //    await page.WaitForTimeoutAsync(2000);
    //}

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
