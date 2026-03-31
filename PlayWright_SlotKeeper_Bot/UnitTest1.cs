using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

class Program
{
    static async Task Main()
    {
        List<string> urls = new List<string>
        {
            "https://calendly.com/tesliuk-volodymyr-lll/10",
            "https://calendly.com/kasyk3/labs"
        };

        string botToken = "8710843756:AAGBdfHxcFrfLz2a32olOAJQgBD50OG0o6Q";
        string chatId = "631232411";

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync();
        var page = await browser.NewPageAsync();

        Console.WriteLine("Бот запущено. Починаю моніторинг...");

        while (true)
        {
            foreach (var url in urls)
            {
                try
                {
                    Console.WriteLine($"Перевірка: {url}");
                    await page.GotoAsync(url);
                    await page.WaitForTimeoutAsync(5000);

                    var availableDays = await page.Locator("button[data-component='calendar-day']:not([disabled])").CountAsync();

                    if (availableDays > 0)
                    {
                        Console.WriteLine($"!!! ЗНАЙДЕНО СЛОТ: {url}");

                        using var httpClient = new HttpClient();
                        string message = $"Вільний слот доступний тут: {url}";

                        string tgUrl = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(message)}";

                        await httpClient.GetAsync(tgUrl);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при перевірці {url}: {ex.Message}");
                }

                await Task.Delay(10000);
            }

            await Task.Delay(60000);
        }
    }
}