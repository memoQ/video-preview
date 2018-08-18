using System;
using System.IO;

namespace Vlc.DotNet.Wpf
{
    public static class VlcControlExtensions
    {
        /// <summary>
        /// Tries to play a media first by assuming it is a file then assuming it is an uri string.
        /// </summary>
        public static void PlayMedia(this VlcControl vlcControl, string media)
        {
            try
            {
                vlcControl.MediaPlayer.Play(new FileInfo(media));
            }
            catch
            {
                vlcControl.MediaPlayer.Play(new Uri(media));
            }
        }
    }
}
