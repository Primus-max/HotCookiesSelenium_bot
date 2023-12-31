﻿using HotCookies;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.Extensions;
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

    public async Task Run(Profile profile)
    {
        try
        {
            SetupLogger();
            LoadConfiguration();

            await serverSemaphore.WaitAsync(); // Ожидаем доступ к серверу
            var driver = await BrowserManager.ConnectBrowserAsync(profile.UserId);
            serverSemaphore.Release(); // Освобождаем доступ к серверу

            if (driver == null)
            {
                return;
            }

            try
            {
                // Открыть новую вкладку
                driver.ExecuteJavaScript("window.open();");

                // Получить список открытых вкладок
                var openTabs = driver.WindowHandles;

                // Проверить, есть ли более одной открытой вкладки
                if (openTabs.Count > 1)
                {
                    // Закрыть все вкладки, кроме первой
                    for (int j = openTabs.Count - 1; j > 0; j--)
                    {
                        driver.SwitchTo().Window(openTabs[j]);
                        driver.Close();
                        await Task.Delay(500);
                    }

                    // Переключиться обратно на первую вкладку
                    driver.SwitchTo().Window(openTabs[0]);
                }

                // Переключиться на новую вкладку
                driver.SwitchTo().Window(driver.WindowHandles.Last());

                // Перейти на сайт google.com
                driver.Url = "https://www.google.com";
            }
            catch (Exception ex)
            {
                logger.Error($"Произошла ошибка в методе Run {ex}");
            }

            // Рандомное количество поисковых запросов
            int minSearchCount = configuration.MinSearchCount;
            int maxSearchCount = configuration.MinSearchCount;
            int curSearchcount = 0;

            var randomSearchCount = new Random().Next(minSearchCount, maxSearchCount);


            while (randomSearchCount != curSearchcount)
            {
                // Получаю input с поиском и вызываю метод для вставки рандомного текста в поиск
                try
                {
                    IWebElement searchInput = driver.FindElement(By.Id("APjFqb"));
                    ClearAndEnterText(searchInput, GetRandomSearchQuery());
                }
                catch (Exception) { }

                SpendRandomTime();
                ClickRandomLink(driver);
                curSearchcount++;
            }


            CloseBrowser(driver);
            Thread.Sleep(3000);
        }
        catch (Exception ex)
        {
            // Обработка ошибок
            logger.Error($"Произошла ошибка в методе Run {ex}");
        }
    }

    private static void CloseBrowser(IWebDriver driver)
    {
        try
        {
            // Перейти на сайт google.com
            driver.Url = "https://www.google.com";
        }
        catch (Exception ex)
        {
            logger.Error($"Перед закрытием не удалось перейти на google.com {ex}");
        }

        try
        {
            // Получаем список идентификаторов всех открытых вкладок
            var windowHandles = driver.WindowHandles;

            // Закрываем каждую вкладку
            foreach (var windowHandle in windowHandles)
            {
                driver.SwitchTo().Window(windowHandle);
                driver.Close();
                Thread.Sleep(500);
            }

            // Закрываем драйвер
            driver.Quit();
        }
        catch (Exception ex)
        {
            logger.Error($"Произошла ошибка в методе CloseBrowser {ex}");
        }
    }

    private void ClickRandomLink(IWebDriver driver)
    {
        try
        {
            var clickedLinks = new List<string>();

            var randomSite = new Random();
            int randomVisitCount = randomSite.Next(configuration.MinSiteVisitCount, configuration.MaxSiteVisitCount);

            while (clickedLinks.Count < randomVisitCount)
            {
                try
                {
                    IList<IWebElement> linkElements = driver.FindElements(By.CssSelector("a[jsname='qOiK6e']"));

                    //var linkElements = driver.FindElements(By.CssSelector(".yuRUbf"));
                    if (linkElements.Count == 0)
                    {
                        // Открыть новую вкладку
                        driver.ExecuteJavaScript("window.open();");

                        // Получить список открытых вкладок
                        var openTabs = driver.WindowHandles;

                        // Проверить, есть ли более одной открытой вкладки
                        if (openTabs.Count > 1)
                        {
                            // Закрыть все вкладки, кроме первой
                            for (int j = openTabs.Count - 1; j > 0; j--)
                            {
                                driver.SwitchTo().Window(openTabs[j]);
                                driver.Close();
                                Thread.Sleep(500);
                            }

                            // Переключиться обратно на первую вкладку
                            driver.SwitchTo().Window(openTabs[0]);
                        }

                        // Переключиться на новую вкладку
                        driver.SwitchTo().Window(driver.WindowHandles.Last());

                        // Перейти на сайт google.com
                        driver.Url = "https://www.google.com";

                        // Получаю input с поиском и вызываю метод для вставки рандомного текста в поиск
                        try
                        {
                            IWebElement searchInput = driver.FindElement(By.Id("APjFqb"));
                            ClearAndEnterText(searchInput, GetRandomSearchQuery());
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                    }

                    if (linkElements.Count > 0)
                    {
                        var random = new Random();
                        var randomIndex = random.Next(0, linkElements.Count);
                        var linkElement = linkElements[randomIndex];

                        string? textLink = string.Empty;

                        try
                        {
                            textLink = linkElement.FindElement(By.CssSelector("h3.LC20lb.MBeuO.DKV0Md")).Text;
                        }
                        catch (Exception) { }

                        if (!clickedLinks.Contains(textLink))
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].scrollIntoView({ behavior: 'smooth', block: 'center' });", linkElement);
                            Thread.Sleep(3000);

                            try
                            {
                                linkElement.Click();
                                // ((IJavaScriptExecutor)driver).ExecuteScript("arguments[0].click();", linkElement);
                                clickedLinks.Add(textLink);
                            }
                            catch (Exception ex)
                            {
                                string textEx = ex.ToString();
                                if (textEx.Contains("timed out after"))
                                {
                                    logger.Error($"Ошибка в методе ClickRandomLink {ex}");
                                    CloseBrowser(driver);
                                    return;
                                }
                                logger.Error($"Ошибка в методе ClickRandomLink {ex}");
                                continue;
                            }

                            SimulateUserBehavior(driver);

                            driver.Navigate().Back();

                            Thread.Sleep(3000);

                        }
                    }

                    // Если все ссылки уже были посещены, прокручиваем страницу
                    if (clickedLinks.Count == linkElements.Count)
                    {
                        int initialUniqueLinksCount = clickedLinks.Count;
                        int scrollHeight = (int)((IJavaScriptExecutor)driver).ExecuteScript("return Math.max(document.documentElement.scrollHeight, document.body.scrollHeight);");
                        int clientHeight = (int)((IJavaScriptExecutor)driver).ExecuteScript("return document.documentElement.clientHeight;");
                        int duration = 3000; // Длительность анимации в миллисекундах
                        int increment = 20; // Шаг прокрутки за один кадр

                        int currentHeight = (int)((IJavaScriptExecutor)driver).ExecuteScript("return window.pageYOffset;");
                        int maxHeight = currentHeight + clientHeight;

                        while (currentHeight < scrollHeight)
                        {
                            ((IJavaScriptExecutor)driver).ExecuteScript("window.scrollBy(0, arguments[0]);", increment);
                            Thread.Sleep(duration / (scrollHeight / increment));
                            currentHeight = (int)((IJavaScriptExecutor)driver).ExecuteScript("return window.pageYOffset;");

                            if (currentHeight + clientHeight > maxHeight)
                            {
                                maxHeight = currentHeight + clientHeight;
                                initialUniqueLinksCount = clickedLinks.Count; // Обновляем начальное количество уникальных ссылок
                            }
                            else if (clickedLinks.Count == initialUniqueLinksCount)
                            {
                                break; // Не было загружено новых элементов, выходим из цикла
                            }
                        }
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

    private static void ClearAndEnterText(IWebElement element, string text)
    {
        Random random = new Random();

        element.Clear(); // Очищаем элемент перед вводом текста

        foreach (char letter in text)
        {
            if (letter == '\b')
            {
                // Если символ является символом backspace, удаляем следующий символ
                element.SendKeys(Keys.Delete);
            }
            else
            {
                // Вводим символ
                element.SendKeys(letter.ToString());
            }

            Thread.Sleep(random.Next(100, 350));  // Добавляем небольшую паузу между вводом каждого символа
        }

        element.Submit();
        Thread.Sleep(random.Next(300, 700));
    }


    private void SimulateUserBehavior(IWebDriver driver)
    {
        try
        {
            Thread.Sleep(5000);

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
                        ScrollPageSmoothly(driver, ScrollDirection.Up);
                        Thread.Sleep(1000); // Добавляем небольшую задержку между прокрутками
                    }
                    else
                    {
                        // Продолжаем прокручивать вниз
                        ScrollPageSmoothly(driver, ScrollDirection.Down);
                        Thread.Sleep(1500); // Добавляем небольшую задержку между прокрутками
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
            return;
        }
    }

    private void ScrollPageSmoothly(IWebDriver driver, ScrollDirection direction)
    {
        try
        {
            var jsExecutor = (IJavaScriptExecutor)driver;
            var scrollHeight = (long)jsExecutor.ExecuteScript("return document.body.scrollHeight;");
            var windowHeight = (long)jsExecutor.ExecuteScript("return window.innerHeight;");
            var currentScroll = (long)jsExecutor.ExecuteScript("return window.scrollY;");

            var scrollStep = 150;

            int minTimeSpent = configuration.MinTimeSpent;
            int maxTimeSpent = configuration.MaxTimeSpent;

            var randomTime = new Random().Next(minTimeSpent, maxTimeSpent + 1) * 1000; // Преобразуем время в миллисекунды
            var endTime = DateTime.UtcNow.AddMilliseconds(randomTime);

            while (DateTime.UtcNow < endTime)
            {
                if (direction == ScrollDirection.Down && currentScroll + windowHeight >= scrollHeight)
                {
                    break; // Достигнут нижний конец страницы, прекращаем прокрутку вниз
                }

                if (direction == ScrollDirection.Up && currentScroll <= 0)
                {
                    break; // Достигнут верхний конец страницы, прекращаем прокрутку вверх
                }

                try
                {
                    currentScroll += (direction == ScrollDirection.Down) ? scrollStep : -scrollStep;
                    jsExecutor.ExecuteScript($"window.scrollBy(0, {(direction == ScrollDirection.Down ? scrollStep : -scrollStep)});");

                    Random randomDelay = new Random();
                    int scrollDelay = randomDelay.Next(200, 1000);
                    Thread.Sleep(scrollDelay);
                }
                catch (Exception ex)
                {
                    // Обработка ошибок, возникающих при прокрутке страницы
                    logger.Error($"Ошибка в методе ScrollPageSmoothly {ex}");
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Ошибка в методе ScrollPageSmoothly {ex}");
            return;
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
            string[]? queries = configuration?.SearchQueries?.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (queries?.Length == 0)
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

    private void SpendRandomTime()
    {
        try
        {
            int time = random.Next(configuration.MinTimeSpent, configuration.MaxTimeSpent + 1);
            Thread.Sleep(time * 1000);
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
