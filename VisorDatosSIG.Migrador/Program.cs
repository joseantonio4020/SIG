using System;
using System.Windows.Forms;

namespace VisorDatosSIG.Migrador;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MigradorForm());
    }
}
