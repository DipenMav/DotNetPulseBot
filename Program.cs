using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using System.Collections.Generic;

class Program
{
    //private static readonly string botToken = "8206028548:AAFxsMT7epDdg2Y4B2ia-na9utdJ6FEMi4c"; // Your bot token
    //private static readonly string channelUsername = "@dotnetdrops"; // Your public Telegram channel
    static string? botToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN"); // Your bot token
    static string? channelUsername = Environment.GetEnvironmentVariable("CHANNEL_USERNAME"); // Your public Telegram channel
    static string postedLinksFile = "posted_links.txt";

    static async Task Main(string[] args)
    {
        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(channelUsername))
        {
            Console.WriteLine("❌ BOT_TOKEN or CHANNEL_USERNAME environment variables are missing.");
            return;
        }

        var botClient = new TelegramBotClient(botToken);

        while (true)
        {
            try
            {
                string feedUrl = "https://devblogs.microsoft.com/dotnet/feed/";

                using (var reader = XmlReader.Create(feedUrl))
                {
                    var feed = SyndicationFeed.Load(reader);
                    if (feed == null)
                    {
                        Console.WriteLine("❌ Failed to load RSS feed.");
                        continue;
                    }

                    var postedLinks = File.Exists(postedLinksFile)
                        ? File.ReadAllLines(postedLinksFile).ToHashSet()
                        : new HashSet<string>();

                    var newItems = feed.Items
                        .Where(item => item.Links.FirstOrDefault() != null)
                        .Where(item => !postedLinks.Contains(item.Links.First().Uri.ToString()))
                        .Take(3)
                        .ToList();

                    if (newItems.Count == 0)
                    {
                        Console.WriteLine("ℹ️ No new items found.");
                    }

                    foreach (var item in newItems)
                    {
                        string title = item.Title.Text;
                        string link = item.Links.First().Uri.ToString();
                        string summary = StripHtml(item.Summary?.Text ?? "No summary available");

                        string message = $"📰 <b>{EscapeHtml(title)}</b>\n\n{EscapeHtml(summary)}\n\n🔗 <a href=\"{link}\">Read more</a>";

                        await botClient.SendTextMessageAsync(
                            chatId: channelUsername,
                            text: message,
                            parseMode: ParseMode.Html
                        );

                        File.AppendAllLines(postedLinksFile, new[] { link });
                        Console.WriteLine($"✅ Posted: {title}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }

            Console.WriteLine("⏳ Sleeping for 6 hours...");
            await Task.Delay(TimeSpan.FromHours(6));
        }
    }

    // Remove HTML tags from summary
    static string StripHtml(string html)
    {
        return Regex.Replace(html, "<.*?>", string.Empty);
    }

    // Escape HTML-sensitive characters
    static string EscapeHtml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
}