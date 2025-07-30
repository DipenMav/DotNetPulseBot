using System;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot;

class Program
{
    private static readonly string botToken = "8206028548:AAFxsMT7epDdg2Y4B2ia-na9utdJ6FEMi4c"; // Your bot token
    private static readonly string channelUsername = "@dotnetdrops"; // Your public Telegram channel

    static async Task Main(string[] args)
    {
        var botClient = new TelegramBotClient(botToken);

        string feedUrl = "https://devblogs.microsoft.com/dotnet/feed/";

        using (var reader = XmlReader.Create(feedUrl))
        {
            var feed = SyndicationFeed.Load(reader);

            var latestItems = feed.Items.Take(3); // Send top 3 latest items

            foreach (var item in latestItems)
            {
                string title = item.Title.Text;
                string link = item.Links.FirstOrDefault()?.Uri.ToString();
                string summary = StripHtml(item.Summary?.Text ?? "No summary available");

                string message = $"📰 <b>{EscapeHtml(title)}</b>\n\n{EscapeHtml(summary)}\n\n🔗 <a href=\"{link}\">Read more</a>";

                await botClient.SendTextMessageAsync(
                    chatId: channelUsername,
                    text: message,
                    parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
                );
            }

            Console.WriteLine("✅ Successfully posted to @dotnetdrops!");
        }
    }

    // Remove HTML tags
    static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
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