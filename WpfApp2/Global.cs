using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2
{
    internal static class Global
    {
        public static int? UserId { get; set; } = 1;
        public static bool SignUp = false;
        public static string? deafultScreenShotPath = @"C:\temp\Rave Media";

    }
}
