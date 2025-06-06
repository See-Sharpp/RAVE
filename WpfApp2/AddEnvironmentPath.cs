using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2
{
    public class AddEnvironmentPath
    {
        public static void AddNircmd()
        {
            string? exeDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "nircmd-x64");
            string? systemPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

            if (!systemPath.Split(';').Any(p => p.Trim().Equals(exeDir, StringComparison.OrdinalIgnoreCase)))
            {
                string newPath = systemPath + ";" + exeDir;
                Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                Debug.WriteLine("NirCmd path added to **User PATH**.");
            }
            else
            {
                Debug.WriteLine("NirCmd path not added to **User PATH**.");
            }
        }

    }
}

