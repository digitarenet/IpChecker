namespace IpChecker;

using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
//using System.Threading;
//using System.Reflection;// Per BindingFlags nel metodo TrayIcon_MouseClick

static class Program {
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new IPCheckerContext());
    }
}

public class IPCheckerContext : ApplicationContext {
    private NotifyIcon trayIcon;
    private ToolStripMenuItem ipAddressItem;
    private string currentIp = "Verifica in corso...";
    private readonly System.Threading.Timer checkTimer;
    private const int CheckIntervalMinutes = 5;

    public IPCheckerContext()
    {
        InitializeUI();

        // Avvia il timer per controllare l'IP pubblico ogni 5 minuti
        checkTimer = new System.Threading.Timer(
        async (o) => await CheckPublicIp(),
        null,
        TimeSpan.Zero,
        TimeSpan.FromMinutes(CheckIntervalMinutes));
    }

    private void InitializeUI()
    {
        ipAddressItem = new ToolStripMenuItem("IP: " + currentIp)
        {
            Enabled = false
        };

        var copyMenuItem = new ToolStripMenuItem("Copia IP", null, OnCopyIpToClipboard);
        var refreshMenuItem = new ToolStripMenuItem("Aggiorna IP", null, async (s, e) => await CheckPublicIp());
        var exitMenuItem = new ToolStripMenuItem("Esci", null, OnExit);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(ipAddressItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(copyMenuItem);
        contextMenu.Items.Add(refreshMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        // Inizializza l'icona nella system tray
        trayIcon = new NotifyIcon()
        {
            Icon = GetIcon(statusType.Updating),
            ContextMenuStrip = contextMenu,
            Visible = true,
            Text = "Controllore IP Pubblico"
        };

        trayIcon.MouseClick += TrayIcon_MouseClick;
    }

    private enum statusType {
        Ok,
        Updating,
        Starting,
        Error
    }

    private Icon GetIcon(statusType type)
    {
        Brush color;
        switch (type)
        {
            case statusType.Ok:
                color = Brushes.Green;
                break;
            case statusType.Updating:
                color = Brushes.Orange;
                break;
            case statusType.Starting:
                color = Brushes.Plum;
                break;
            case statusType.Error:
                color = Brushes.Red;
                break;
            default:
                color = Brushes.DodgerBlue;
                break;
        }

        // Crea un'icona semplice con un punto colorato
        var bitmap = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Transparent);
            g.FillEllipse(color, 2, 2, 30, 30);
        }

        var hIcon = bitmap.GetHicon();
        var icon = Icon.FromHandle(hIcon);
        return icon;
    }


    private async Task CheckPublicIp()
    {
        var prevIp = currentIp;
        try
        {
            // Cambia temporaneamente l'icona per indicare il controllo in corso
            trayIcon.Icon = GetIcon(statusType.Updating);
            trayIcon.Text = "Verifica IP in corso...";

            using var client = new HttpClient();

            // Utilizziamo un servizio che restituisce solo l'indirizzo IP in testo semplice
            var response = await client.GetStringAsync("https://api.ipify.org");
            currentIp = response.Trim();

            // Aggiorna il menu e l'icona
            ipAddressItem.Text = "IP: " + currentIp;
            trayIcon.Text = "IP Pubblico: " + currentIp;
            trayIcon.Icon = GetIcon(statusType.Ok);

            if (currentIp != prevIp)
                // Mostra una notifica
                trayIcon.ShowBalloonTip(
                3000,
                currentIp,
                "Il tuo IP Pubblico è stato aggiornato",
                ToolTipIcon.Info);
        }
        catch (Exception ex)
        {
            currentIp = "Errore";
            ipAddressItem.Text = "IP: Errore di verifica";
            trayIcon.Icon = GetIcon(statusType.Error);
            trayIcon.Text = "Errore durante il controllo dell'IP: " + ex.Message;

            trayIcon.ShowBalloonTip(
            3000,
            "Errore",
            "Impossibile verificare l'IP pubblico: " + ex.Message,
            ToolTipIcon.Error);
        }
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            // Copia l'indirizzo IP
            CopyToClipboard();
            
            // Mostra il menu contestuale anche con il clic sinistro
            // var mi = typeof(NotifyIcon)
            //     .GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            // mi?.Invoke(trayIcon, null);
        }
    }

    private void OnCopyIpToClipboard(object? sender, EventArgs e)
    {
        CopyToClipboard();
    }
    
    private void CopyToClipboard()
    {
        if (currentIp != "Verifica in corso..." && currentIp != "Errore")
        {
            Clipboard.SetText(currentIp);
            trayIcon.ShowBalloonTip(
            2000,
            currentIp,
            "Indirizzo IP copiato negli appunti",
            ToolTipIcon.Info);
        }
    }

    private void OnExit(object? sender, EventArgs e)
    {
        // Pulisci le risorse
        checkTimer.Dispose();
        trayIcon.Visible = false;
        trayIcon.Dispose();
        Application.Exit();
    }
    
    
}