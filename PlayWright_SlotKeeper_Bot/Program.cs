using Microsoft.Playwright;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using System.Linq;

class Program
{
    static async Task Main()
    {
        string botToken = "8710843756:AAGBdfHxcFrfLz2a32olOAJQgBD50OG0o6Q";
        HashSet<string> chatIds = new HashSet<string>();
        int lastUpdateId = 0;

        List<string> urls = new List<string>
        {
            "https://calendly.com/tesliuk-volodymyr-lll/10",
            "https://calendly.com/kasyk3/labs"
        };

        bool[] visited = new bool[urls.Count];
        DateTime lastResetDate = DateTime.Now.Date;

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        var context = await browser.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        var page = await context.NewPageAsync();
        Console.WriteLine($"[{DateTime.Now}] Bot started working...");

        while (true)
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetStringAsync($"https://api.telegram.org/bot{botToken}/getUpdates?offset={lastUpdateId + 1}");
                using var doc = JsonDocument.Parse(response);
                foreach (var update in doc.RootElement.GetProperty("result").EnumerateArray())
                {
                    lastUpdateId = update.GetProperty("update_id").GetInt32();
                    if (update.TryGetProperty("message", out var message))
                    {
                        string fromId = message.GetProperty("chat").GetProperty("id").GetRawText();
                        string text = message.TryGetProperty("text", out var t) ? t.GetString() : "";

                        if (text == "/start" && chatIds.Add(fromId))
                        {
                            await client.GetAsync($"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={fromId}&text={Uri.EscapeDataString("Monitoring started")}");
                        }
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"Update Error: {ex.Message}"); }

            if (DateTime.Now.Date > lastResetDate)
            {
                for (int i = 0; i < visited.Length; i++) visited[i] = false;
                lastResetDate = DateTime.Now.Date;
            }

            if (chatIds.Any())
            {
                for (int i = 0; i < urls.Count; i++)
                {
                    if (visited[i]) continue;

                    string url = urls[i];
                    try
                    {
                        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });
                        await page.WaitForSelectorAsync("button", new PageWaitForSelectorOptions { Timeout = 20000 });
                        await page.WaitForTimeoutAsync(3000);

                        var availableDays = await page.Locator("button[data-component='calendar-day']:not([disabled]):not([aria-disabled='true'])").AllAsync();

                        if (availableDays.Count == 0)
                        {
                            availableDays = await page.Locator("td button:not([disabled]):not([aria-disabled='true'])").AllAsync();
                        }

                        if (availableDays.Count > 0)
                        {
                            Console.WriteLine($"!!!SLOT FOUND!!!: {url}");
                            visited[i] = true;

                            using var client = new HttpClient();
                            string message = $"!!!SLOT FOUND!!!\nLink: {url}";
                            foreach (var id in chatIds)
                            {
                                await client.GetAsync($"https://api.telegram.org/bot{botToken}/sendMessage?chat_id={id}&text={Uri.EscapeDataString(message)}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking {url}: {ex.Message}");
                    }
                    await Task.Delay(5000);
                }
            }

            await Task.Delay(10000);
        }
    }
}