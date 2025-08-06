using System;
using System.Linq;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using System.Xml;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Supabase;
using DotNetPulseBot.Models;
using System.Collections.Generic;
using System.Text.RegularExpressions;

class Program
{
    public static string botToken = "8206028548:AAFxsMT7epDdg2Y4B2ia-na9utdJ6FEMi4c"; // Your bot token
    public static string channelUsername = "@dotnetdrops"; // Your public Telegram channel
    // static string? botToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
    // static string? channelUsername = Environment.GetEnvironmentVariable("CHANNEL_USERNAME");
    public static string supabaseUrl = "https://bzutdbajwcokhjkstqzg.supabase.co";
    public static string supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJ6dXRkYmFqd2Nva2hqa3N0cXpnIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NTM4Nzg4NzEsImV4cCI6MjA2OTQ1NDg3MX0.ggGv9x8cVsm9yLjsT24qG3JKPbHt9yNnpjZ1PMk0a5I";
    // Load from environment variables
    //private static string botToken = Environment.GetEnvironmentVariable("TELEGRAM_TOKEN");
    //private static string channelUsername = Environment.GetEnvironmentVariable("CHANNEL_USERNAME");
    //private static string supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
    //private static string supabaseKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

    static async Task Main(string[] args)
    {
        if (string.IsNullOrEmpty(botToken) || string.IsNullOrEmpty(channelUsername) ||
            string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
        {
            Console.WriteLine("❌ One or more environment variables are not set.");
            return;
        }

        var supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey);
        await supabaseClient.InitializeAsync();

        var botClient = new TelegramBotClient(botToken);

        List<string> feedUrls = new List<string>
        {
            "https://devblogs.microsoft.com/dotnet/feed/",
            "https://devblogs.microsoft.com/dotnet/tag/ml-net/feed/",
            "https://devblogs.microsoft.com/xamarin/feed/",
            "https://devblogs.microsoft.com/aspnet/feed/",
            "https://devblogs.microsoft.com/nuget/feed/",
            //"https://azurecomcdn.azureedge.net/en-us/blog/feed/",
            //"https://techcommunity.microsoft.com/gxcuf89792/rss/azure",
            //"https://azure.microsoft.com/en-us/updates/feed/",
            //"https://dev.to/feed/tag/dotnet",
            "https://medium.com/feed/tag/dotnet",
            "https://hnrss.org/newest?q=dotnet+azure+cloud+ml",
            //"https://openai.com/feed.xml",
            //"https://techcommunity.microsoft.com/gxcuf89792/rss/Microsoft-AI",
            "https://github.blog/feed/",
            //"https://feeds.feedburner.com/ScottHanselman", // Hanselman's blog
            "https://weblog.west-wind.com/rss.aspx", // Rick Strahl (.NET MVP)
            "https://www.thinktecture.com/feed/", // Thinktecture team blog
            //"https://www.infoq.com/dotnet/rss", // .NET on InfoQ
            "https://codeopinion.com/feed/" // Domain-driven design (.NET)
        };

        // Fetch articles from the provided feed URLs
        var allNewArticles = new List<SyndicationItem>();

        foreach (var feedUrl in feedUrls)
        {
            try
            {
                using var reader = XmlReader.Create(feedUrl);
                var feed = SyndicationFeed.Load(reader);
                if (feed != null && feed.Items != null)
                {
                    allNewArticles.AddRange(feed.Items);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching feed {feedUrl}: {ex.Message}");
            }
        }

        // Step 1: Get posted URLs from Supabase
        var posted = await supabaseClient
            .From<PostedArticle>()
            .Get();

        var postedUrls = posted.Models
            .Select(x => x.Url.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Step 2: Filter and check duplicates
        var newArticles = allNewArticles
            .Where(item =>
            {
                var url = item.Links.FirstOrDefault()?.Uri.ToString().TrimEnd('/');
                return url != null && !postedUrls.Contains(url) && item.PublishDate > DateTime.UtcNow.AddDays(-7); // Filter out articles older than 7 days
            })
            .OrderByDescending(item => item.PublishDate)
            .Take(3) // Limit to 3 articles
            .ToList();

        if (newArticles.Count == 0)
        {
            Console.WriteLine("ℹ️ No new articles found.");
            return;
        }

        // Post new articles to Telegram and save them to Supabase
        foreach (var item in newArticles)
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

            var article = new PostedArticle
            {
                Id = Guid.NewGuid(),
                Title = title,
                Url = link,
                PublishedAt = item.PublishDate.UtcDateTime
            };

            try { 

            await supabaseClient.From<PostedArticle>().Insert(article);
            Console.WriteLine($"✅ Posted and saved: {title}");
            }
            catch (Supabase.Postgrest.Exceptions.PostgrestException ex)
            when (ex.Message.Contains("duplicate key"))
            {
                // skip duplicates without crashing
                Console.WriteLine($"⚠️ Skipped duplicate URL: {link}");
            }
        }
    }
    }

    static string StripHtml(string html) =>
        Regex.Replace(html, "<.*?>", string.Empty);

    static string EscapeHtml(string input) =>
        input.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
}
