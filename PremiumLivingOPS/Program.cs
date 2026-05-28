using PremiumLivingOPS.Views.Login;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new LoginForm());
        }
    }
}
