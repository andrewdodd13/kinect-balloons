using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BubblesClient.Model
{
    /// <summary>
    /// Balloon represents a Balloon as it exists throughout the system. It 
    /// contains static details such as the URL and Source, but not transient
    /// details such as which server it is on or where it is located on the 
    /// screen.
    /// These details should be maintained by the local system.
    /// 
    /// Note: It belongs in the server project, but we don't have one of those 
    /// yet.
    /// </summary>
    class Balloon
    {
        public string TargetURL { get; set; }
        public string Source { get; set; }
        public string Excerpt { get; set; }

        // Do we need formatting options, etc?
    }
}
