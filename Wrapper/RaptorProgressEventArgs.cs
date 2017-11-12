﻿using System;

namespace Raptor.Unity {

    /// <summary>
    /// Event args that are raised in the Raptor3D Progress event.
    /// </summary>
    public class RaptorProgressEventArgs : EventArgs {

        /// <summary>
        /// The name of the progress that is being reported.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The number of events that have occurred, will start at 0 and will increment until Total is reached.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// The total number of progress items, could be 1 for single events like 'Optimizing'.
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// The amount of time, in milliseconds, since the first progress event for this title.
        /// </summary>
        public int Duration { get; set; }

    }
}
