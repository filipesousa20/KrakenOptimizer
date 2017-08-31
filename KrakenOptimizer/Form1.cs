using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KrakenOptimizer
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        public Form1()
        {
            InitializeComponent();
            buttonOptimize.Enabled = false;
            textBoxResults.Text = "Results pane";
            textBoxResults.TextChanged += TextBoxResults_TextChanged;
            buttonCancel.Visible = false;
        }

        private void TextBoxResults_TextChanged(object sender, EventArgs e)
        {
            textBoxResults.SelectionStart = textBoxResults.Text.Length;
            textBoxResults.ScrollToCaret();
        }

        private void buttonOpenFolder_Click(object sender, EventArgs e)
        {
            var openFolderDialog = new FolderBrowserDialog();
            if (openFolderDialog.ShowDialog() == DialogResult.OK)
            {
                textBoxFolderPath.Text = openFolderDialog.SelectedPath;
            }
            buttonOptimize.Enabled = true;
        }

        private async void buttonOptimize_Click(object sender, EventArgs e)
        {
            await OptimizeImages(cancellationTokenSource.Token);
        }

        private async Task OptimizeImages(CancellationToken cancellationToken)
        {
            const int MAX_DOWNLOADS = 10;
            double totalSavedBytes = 0;
            var extensions = new List<string> { ".jpg", ".jpeg", ".gif", ".png" };
            buttonOptimize.Enabled = buttonPickFolder.Enabled = false;
            //buttonCancel.Enabled = buttonCancel.Visible = true;
            textBoxResults.Text = string.Empty;
            var fileEntries = Directory.GetFiles(textBoxFolderPath.Text, "*.*", SearchOption.AllDirectories).Where(f => extensions.Contains(Path.GetExtension(f)));

            using (var semaphore = new SemaphoreSlim(MAX_DOWNLOADS))
            using (var optimizer = new KrakenOptimizer())
            {
                var tasks = fileEntries.Select(async file =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var data = await optimizer.Optimize(file);
                        var filePath = Path.Combine(textBoxFolderPath.Text, data.Body.FileName);
                        using (var client = new WebClient())
                        {
                            client.DownloadFileCompleted += Client_DownloadFileCompleted;
                            client.DownloadFileAsync(new Uri(data.Body.KrakedUrl), filePath);
                        }
                        var success = data.Success ? "OK" : "ERROR";
                        totalSavedBytes += data.Body.SavedBytes;
                        textBoxResults.Text += $"{success} - Optimized Image {data.Body.FileName}. Saved {data.Body.SavedBytes} bytes. {Environment.NewLine}";
                    }
                    catch (Exception ex)
                    {
                        textBoxResults.Text += $"{ex.Message}";
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                await Task.WhenAny(Task.WhenAll(tasks), Task.Delay(Timeout.Infinite, cancellationToken));
                totalSavedBytes = Math.Round(totalSavedBytes / 1024 / 1024, 2);
                textBoxResults.Text += $"You saved a total of {totalSavedBytes} MB";
            }
            buttonOptimize.Enabled = buttonPickFolder.Enabled = true;
            //buttonCancel.Enabled = buttonCancel.Visible = false;
        }

        private void Client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                textBoxResults.Text += $"Error when downloading: {e.Error.Message} {Environment.NewLine}";
            }
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            cancellationTokenSource.Cancel();
            buttonCancel.Enabled = buttonCancel.Visible = false;
            textBoxResults.Text += $"Optimization cancelled by user. {Environment.NewLine}";
        }
    }
}
