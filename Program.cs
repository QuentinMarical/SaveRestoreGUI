using SaveRestoreGUI.Services;
using SaveRestoreGUI.UI;

namespace SaveRestoreGUI
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            // ── Splash screen ────────────────────────────────────────────────
            var splash = new SplashForm();
            splash.Show();
            Application.DoEvents();

            // Étape 1 : navigateurs
            splash.UpdateProgress(15, "Détection des navigateurs installés...");
            var browserEntries = AppLauncherService.GetBrowserEntries();

            // Étape 2 : auto-détection OneDrive + logiciels
            splash.UpdateProgress(45, "Détection OneDrive et logiciels...");
            var autoDetect = AutoDetectService.Detect();

            // Étape 3 : thème
            splash.UpdateProgress(75, "Chargement du thème...");
            // ThemeManager s'initialise à l'accès, rien à faire explicitement

            // Étape 4 : création de la fenêtre principale
            splash.UpdateProgress(90, "Ouverture de l'interface...");
            var mainForm = new MainForm(browserEntries, autoDetect);

            // Fermeture du splash et affichage de MainForm
            splash.UpdateProgress(100, "Prêt !");
            // Laisser le message pump traiter l'affichage du 100% sans bloquer le thread UI
            Application.DoEvents();
            splash.Close();
            splash.Dispose();

            Application.Run(mainForm);
        }
    }
}
