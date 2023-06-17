using HotCookies;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.Extensions;
using PuppeteerSharp;
using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public class SearchBot
{
    private static readonly Random random = new Random();

    private ConfigurationModel? configuration;
    Browser browser = null;
    private static readonly SemaphoreSlim serverSemaphore = new SemaphoreSlim(1, 1);

    private static readonly ILogger logger = Log.ForContext<SearchBot>();


    // Настройка логгера

    public async Task Run()
    {
        SetupLogger();

        try
        {
            LoadConfiguration();

            await serverSemaphore.WaitAsync(); // Ожидаем доступ к серверу
            List<Profile> profiles = await ProfileManager.GetProfiles();
            serverSemaphore.Release(); // Освобождаем доступ к серверу

            // Поиск профилей по группе
            List<Profile> selectedProfiles = profiles.Where(p => p.GroupName == configuration?.ProfileGroupName).ToList();
            if (selectedProfiles.Count == 0)
            {
                return;
            }

            List<Task> tasks = new List<Task>();

            foreach (Profile profile in selectedProfiles)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (profile is null) return;

                        await serverSemaphore.WaitAsync(); // Ожидаем доступ к серверу
                        var driver = await BrowserManager.ConnectBrowserAsync(profile.UserId);
                        serverSemaphore.Release(); // Освобождаем доступ к серверу

                        if (driver == null)
                        {
                            return;
                        }

                        var random = new Random();
                        int randomVisitCount = random.Next(configuration.MinSiteVisitCount, configuration.MaxSiteVisitCount);

                        for (int i = 0; i < randomVisitCount; i++)
                        {
                            // Открыть новую вкладку
                            driver.ExecuteJavaScript("window.open();");

                            // Переключиться на новую вкладку
                            driver.SwitchTo().Window(driver.WindowHandles.Last());

                            // Перейти на сайт google.com
                            driver.Url = "https://www.youtube.com";

                            //await PerformSearch(driver, GetRandomSearchQuery());
                            //await SpendRandomTime();
                            //await ClickRandomLink(driver);

                            await serverSemaphore.WaitAsync(); // Ожидаем доступ к серверу перед закрытием страницы
                            await CloseBrowser(driver);
                            serverSemaphore.Release(); // Освобождаем доступ к серверу
                        }

                        await serverSemaphore.WaitAsync(); // Ожидаем доступ к серверу перед закрытием браузера
                        driver.Quit();
                        serverSemaphore.Release(); // Освобождаем доступ к серверу
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Произошла ошибка в методе Run {ex}");
                        // Обработка ошибок
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            // Обработка ошибок
            logger.Error($"Произошла ошибка в методе Run {ex}");
        }
    }

    private async Task CloseBrowser(IWebDriver driver)
    {
        // Получаем список идентификаторов всех открытых вкладок
        var windowHandles = driver.WindowHandles;

        // Закрываем каждую вкладку
        foreach (var windowHandle in windowHandles)
        {
            driver.SwitchTo().Window(windowHandle);
            driver.Close();
        }

        // Закрываем драйвер
        driver.Quit();
    }


    //private async Task PerformSearch(IWebDriver page, string searchQuery)
    //{
    //    try
    //    {
    //        await page.WaitForSelectorAsync("input[name='q']");
    //        await page.FocusAsync("input[name='q']");
    //        await page.Keyboard.PressAsync("End");

    //        var inputValue = await page.EvaluateExpressionAsync<string>("document.querySelector('input[name=\"q\"]').value");
    //        for (int i = 0; i < inputValue.Length; i++)
    //        {
    //            try
    //            {
    //                await page.Keyboard.PressAsync("Backspace");

    //                Random randomDelay = new Random();
    //                int typeDelay = randomDelay.Next(200, 700);
    //                await page.WaitForTimeoutAsync(typeDelay);
    //            }
    //            catch (Exception ex)
    //            {                    
    //                logger.Error($"Ошибка в методе PerformSearch {ex}");                    
    //            }
    //        }

    //        try
    //        {
    //            await page.TypeAsync("input[name='q']", searchQuery);
    //            await page.Keyboard.PressAsync("Enter");
    //            await page.WaitForTimeoutAsync(2000);
    //        }
    //        catch (Exception ex)
    //        {                
    //            logger.Error($"Ошибка в методе PerformSearch {ex}");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        // Обработка ошибок, возникающих при выполнении операций внутри метода PerformSearch
    //        logger.Error($"Ошибка в методе PerformSearch {ex}");
    //        // Логирование ошибки или предпринятие других действий по обработке ошибки
    //    }
    //}

    private async Task ClickRandomLink(IPage page)
    {
        try
        {
            var clickedLinks = new List<string>();
            int maxSiteVisitCount = configuration.MaxSiteVisitCount;

            while (clickedLinks.Count < maxSiteVisitCount)
            {
                try
                {
                    var linkElements = await page.QuerySelectorAllAsync(".A9xod.ynAwRc.ClLRCd.q8U8x.MBeuO.oewGkc.LeUQr");
                    if (linkElements.Length == 0)
                    {
                        //await PerformSearch(page, GetRandomSearchQuery());
                    }

                    foreach (var linkElement in linkElements)
                    {
                        try
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

                                await page.WaitForTimeoutAsync(5000);

                                await linkElement.ClickAsync();

                                clickedLinks.Add(linkText);

                               // await SimulateUserBehavior(page);


                                //try
                                //{
                                //    await page.GoToAsync("https://www.google.com");
                                //}
                                //catch (Exception)
                                //{

                                //    MessageBox.Show($"НЕ МОГУ ПЕРЕЙТИ!");
                                //}

                                //await page.WaitForTimeoutAsync(10000);

                                //if (!page.IsClosed)
                                //{
                                //    await page.GoBackAsync();
                                //}
                                //else
                                //{
                                //    MessageBox.Show($"Потерял связь с реальностью");
                                //}
                                //await page.WaitForTimeoutAsync(10000);
                                //try
                                //{
                                //    await page.GoBackAsync();
                                //}
                                //catch (Exception ex)
                                //{

                                //}


                                page.DefaultNavigationTimeout = 50000;
                                try
                                {
                                    await page.GoBackAsync();
                                }
                                catch (Exception)
                                {
                                    await page.GoToAsync("https://activity.adspower.com");
                                }
                              
                                //int maxRetries = 10;
                                //int retryCount = 0;

                                //while (retryCount < maxRetries)
                                //{
                                //    try
                                //    {
                                //        await page.GoBackAsync();
                                //        break; // Если успешно, выходим из цикла
                                //    }
                                //    catch (Exception)
                                //    {                                        
                                //        retryCount++;
                                //    }
                                //}

                                //if (retryCount == maxRetries)
                                //{
                                   
                                //}

                                await page.WaitForTimeoutAsync(20000);

                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            // проверить Navigation failed because browser has disconnected!
                            // проверить Session closed. Most likely the Page has been closed.Close reason
                            logger.Error($"Ошибка в методе ClickRandomLink {ex}");                                                        
                            return;                            
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
                catch (Exception ex)
                {
                    logger.Error($"Ошибка в методе ClickRandomLink {ex}");                    
                    continue;
                    // Логирование ошибки или предпринятие других действий по обработке ошибки
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Ошибка в методе ClickRandomLink {ex}");
            return;
            // Логирование ошибки или предпринятие других действий по обработке ошибки
        }
    }

    private async Task SimulateUserBehavior(IPage page)
    {
        try
        {
            await page.WaitForTimeoutAsync(20000);

            int minTimeSpent = configuration.MinTimeSpent;
            int maxTimeSpent = configuration.MaxTimeSpent;

            var randomTime = new Random().Next(minTimeSpent, maxTimeSpent + 1) * 1000; // Преобразуем время в миллисекунды
            var endTime = DateTime.UtcNow.AddMilliseconds(randomTime);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    var scrollHeight = await page.EvaluateExpressionAsync<int>("document.body.scrollHeight");
                    var windowHeight = await page.EvaluateExpressionAsync<int>("window.innerHeight");
                    var currentScroll = await page.EvaluateExpressionAsync<int>("window.scrollY");

                    if (currentScroll + windowHeight >= scrollHeight)
                    {
                        // Достигнут нижний конец страницы, прокручиваем вверх
                        //await ScrollPageSmoothly(page, ScrollDirection.Up);
                        await page.WaitForTimeoutAsync(1000); // Добавляем небольшую задержку между прокрутками
                    }
                    else
                    {
                        // Продолжаем прокручивать вниз
                        //await ScrollPageSmoothly(page, ScrollDirection.Down);
                        await page.WaitForTimeoutAsync(1500); // Добавляем небольшую задержку между прокрутками
                    }
                }
                catch (Exception ex)
                {
                    // Обработка ошибок, возникающих при симуляции поведения пользователя
                    logger.Error($"Ошибка в методе SimulateUserBehavior {ex}");
                    continue;
                    // Логирование ошибки или предпринятие других действий по обработке ошибки
                }
            }
        }
        catch (Exception ex)
        {
            // Обработка ошибок, возникающих при выполнении операций внутри метода SimulateUserBehavior
            logger.Error($"Ошибка в методе SimulateUserBehavior {ex}");
        }
    }

    private async Task ScrollPageSmoothly(IPage page, ScrollDirection direction)
    {
        try
        {
            
            var scrollHeight = await page.EvaluateExpressionAsync<int>("document.body.scrollHeight");
            var windowHeight = await page.EvaluateExpressionAsync<int>("window.innerHeight");
            var currentScroll = await page.EvaluateExpressionAsync<int>("window.scrollY");

            var scrollStep = 150;

            // Задержка между прокруткой

            if (direction == ScrollDirection.Down)
            {
                while (currentScroll + windowHeight < scrollHeight)
                {
                    try
                    {
                        currentScroll += scrollStep;
                        await page.EvaluateFunctionAsync(@"(scrollStep) => {
                        window.scrollBy(0, scrollStep);
                    }", scrollStep);

                        Random randomDelay = new Random();
                        int scrollDelay = randomDelay.Next(200, 1000);
                        await page.WaitForTimeoutAsync(scrollDelay);
                    }
                    catch (Exception ex)
                    {
                        // Обработка ошибок, возникающих при прокрутке страницы вниз
                        logger.Error($"Ошибка в методе ScrollPageSmoothly {ex}");
                        continue;                       
                    }
                }
            }
            else if (direction == ScrollDirection.Up)
            {
                while (currentScroll > 0)
                {
                    try
                    {
                        currentScroll -= scrollStep;
                        await page.EvaluateFunctionAsync(@"(scrollStep) => {
                        window.scrollBy(0, -scrollStep);
                    }", scrollStep);

                        Random randomDelay = new Random();
                        int scrollDelay = randomDelay.Next(200, 1000);
                        await page.WaitForTimeoutAsync(scrollDelay);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Ошибка в методе ScrollPageSmoothly {ex}");                        
                        continue;                        
                    }
                }
            }
        }
        catch (Exception ex)
        {           
            logger.Error($"Ошибка в методе ScrollPageSmoothly {ex}");         
        }
    }

    private enum ScrollDirection
    {
        Up,
        Down
    }

    private void LoadConfiguration()
    {
        try
        {
            string json = File.ReadAllText("config.json");
            configuration = JsonConvert.DeserializeObject<ConfigurationModel>(json);
        }
        catch (Exception ex)
        {
            // Обработка ошибки при загрузке и десериализации конфигурации
            logger.Error($"Ошибка в методе LoadConfiguration {ex}");
            // Логирование ошибки или предпринятие других действий по обработке ошибки
        }
    }

    private string GetRandomSearchQuery()
    {
        try
        {
            string[] queries = configuration.SearchQueries?.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (queries.Length == 0)
            {                
                return string.Empty;
            }

            int randomIndex = random.Next(queries.Length);
            return queries[randomIndex];
        }
        catch (Exception ex)
        {           
            logger.Error($"Ошибка в методе GetRandomSearchQuery {ex}");            
            return string.Empty; 
        }
    }

    private async Task SpendRandomTime()
    {
        try
        {
            int time = random.Next(configuration.MinTimeSpent, configuration.MaxTimeSpent + 1);
            await Task.Delay(time * 1000);
        }
        catch (Exception ex)
        {
            logger.Error($"Ошибка в методе SpendRandomTime {ex}");
            // Логирование ошибки или предпринятие других действий по обработке ошибки
        }
    }

    private void SetupLogger()
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File("logs.txt") // Укажите путь к файлу логов
            .CreateLogger();
    }

    private async Task ClosePagesAndBrowser(IPage page)
    {
        await serverSemaphore.WaitAsync(); // Ожидаем доступ к серверу перед закрытием браузера
        var pages = await browser.PagesAsync();
        foreach (var p in pages)
        {
            await p.CloseAsync();
            await page.WaitForTimeoutAsync(500);
        }
        await page.WaitForTimeoutAsync(500);
        await browser.CloseAsync();

        serverSemaphore.Release(); // Освобождаем доступ к серверу
    }
}
