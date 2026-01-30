using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TimeChangeClient
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.btnSetTime = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.lblCurrentTime = new System.Windows.Forms.Label();
            this.timerCurrentTime = new System.Windows.Forms.Timer();
            this.SuspendLayout();
            
            // dateTimePicker
            this.dateTimePicker.CustomFormat = "yyyy-MM-dd HH:mm:ss";
            this.dateTimePicker.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dateTimePicker.Location = new System.Drawing.Point(30, 70);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.ShowUpDown = true;
            this.dateTimePicker.Size = new System.Drawing.Size(200, 23);
            this.dateTimePicker.TabIndex = 0;
            
            // btnSetTime
            this.btnSetTime.Location = new System.Drawing.Point(250, 68);
            this.btnSetTime.Name = "btnSetTime";
            this.btnSetTime.Size = new System.Drawing.Size(120, 27);
            this.btnSetTime.TabIndex = 1;
            this.btnSetTime.Text = "Changer l'heure";
            this.btnSetTime.UseVisualStyleBackColor = true;
            this.btnSetTime.Click += new System.EventHandler(this.BtnSetTime_Click);
            
            // lblCurrentTime
            this.lblCurrentTime.AutoSize = true;
            this.lblCurrentTime.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblCurrentTime.Location = new System.Drawing.Point(30, 30);
            this.lblCurrentTime.Name = "lblCurrentTime";
            this.lblCurrentTime.Size = new System.Drawing.Size(150, 19);
            this.lblCurrentTime.TabIndex = 2;
            this.lblCurrentTime.Text = "Heure actuelle: --:--:--";
            
            // lblStatus
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(30, 120);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(43, 15);
            this.lblStatus.TabIndex = 3;
            this.lblStatus.Text = "Statut:";
            
            // timerCurrentTime
            this.timerCurrentTime.Enabled = true;
            this.timerCurrentTime.Interval = 1000;
            this.timerCurrentTime.Tick += new System.EventHandler(this.TimerCurrentTime_Tick);
            
            // MainForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 180);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblCurrentTime);
            this.Controls.Add(this.btnSetTime);
            this.Controls.Add(this.dateTimePicker);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Changement d'heure système";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private DateTimePicker dateTimePicker;
        private Button btnSetTime;
        private Label lblStatus;
        private Label lblCurrentTime;
        private Timer timerCurrentTime;

        private void MainForm_Load(object sender, EventArgs e)
        {
            dateTimePicker.Value = DateTime.Now;
            UpdateCurrentTime();
        }

        private void TimerCurrentTime_Tick(object sender, EventArgs e)
        {
            UpdateCurrentTime();
        }

        private void UpdateCurrentTime()
        {
            lblCurrentTime.Text = $"Heure actuelle: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        }

        private async void BtnSetTime_Click(object sender, EventArgs e)
        {
            btnSetTime.Enabled = false;
            lblStatus.Text = "Statut: Envoi de la commande au service...";
            lblStatus.ForeColor = System.Drawing.Color.Blue;

            try
            {
                string result = await SendTimeChangeCommand(dateTimePicker.Value);
                
                if (result.StartsWith("SUCCESS"))
                {
                    lblStatus.Text = "Statut: Heure modifiée avec succès!";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                    UpdateCurrentTime();
                }
                else
                {
                    lblStatus.Text = $"Statut: Erreur - {result.Substring(6)}";
                    lblStatus.ForeColor = System.Drawing.Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Statut: Erreur - {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                
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
                btnSetTime.Enabled = true;
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
    }
}
