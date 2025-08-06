using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace DotNetPulseBot.Models
{
    [Table("posted_articles")]
    public class PostedArticle : BaseModel
    {
        [PrimaryKey("id", false)] // Set 'true' if Supabase auto-generates the id
        public Guid Id { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("url")]
        public string Url { get; set; }

        [Column("published_at")]
        public DateTime PublishedAt { get; set; }

        public PostedArticle()
        {
            Id=Guid.NewGuid();
        }

    }
}