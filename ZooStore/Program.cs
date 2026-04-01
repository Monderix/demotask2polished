using System;
using System.Threading;
using System.Windows.Forms;
using ZooStore.Services;

namespace ZooStore
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.ThreadException += OnThreadException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FormAuth());
        }

        private static void OnThreadException(object sender, ThreadExceptionEventArgs e)
        {
            DialogService.ShowException(e.Exception, "выполнить операцию");
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception ?? new Exception("Неизвестная ошибка.");
            DialogService.ShowException(exception, "выполнить операцию");
        }
    }
}
