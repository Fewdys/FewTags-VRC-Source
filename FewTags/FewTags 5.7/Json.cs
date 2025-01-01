using System;
using System.Collections.Generic;

namespace FewTags
{
    internal class Json
    {

        [Serializable]
        public class _Tags
        {
            public List<Tags> records { get; set; }
            //public string UserID { get; set; }
            //public Tags UserTags { get; set; }
        }

        public class Tags
        {
            public int id { get; set; }
            public string UserID { get; set; }
            public string PlateBigText { get; set; }
            public bool Malicious { get; set; }
            public bool Active { get; set; }
            public bool TextActive { get; set; }
            public bool BigTextActive { get; set; }
            public string Size { get; set; }
            public string[] Tag { get; set; }
        }
    }
}
