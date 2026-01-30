using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.AccessControl;
using System.Security.Principal;

namespace TimeChangeService
{
    public partial class TimeChangeService : ServiceBase
    {
        private CancellationTokenSource _cancellationTokenSource;
        private Task _listenerTask;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetSystemTime(ref SYSTEMTIME st);

        [StructLayout(LayoutKind.Sequential)]
        private struct SYSTEMTIME
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

        public TimeChangeService()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.ServiceName = "TimeChangeService";
            this.CanStop = true;
            this.CanPauseAndContinue = false;
            this.AutoLog = true;
        }

        protected override void OnStart(string[] args)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _listenerTask = Task.Run(() => ListenForCommands(_cancellationTokenSource.Token));
            EventLog.WriteEntry("TimeChangeService démarré", System.Diagnostics.EventLogEntryType.Information);
        }

        protected override void OnStop()
        {
            _cancellationTokenSource?.Cancel();
            _listenerTask?.Wait(5000);
            EventLog.WriteEntry("TimeChangeService arrêté", System.Diagnostics.EventLogEntryType.Information);
        }

        private async Task ListenForCommands(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Créer le pipe avec des permissions pour tous les utilisateurs
                    var pipeSecurity = new PipeSecurity();

                    // Autoriser tous les utilisateurs authentifiés à se connecter
                    var sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                    var accessRule = new PipeAccessRule(
                        sid,
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow);
                    pipeSecurity.AddAccessRule(accessRule);

                    // Autoriser aussi le groupe "Utilisateurs"
                    var usersSid = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                    var usersAccessRule = new PipeAccessRule(
                        usersSid,
                        PipeAccessRights.ReadWrite,
                        AccessControlType.Allow);
                    pipeSecurity.AddAccessRule(usersAccessRule);

                        using (var pipeServer = new NamedPipeServerStream(
                        "TimeChangePipe",
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous,256,256,pipeSecurity))
                    {
                        await pipeServer.WaitForConnectionAsync(cancellationToken);

                        byte[] buffer = new byte[256];
                        int bytesRead = await pipeServer.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                        
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        string response = ProcessCommand(message);

                        byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                        await pipeServer.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                        await pipeServer.FlushAsync(cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    EventLog.WriteEntry($"Erreur: {ex.Message}", System.Diagnostics.EventLogEntryType.Error);
                }
            }
        }

        private string ProcessCommand(string command)
        {
            try
            {
                // Format attendu: "SETTIME|yyyy-MM-dd HH:mm:ss"
                var parts = command.Split('|');
                if (parts.Length != 2 || parts[0] != "SETTIME")
                {
                    return "ERROR|Format de commande invalide";
                }

                if (!DateTime.TryParseExact(parts[1], "yyyy-MM-dd HH:mm:ss", 
                    System.Globalization.CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out DateTime newTime))
                {
                    return "ERROR|Format de date invalide";
                }

                // Conversion en UTC pour SetSystemTime
                DateTime utcTime = newTime.ToUniversalTime();

                SYSTEMTIME st = new SYSTEMTIME
                {
                    wYear = (short)utcTime.Year,
                    wMonth = (short)utcTime.Month,
                    wDay = (short)utcTime.Day,
                    wHour = (short)utcTime.Hour,
                    wMinute = (short)utcTime.Minute,
                    wSecond = (short)utcTime.Second,
                    wMilliseconds = (short)utcTime.Millisecond
                };

                if (SetSystemTime(ref st))
                {
                    EventLog.WriteEntry($"Heure changée avec succès: {newTime}", 
                        System.Diagnostics.EventLogEntryType.Information);
                    return "SUCCESS|Heure modifiée avec succès";
                }
                else
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    EventLog.WriteEntry($"Échec du changement d'heure. Code d'erreur: {errorCode}", 
                        System.Diagnostics.EventLogEntryType.Error);
                    return $"ERROR|Échec du changement d'heure (Code: {errorCode})";
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry($"Erreur lors du traitement: {ex.Message}", 
                    System.Diagnostics.EventLogEntryType.Error);
                return $"ERROR|{ex.Message}";
            }
        }
    }
}
