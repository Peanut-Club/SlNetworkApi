﻿using System.ComponentModel;

namespace SlNetworkApiClient
{
    public class Config
    {
        [Description("Whether or not to show debug messages.")]
        public bool ShowDebug { get; set; }

        [Description("Sets the server's ID.")]
        public string Id { get; set; } = "";

        [Description("Sets the master server's URL")]
        public string Url { get; set; } = "";
    }
}