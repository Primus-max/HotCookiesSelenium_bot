using HotCookies;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
using OpenQA.Selenium.Support.UI;
using PuppeteerSharp;
using Serilog;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class SearchBot
{
    private static readonly Random random = new Random();

    private ConfigurationModel? configuration;    
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
                            driver.Url = "https://www.google.com";

                            var searchTask = PerformSearch(driver, GetRandomSearchQuery());
                            await SpendRandomTime();
                            await ClickRandomLink(driver);


                            var closeTask = CloseBrowser(driver);

                            await Task.WhenAll(searchTask, closeTask);

                            //serverSemaphore.Release(); // Освобождаем доступ к серверу
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
        try
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
        catch (Exception ex)
        {
            logger.Error($"Произошла ошибка в методе CloseBrowser {ex}");
        }
    }

    private async Task PerformSearch(IWebDriver driver, string searchQuery)
    {
        try
        {
            var searchInput = driver.FindElement(By.CssSelector("input[name='q']"));
            searchInput.SendKeys(Keys.End);

            var inputValue = searchInput.GetAttribute("value");
            for (int i = 0; i < inputValue.Length; i++)
            {
                try
                {
                    searchInput.SendKeys(Keys.Backspace);

                    Random randomDelay = new Random();
                    int typeDelay = randomDelay.Next(200, 700);
                    await Task.Delay(typeDelay);
                }
                catch (Exception ex)
                {
                    logger.Error($"Ошибка в методе PerformSearch {ex}");
                }
            }

            try
            {
                searchInput.SendKeys(searchQuery);
                searchInput.SendKeys(Keys.Enter);
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {
                logger.Error($"Ошибка в методе PerformSearch {ex}");
            }
        }
        catch (Exception ex)
        {
            // Обработка ошибок, возникающих при выполнении операций внутри метода PerformSearch
            logger.Error($"Ошибка в методе PerformSearch {ex}");
            // Логирование ошибки или предпринятие других действий по обработке ошибки
        }
    }


    private async Task ClickRandomLink(IWebDriver driver)
    {

        try
        {
            var clickedLinks = new List<string>();
            int maxSiteVisitCount = configuration.MaxSiteVisitCount;

            while (clickedLinks.Count < maxSiteVisitCount)
            {
                try
                {
                    var linkElements = driver.FindElements(By.CssSelector(".A9xod.ynAwRc.ClLRCd.q8U8x.MBeuO.oewGkc.LeUQr"));
                    if (linkElements.Count == 0)
                    {
                        //await PerformSearch(page, GetRandomSearchQuery());
                    }

                    foreach (var linkElement in linkElements)
                    {
                        try
                        {
                            var linkText = linkElement.Text;

                            if (!clickedLinks.Contains(linkText))
                            {
                                ((IJavaScriptExecutor)driver).ExecuteScript(@"(element) => {
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

                                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

                                try
                                {
                                    WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromMinutes(5));
                                    wait.Until(driver =>
                                    {
                                        IJavaScriptExecutor jsExecutor = (IJavaScriptExecutor)driver;
                                        jsExecutor.ExecuteScript("arguments[0].click();", linkElement);
                                        return true;
                                    });
                                    //linkElement.Click();
                                }
                                catch (Exception ex)
                                {
                                    string asdf = ex.ToString();
                                }


                                clickedLinks.Add(linkText);

                                await SimulateUserBehavior(driver);

                                driver.Navigate().Back();

                                await Task.Delay(20000);

                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Ошибка в методе ClickRandomLink {ex}");
                            return;
                        }
                    }

                    // Если все ссылки уже были посещены, прокручиваем страницу
                    if (clickedLinks.Count == linkElements.Count)
                    {
                        ((IJavaScriptExecutor)driver).ExecuteScript(@"() => {
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
                        await Task.Delay(3000);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Ошибка в методе ClickRandomLink {ex}");
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Ошибка в методе ClickRandomLink {ex}");
            return;
        }
    }

    private async Task SimulateUserBehavior(IWebDriver driver)
    {
        try
        {
            await Task.Delay(20000);

            int minTimeSpent = configuration.MinTimeSpent;
            int maxTimeSpent = configuration.MaxTimeSpent;

            var randomTime = new Random().Next(minTimeSpent, maxTimeSpent + 1) * 1000; // Преобразуем время в миллисекунды
            var endTime = DateTime.UtcNow.AddMilliseconds(randomTime);

            while (DateTime.UtcNow < endTime)
            {
                try
                {
                    var scrollHeight = (long)((IJavaScriptExecutor)driver).ExecuteScript("return document.body.scrollHeight;");
                    var windowHeight = (long)((IJavaScriptExecutor)driver).ExecuteScript("return window.innerHeight;");
                    var currentScroll = (long)((IJavaScriptExecutor)driver).ExecuteScript("return window.scrollY;");

                    if (currentScroll + windowHeight >= scrollHeight)
                    {
                        // Достигнут нижний конец страницы, прокручиваем вверх
                        await ScrollPageSmoothly(driver, ScrollDirection.Up);
                        await Task.Delay(1000); // Добавляем небольшую задержку между прокрутками
                    }
                    else
                    {
                        // Продолжаем прокручивать вниз
                        await ScrollPageSmoothly(driver, ScrollDirection.Down);
                        await Task.Delay(1500); // Добавляем небольшую задержку между прокрутками
                    }
                }
                catch (Exception ex)
                {
                    // Обработка ошибок, возникающих при симуляции поведения пользователя
                    logger.Error($"Ошибка в методе SimulateUserBehavior {ex}");
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            // Обработка ошибок, возникающих при выполнении операций внутри метода SimulateUserBehavior
            logger.Error($"Ошибка в методе SimulateUserBehavior {ex}");
        }
    }

    private async Task ScrollPageSmoothly(IWebDriver driver, ScrollDirection direction)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            var scrollHeight = (long)jsExecutor.ExecuteScript("return document.body.scrollHeight;");
            var windowHeight = (long)jsExecutor.ExecuteScript("return window.innerHeight;");
            var currentScroll = (long)jsExecutor.ExecuteScript("return window.scrollY;");

            var scrollStep = 150;

            // Задержка между прокруткой

            if (direction == ScrollDirection.Down)
            {
                while (currentScroll + windowHeight < scrollHeight)
                {
                    try
                    {
                        currentScroll += scrollStep;
                        jsExecutor.ExecuteScript($"window.scrollBy(0, {scrollStep});");

                        Random randomDelay = new Random();
                        int scrollDelay = randomDelay.Next(200, 1000);
                        await Task.Delay(scrollDelay);
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
                        jsExecutor.ExecuteScript($"window.scrollBy(0, -{scrollStep});");

                        Random randomDelay = new Random();
                        int scrollDelay = randomDelay.Next(200, 1000);
                        await Task.Delay(scrollDelay);
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

}
