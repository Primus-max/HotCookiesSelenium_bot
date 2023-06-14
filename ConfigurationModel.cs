namespace HotCookies
{
    public class ConfigurationModel
    {
        public int RepeatCount { get; set; }
        public string? SearchQueries { get; set; }
        public int MinSearchCount { get; set; }
        public int MaxSearchCount { get; set; }
        public int MinSiteVisitCount { get; set; }
        public int MaxSiteVisitCount { get; set; }
        public int MinTimeSpent { get; set; }
        public int MaxTimeSpent { get; set; }
        public string? ProfileGroupName { get; set; }
    }

}
