using System.Collections.Generic;

namespace SitecoreMediaAI.Model
{
    public class Caption
    {
        public string text { get; set; }
        public double confidence { get; set; }
    }

    public class Description
    {
        public IList<string> tags { get; set; }
        public IList<Caption> captions { get; set; }
    }

    public class Metadata
    {
        public int width { get; set; }
        public int height { get; set; }
        public string format { get; set; }
    }

    public class VisonResponse
    {
        public Description description { get; set; }
        public string requestId { get; set; }
        public Metadata metadata { get; set; }
    }
}
