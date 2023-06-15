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
    Browser browser = null;
    public async Task Run()
    {
        try
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
                try
                {
                    if (profile is null) continue;

                    browser = await BrowserManager.ConnectBrowser(profile.UserId);

                    if (browser == null)
                    {
                        Console.WriteLine($"Failed to connect browser for profile {profile.UserId}.");
                        continue;
                    }

                    // Открываю новую вкладку и перехожу
                    var page = await browser.NewPageAsync();
                    await page.GoToAsync("https://www.google.com");

                    //var pages = await browser.PagesAsync();

                    //for (int i = pages.Length - 1; i > 0; i--)
                    //{
                    //    await pages[i].CloseAsync();
                    //}

                    //var page = pages[0];
                    //await page.GoToAsync("https://www.google.com");


                    Random random = new Random();
                    int randomVisitCount = random.Next(configuration.MinSiteVisitCount, configuration.MaxSiteVisitCount);

                    for (int i = 0; i < randomVisitCount; i++)
                    {
                        string searchQuery = GetRandomSearchQuery();
                        await PerformSearch(page, searchQuery);
                        await SpendRandomTime();

                        await ClickRandomLink(page);

                    }

                    var pages = await browser.PagesAsync();

                    for (int i = pages.Length - 1; i > 0; i--)
                    {
                        await pages[i].CloseAsync();
                        await page.WaitForTimeoutAsync(500);
                    }

                    await page.WaitForTimeoutAsync(1000);

                    if (browser != null)
                        await browser.CloseAsync();
                }
                catch (Exception ex)
                {
                    // Обработка ошибок, возникающих внутри цикла обработки профилей
                   
                    // Логирование ошибки или предпринятие других действий по обработке ошибки
                }
            }
        }
        catch (Exception ex)
        {
            // Обработка ошибок, возникающих внутри метода Run
            
            // Логирование ошибки или предпринятие других действий по обработке ошибки
        }
    }


    private async Task PerformSearch(IPage page, string searchQuery)
    {
        await page.WaitForSelectorAsync("input[name='q']");
        await page.FocusAsync("input[name='q']");
        await page.Keyboard.PressAsync("End");

        var inputValue = await page.EvaluateExpressionAsync<string>("document.querySelector('input[name=\"q\"]').value");
        for (int i = 0; i < inputValue.Length; i++)
        {
            await page.Keyboard.PressAsync("Backspace");

            Random randomDelay = new Random();
            int typeDelay = randomDelay.Next(200, 700);
            await page.WaitForTimeoutAsync(typeDelay);
        }

        await page.TypeAsync("input[name='q']", searchQuery);
        await page.Keyboard.PressAsync("Enter");
    }

    private async Task ClickRandomLink(IPage page)
    {
        var clickedLinks = new List<string>();
        int maxSiteVisitCount = configuration.MaxSiteVisitCount;

        while (clickedLinks.Count < maxSiteVisitCount)
        {
            var linkElements = await page.QuerySelectorAllAsync(".A9xod.ynAwRc.ClLRCd.q8U8x.MBeuO.oewGkc.LeUQr");

            foreach (var linkElement in linkElements)
            {
                var linkText = await linkElement.EvaluateFunctionAsync<string>("el => el.innerText");

                if (!clickedLinks.Contains(linkText))
                {
                    await page.EvaluateFunctionAsync(@"(element) => {
                    const y = element.getBoundingClientRect().top + window.pageYOffset;
                    const duration = 1000; // Длительность анимации в миллисекундах
                    const increment = 20; // Шаг прокрутки за один кадр

                    const scrollToY = (to, duration) => {
                        if (duration <= 0) return;
                        const difference = to - window.pageYOffset;
                        const perTick = difference / duration * increment;

                        setTimeout(() => {
                            window.scrollBy(0, perTick);
                            if (window.pageYOffset === to) return;
                            scrollToY(to, duration - increment);
                        }, increment);
                    }

                    scrollToY(y, duration);
                }", linkElement);

                    await page.WaitForTimeoutAsync(2000);

                    await linkElement.ClickAsync();

                    clickedLinks.Add(linkText);

                    await SimulateUserBehavior(page);

                    await page.GoBackAsync();

                    await page.WaitForTimeoutAsync(2000);

                    break; // Прерываем цикл после успешного клика
                }
            }

            // Если все ссылки уже были посещены, прокручиваем страницу
            if (clickedLinks.Count == linkElements.Length)
            {
                await page.EvaluateFunctionAsync(@"() => {
                const scrollHeight = Math.max(document.documentElement.scrollHeight, document.body.scrollHeight);
                const clientHeight = document.documentElement.clientHeight;
                const duration = 3000; // Длительность анимации в миллисекундах
                const increment = 20; // Шаг прокрутки за один кадр

                const scrollToBottom = (duration) => {
                    if (duration <= 0) return;
                    const difference = scrollHeight - window.pageYOffset - clientHeight;
                    const perTick = difference / duration * increment;

                    setTimeout(() => {
                        window.scrollBy(0, perTick);
                        if (window.pageYOffset + clientHeight === scrollHeight) return;
                        scrollToBottom(duration - increment);
                    }, increment);
                }

                scrollToBottom(duration);
            }");

                // Ждем, пока страница прокрутится и новые элементы загрузятся
                await page.WaitForTimeoutAsync(3000);
            }
        }
    }

    private async Task SimulateUserBehavior(IPage page)
    {
        await page.WaitForTimeoutAsync(10000);

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
                await page.WaitForTimeoutAsync(1000); // Добавляем небольшую задержку между прокрутками
            }
            else
            {
                // Продолжаем прокручивать вниз
                await ScrollPageSmoothly(page, ScrollDirection.Down);
                await page.WaitForTimeoutAsync(1500); // Добавляем небольшую задержку между прокрутками
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

        var scrollStep = 150;

         // Задержка между прокруткой

        if (direction == ScrollDirection.Down)
        {
            while (currentScroll + windowHeight < scrollHeight)
            {
                currentScroll += scrollStep;
                await page.EvaluateFunctionAsync(@"(scrollStep) => {
                window.scrollBy(0, scrollStep);
            }", scrollStep);

                Random randomDelay = new Random();
                int scrollDelay = randomDelay.Next(200, 1000);
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

                Random randomDelay = new Random();
                int scrollDelay = randomDelay.Next(200, 1000);
                await page.WaitForTimeoutAsync(scrollDelay);
            }
        }
    }

    private enum ScrollDirection
    {
        Up,
        Down
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

    private async Task SpendRandomTime()
    {
        int time = random.Next(configuration.MinTimeSpent, configuration.MaxTimeSpent + 1);
        await Task.Delay(time * 1000);
    }

}
