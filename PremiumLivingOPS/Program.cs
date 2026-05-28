using PremiumLivingOPS.Views.Auth;
using System;
using System.Windows.Forms;

namespace PremiumLivingOPS
{
    internal static class Program
    {
        /// <summary>
        /// Application entry point.
        /// LoginForm is in Views.Auth namespace (not Views.Login).
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new LoginForm());
        }
    }
}
