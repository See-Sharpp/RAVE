using WpfApp2.Models;

namespace WpfApp2
{
    internal static class Global
    {
        public static int? UserId { get; set; } = 1;
        public static bool SignUp = false;
        public static string? deafultScreenShotPath = @"C:\temp\Rave Media";
        public static FloatingIcon? floatingIcon { get; set; } = new FloatingIcon();

        public static WakeWordHelper? _wakeWordHelper { get; set; } = null;

        public static bool logout = false;
        public static Queue<LLM_Detail> web_browse=new Queue<LLM_Detail>();

        public static Queue<LLM_Detail> file_operation = new Queue<LLM_Detail>();

        public static Queue<LLM_Detail> application_control = new Queue<LLM_Detail>();

        public static Queue<LLM_Detail> system_control = new Queue<LLM_Detail>();

        public static Queue<LLM_Detail> total_commands = new Queue<LLM_Detail>();

        public static bool autoOpen =false;

        public static int Scanning = 0;

    }
}
