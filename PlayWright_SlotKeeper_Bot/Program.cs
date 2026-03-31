using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;

class Program
{
    static async Task Main()
    {

        string botToken = "8710843756:AAGBdfHxcFrfLz2a32olOAJQgBD50OG0o6Q";
        string chatId = "631232411";

        using (var httpClient = new HttpClient())
        {
            await httpClient.GetAsync($"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString("Bot has access to tg")}");
        }

        List<string> urls = new List<string>
        {
            "https://calendly.com/tesliuk-volodymyr-lll/10",
            "https://calendly.com/kasyk3/labs"
        };

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        Console.WriteLine("Bot monitors. It will send you a message when any slot will be availible.");

        while (true)
        {
            foreach (var url in urls)
            {
                try
                {
                    await page.GotoAsync(url);
                    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });

                    await page.WaitForTimeoutAsync(5000);

                    var availableDays = await page.Locator("button[data-component='calendar-day']:not([disabled])").CountAsync();

                    if (availableDays > 0)
                    {
                        Console.WriteLine($"!!! SLOT FOUND: {url}");

                        using var httpClient = new HttpClient();
                        string message = $"A slot is available here: {url}";

                        string tgUrl = $"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={chatId}&text={Uri.EscapeDataString(message)}";

                        await httpClient.GetAsync(tgUrl);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during checking {url}: {ex.Message}");
                }

                await Task.Delay(10000);
            }

            await Task.Delay(60000);
        }
    }
}