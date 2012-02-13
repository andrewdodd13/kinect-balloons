﻿using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Balloons.Server
{
    [JsonObject(MemberSerialization.OptIn)]
    public class FeedContent
    {
        [JsonProperty]
        public string ContentID;
        [JsonProperty]
        public string Title;
        [JsonProperty]
        public string SubmittedBy;
        [JsonProperty]
        public string URL;
        [JsonProperty]
        public int TimeCreated;
        [JsonProperty]
        public string BalloonColour;
        [JsonProperty]
        public string ImageURL;
        [JsonProperty]
        public int Type;
        [JsonProperty]
        public string Excerpt;
    }
}