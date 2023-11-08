using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karen.Interop
{
    public static class RedisServer
    {

        public static void Start(string contentFolder)
        {
            Process.Start(new ProcessStartInfo(".\\Redis\\redis-server.exe", $".\\Redis\\redis.conf --dir '{contentFolder}/' --daemonize yes") { UseShellExecute = false });
        }

        public static void Stop()
        {
            Process.Start(new ProcessStartInfo("taskkill", "/F /IM redis-server.exe") { UseShellExecute = false});
        }
    }
}
