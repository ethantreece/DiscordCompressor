using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DiscordCompressor
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length < 1) {
                MessageBox.Show("Please provide the path to the video file.");
                return;
            }

            string inputFilePath = args[0];
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath),
                                                 Path.GetFileNameWithoutExtension(inputFilePath) + "_compressed.mp4");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (File.Exists(outputFilePath))
            {
                DialogResult result = MessageBox.Show($"The file {outputFilePath} already exists. Do you want to delete and replace it?",
                                                      "File Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    MessageBox.Show("Operation cancelled.");
                    return;
                }
                File.Delete(outputFilePath);
            }

            using (var form = new ProgressForm())
            {
                form.Shown += async (sender, e) =>
                {
                    await Task.Run(() => CompressVideo(inputFilePath, outputFilePath, form));
                    form.Close();
                };
                Application.Run(form);
            }

            MessageBox.Show($"Compressed video saved to: {outputFilePath}");

            // ApplicationConfiguration.Initialize();
            // Application.Run(new Form1());
        }


        static void CompressVideo(string inputFilePath, string outputFilePath, ProgressForm form)
        {
            double videoDuration = GetVideoDuration(inputFilePath);
            if (videoDuration <= 0)
            {
                MessageBox.Show("Could not determine video duration.");
                return;
            }

            double targetFileSizeBytes = 24 * 1024 * 1024; // 25 MB in bytes
            double targetBitrate = (targetFileSizeBytes * 8) / videoDuration; // in bits per second

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\" -b:v {targetBitrate} -progress pipe:2 \"{outputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                process.ErrorDataReceived += (sender, e) => ParseProgress(e.Data, videoDuration, form);
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
        }

        static double GetVideoDuration(string inputFilePath)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            double duration = 0;
            using (Process process = Process.Start(startInfo))
            {
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string pattern = @"Duration: (\d{2}):(\d{2}):(\d{2})\.(\d{2})";
                        Match match = Regex.Match(e.Data, pattern);
                        if (match.Success)
                        {
                            int hours = int.Parse(match.Groups[1].Value);
                            int minutes = int.Parse(match.Groups[2].Value);
                            int seconds = int.Parse(match.Groups[3].Value);
                            int milliseconds = int.Parse(match.Groups[4].Value) * 10;
                            duration = new TimeSpan(0, hours, minutes, seconds, milliseconds).TotalSeconds;
                        }
                    }
                };

                process.BeginErrorReadLine();
                process.WaitForExit();
            }

            return duration;
        }

        static void ParseProgress(string data, double totalDuration, ProgressForm form)
        {
            if (string.IsNullOrEmpty(data)) return;

            string pattern = @"out_time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})";
            Match match = Regex.Match(data, pattern);
            if (match.Success)
            {
                int hours = int.Parse(match.Groups[1].Value);
                int minutes = int.Parse(match.Groups[2].Value);
                int seconds = int.Parse(match.Groups[3].Value);
                int milliseconds = int.Parse(match.Groups[4].Value) * 10;
                double currentTime = new TimeSpan(0, hours, minutes, seconds, milliseconds).TotalSeconds;

                double progress = (currentTime / totalDuration) * 100;
                form.UpdateProgress((int)progress, $"Processing... {progress:F2}%");
            }
        }
    }

}