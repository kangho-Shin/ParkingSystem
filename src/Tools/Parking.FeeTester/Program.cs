namespace Parking.FeeTester;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        try
        {
            Application.Run(new MainForm());
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Parking Fee Tester", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
