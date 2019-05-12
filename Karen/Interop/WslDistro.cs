using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Karen.Interop
{
    class WslDistro
    {
        const string DISTRO_NAME = "LANraragi";

        
        public bool CheckDistro()
        {
            return WslApi.WslIsDistributionRegistered(DISTRO_NAME);
        }

        public string GetVersion()
        {
            //perl - Mojo - E 'my $conf = eval(f("lrr.conf")->slurp); say %$conf{version} ." - ". %$conf{version_name}'

            return "";

        }

    }
}
