using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace YMModsApp.Common.Tool
{
    public static class SoundPlayerTool
    {
        public static SoundPlayer player = new SoundPlayer();

        public static void SoundPlay(string filename)
        {
            //异步播放： 
            player.SoundLocation = Environment.CurrentDirectory + @"/Media/" + filename + ".wav";
            player.Load();
            player.Play();

        }

        private static void OnMediaFailed(object sender, ExceptionEventArgs e)
        {
            Debug.WriteLine(e.ToString());
        }
    }
}
