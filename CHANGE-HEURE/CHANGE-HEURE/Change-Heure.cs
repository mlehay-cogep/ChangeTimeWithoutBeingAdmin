using CHANGE_HEURE.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static CHANGE_HEURE.Program;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace CHANGE_HEURE
{
    public partial class Form1 : Form
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public short wYear;
            public short wMonth;
            public short wDayOfWeek;
            public short wDay;
            public short wHour;
            public short wMinute;
            public short wSecond;
            public short wMilliseconds;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void GetSystemTime(ref SYSTEMTIME lpSystemTime);

        public Form1()
        {
            InitializeComponent();
            dateTimePicker1.CustomFormat = "dd-MM-yyyy hh:mm";
            dateTimePicker1.Text = DateTime.Now.ToString("dd-MM-yyyy hh:mm");

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private async void Button1_Click(object sender, EventArgs e)
        {
            // Récupère l'heure actuelle (UTC)
            SYSTEMTIME current = new SYSTEMTIME();
            GetSystemTime(ref current);

            int mois = dateTimePicker1.Value.Month; // récupère le mois en chiffre
            int jour = dateTimePicker1.Value.Day; // récupère le jour en chiffre
            int annee = dateTimePicker1.Value.Year;
            int heure = dateTimePicker2.Value.Hour;
            int minutesfr = dateTimePicker2.Value.Minute;

            current.wYear = ((short)annee);
            current.wMonth = ((short)mois);
            current.wDay = ((short)jour);
            current.wHour = ((short)heure);
            current.wMinute = ((short)minutesfr);

            lblCurrentTime.Text = "Statut: modification de l'heure....";
            /*
            // Applique la nouvelle date
            if (!SetSystemTime(ref current))
            {
                Console.WriteLine("Erreur lors du changement de date.");
            }
            else
            {
                Console.WriteLine("Date système modifiée !");

            }       
            */

            try
            {
                DateTime newDateTime = new DateTime(annee, mois, jour, heure, minutesfr, 0);
                string result = await SendTimeChangeCommand(newDateTime);

                if (result.StartsWith("SUCCESS"))
                {

                    // UpdateCurrentTime();
                    lblCurrentTime.Text = "Statut: Heure modifiée avec succès!";
                    lblCurrentTime.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblCurrentTime.Text = $"Statut: Erreur - {result.Substring(6)}";
                    lblCurrentTime.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                // lblStatus.Text = $"Statut: Erreur - {ex.Message}";
                // lblStatus.ForeColor = System.Drawing.Color.Red;

                if (ex.Message.Contains("pipe") || ex.Message.Contains("service"))
                {
                    MessageBox.Show(
                        "Impossible de se connecter au service.\n\n" +
                        "Vérifiez que le service 'TimeChangeService' est démarré.\n" +
                        "Vous pouvez le démarrer avec la commande:\n" +
                        "net start TimeChangeService",
                        "Erreur de connexion",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                // btnSetTime.Enabled = true;
            }

        }

        private async Task<string> SendTimeChangeCommand(DateTime newTime)
        {
            using (var pipeClient = new NamedPipeClientStream(
                ".",
                "TimeChangePipe",
                PipeDirection.InOut,
                PipeOptions.Asynchronous))
            {
                await pipeClient.ConnectAsync(5000);

                string command = $"SETTIME|{newTime:yyyy-MM-dd HH:mm:ss}";
                byte[] commandBytes = Encoding.UTF8.GetBytes(command);

                await pipeClient.WriteAsync(commandBytes, 0, commandBytes.Length);
                await pipeClient.FlushAsync();

                byte[] buffer = new byte[256];
                int bytesRead = await pipeClient.ReadAsync(buffer, 0, buffer.Length);

                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged_1(object sender, EventArgs e)
        {

        }

        private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
        {

            int mois = dateTimePicker1.Value.Month; // récupère le mois en chiffre
            int jour = dateTimePicker1.Value.Day; // récupère le jour en chiffre
            int annee = dateTimePicker1.Value.Year;
            // textBoxYear.Text= annee.ToString();
        }

        private async void GoodDate(object sender, EventArgs e)
        {
            // Récupère l'heure actuelle (UTC)
            SYSTEMTIME current = new SYSTEMTIME();
            GetSystemTime(ref current);
            lblCurrentTime.Text = "Statut: modification de l'heure....";

            string ntpServer = "time.windows.com";
            DateTime dateNtp = Program.GetNetworkTime(ntpServer);
            current.wYear = ((short)dateNtp.Year);
            current.wMonth = ((short)dateNtp.Month);
            current.wDay = ((short)dateNtp.Day);
            int heure = dateNtp.Hour - 1;
            current.wHour = ((short)heure);
            current.wMinute = ((short)dateNtp.Minute);

            /*
            // Applique la nouvelle date
            if (!SetSystemTime(ref current))
            {
                Console.WriteLine("Erreur lors du changement de date.");
            }
            else
            {
                Console.WriteLine("Date système modifiée !");
            }
            */

            try
            {
                // DateTime newDateTime = new DateTime(annee, mois, jour, heure, minutesfr, 0);
                string result = await SendTimeChangeCommand(dateNtp);

                if (result.StartsWith("SUCCESS"))
                {

                    // UpdateCurrentTime();
                    lblCurrentTime.Text = "Statut: Heure modifiée avec succès!";
                    lblCurrentTime.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    lblCurrentTime.Text = $"Statut: Erreur - {result.Substring(6)}";
                    lblCurrentTime.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                // lblStatus.Text = $"Statut: Erreur - {ex.Message}";
                // lblStatus.ForeColor = System.Drawing.Color.Red;

                if (ex.Message.Contains("pipe") || ex.Message.Contains("service"))
                {
                    MessageBox.Show(
                        "Impossible de se connecter au service.\n\n" +
                        "Vérifiez que le service 'TimeChangeService' est démarré.\n" +
                        "Vous pouvez le démarrer avec la commande:\n" +
                        "net start TimeChangeService",
                        "Erreur de connexion",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
            finally
            {
                // btnSetTime.Enabled = true;
            }


        }

        private void UpdateCurrentTime()
        {
            lblCurrentTime.Text = $"Heure actuelle: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }
    }
}