// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using System.Collections.Generic;
using System.Threading;

namespace SiliconStudio.Core.IO
{
    /// <summary>
    /// Track file system events from several directories.
    /// </summary>
    public partial class DirectoryWatcher : IDisposable
    {
        private const int SleepBetweenWatcherCheck = 200;
#if !SILICONSTUDIO_PLATFORM_UWP
        private Thread watcherCheckThread;
#endif
        private bool exitThread;

        /// <summary>
        /// Occurs when a file/directory change occurred.
        /// </summary>
        public event EventHandler<FileEvent> Modified;

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectoryWatcher"/> class.
        /// </summary>
        /// <param name="fileFilter">The file filter By default null default to *.*</param>
        public DirectoryWatcher(string fileFilter = null)
        {
            FileFilter = fileFilter ?? "*.*";
            InitializeInternal();
        }

        /// <summary>
        /// Gets the file filter used by this instance.
        /// </summary>
        /// <value>The file filter.</value>
        public string FileFilter { get; private set; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public virtual void Dispose()
        {
            exitThread = true;
#if !SILICONSTUDIO_PLATFORM_UWP
            if (watcherCheckThread != null)
            {
                watcherCheckThread.Join();
                watcherCheckThread = null;
            }
#endif
            DisposeInternal();
        }

        /// <summary>
        /// Gets a list of current directories being tracked.
        /// </summary>
        /// <returns>A list of current directories being tracked</returns>
        public List<string> GetTrackedDirectories()
        {
            return GetTrackedDirectoriesInternal();
        }

        /// <summary>
        /// Tracks the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>
        /// If path is a file, this will used the parent directory. If the path is invalid, it will not fail but just skip it.
        /// </remarks>
        public void Track(string path)
        {
            TrackInternal(path);
        }

        /// <summary>
        /// UnTracks the specified path.
        /// </summary>
        /// <remarks>
        /// If path is a file, this will used the parent directory. If the path is invalid, it will not fail but just skip it.
        /// </remarks>
        public void UnTrack(string path)
        {
            UnTrackInternal(path);
        }

        /// <summary>
        /// Called when a file event occurred.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The file event.</param>
        protected virtual void OnModified(object sender, FileEvent e)
        {
            Modified?.Invoke(sender, e);
        }

#if !SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
        // Doesn't throw any exceptions on other platforms

        private void InitializeInternal()
        {
        }

        private void DisposeInternal()
        {
        }

        private void TrackInternal(string path)
        {
        }

        private void UnTrackInternal(string path)
        {
        }

        private List<string> GetTrackedDirectoriesInternal()
        {
            return new List<string>();
        }
#endif
    }
}
