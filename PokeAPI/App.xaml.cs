using System.Windows;

namespace PokeAPI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        PokeContext context;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize the database context
            context = new PokeContext();
            context.Database.EnsureCreated();

            // Show the login panel
            LoginPanel loginPanel = new LoginPanel(context);
            loginPanel.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // clean up database connection
            context.Dispose();
            base.OnExit(e);
        }
    }

}
