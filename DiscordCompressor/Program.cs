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
            if (args.Length < 2) {
                MessageBox.Show("Please provide the path to the video file.");
                return;
            }

            double size = 0;
            String sizeString = args[0];
            if ("25".Equals(sizeString))
            {
                size = 24.9;
            } else if ("50".Equals (sizeString))
            {
                size = 49.9;
            } else if ("100".Equals(sizeString))
            {
                size = 99.9;
            } else
            {
                MessageBox.Show("Please provide a valid desired file size (25, 50, 100).");
                return;
            }

            string inputFilePath = "";
            if (args.Length == 2)
            {
                inputFilePath = args[1];
            } else
            {
                inputFilePath = string.Join(" ", args.Skip(1));
            }
            
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath),
                                                 Path.GetFileNameWithoutExtension(inputFilePath) + "_compressed.mp4");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool fileExists = false;
            try
            {
                fileExists = File.Exists(inputFilePath);
                if (!fileExists)
                {
                    // Attempt to open the file to get more detailed errors
                    using (var fs = File.Open(inputFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        fileExists = true; // This line should never be hit because File.Exists should return true if the file exists
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show($"Access denied to the file: {inputFilePath}. Please check your permissions.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Exception: {ex.Message}");
            }

            if (File.Exists(outputFilePath))
            {
                DialogResult result = MessageBox.Show($"The file {outputFilePath} already exists. Do you want to delete and replace it?",
                                                      "File Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.No)
                {
                    return;
                }
                File.Delete(outputFilePath);
            }

            bool success = false;

            using (var form = new ProgressForm())
            {
                form.Shown += async (sender, e) =>
                {
                    await Task.Run(() => success = CompressVideo(size, inputFilePath, outputFilePath, form));
                    form.Close();
                };
                Application.Run(form);
            }

            if (success) MessageBox.Show($"Compressed video saved to: {outputFilePath}");

        }

        static bool CompressVideo(double size, string inputFilePath, string outputFilePath, ProgressForm form)
        {
            double videoDuration = GetVideoDuration(inputFilePath);
            if (videoDuration <= 0)
            {
                MessageBox.Show("Could not determine video duration.","HI", MessageBoxButtons.YesNo);
                return false;
            }

            double targetFileSizeBytes = size * 1024 * 1024; // desired size in bytes

            // Check if actual is already below target

            FileInfo inputFile = new FileInfo(inputFilePath);
            if (inputFile.Length <= targetFileSizeBytes)
            {
                MessageBox.Show("The file is already below the desired compression size. Compression is not needed.");
                return false;
            }

            double targetVideoBitrate = (targetFileSizeBytes * 8) / videoDuration; // in bits per second

            // Adjust the bitrate calculation to include audio bitrate, here assumed as 128k
            double audioBitrate = GetAudioBitrate(inputFilePath); // 128 kbps in bits per second
            double finalVideoBitrate = targetVideoBitrate - audioBitrate;

            ProcessStartInfo startInfo1 = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-y -i \"{inputFilePath}\" -b:v {finalVideoBitrate} -b:a 128k -pass 1 -an -f mp4 NUL",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            ProcessStartInfo startInfo2 = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{inputFilePath}\" -b:v {finalVideoBitrate} -b:a 128k -pass 2 -progress pipe:2 \"{outputFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process1 = Process.Start(startInfo1))
            {
                process1.BeginErrorReadLine();
                process1.WaitForExit();

                if (process1.ExitCode != 0)
                {
                    MessageBox.Show("An error occurred during the first pass of video compression.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            using (Process process2 = Process.Start(startInfo2))
            {
                process2.ErrorDataReceived += (sender, e) => ParseProgress(e.Data, videoDuration, form);
                process2.BeginErrorReadLine();
                process2.WaitForExit();

                if (process2.ExitCode != 0)
                {
                    MessageBox.Show("An error occurred during the second pass of video compression.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            // Delete temporary files
            DeleteTemporaryFiles();

            return true;
        }

        static void DeleteTemporaryFiles()
        {
            try
            {
                string tempFile1 = "ffmpeg2pass-0.log";
                string tempFile2 = "ffmpeg2pass-0.log.mbtree";

                if (File.Exists(tempFile1))
                {
                    File.Delete(tempFile1);
                }

                if (File.Exists(tempFile2))
                {
                    File.Delete(tempFile2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error deleting temporary files: {ex.Message}");
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

        static int GetAudioBitrate(string inputFilePath)
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

            int audioBitrate = 128000; // Default to 128kbps if not found
            using (Process process = Process.Start(startInfo))
            {
                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        string pattern = @"Stream.*Audio:.* (\d+) kb/s";
                        Match match = Regex.Match(e.Data, pattern);
                        if (match.Success)
                        {
                            audioBitrate = int.Parse(match.Groups[1].Value) * 1000; // Convert kbps to bps
                        }
                    }
                };

                process.BeginErrorReadLine();
                process.WaitForExit();
            }

            return audioBitrate;
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