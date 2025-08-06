using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;


namespace DotNetPulseBot.Models
{ 

[Table("failed_articles")]
public class FailedArticle : BaseModel
{
    [PrimaryKey("url", false)]
    public string Url { get; set; }

    [Column("reason")]
    public string Reason { get; set; }

    [Column("failed_at")]
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}
}
