using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;



namespace IVolt.Apps.IV_Elite_PC_Cleaner.CLI
{
	public static class Interactive
	{
		public static void RunProcess()
		{
			string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources\\Data\\FileScan.db");

			if (!File.Exists(dbPath))
			{
				Console.WriteLine("❌ Database not found. Please ensure 'FileScan.db' is in the same directory.");
				return;
			}

			Console.Write("Enter the directory to scan: ");
			string scanDir = Console.ReadLine();

			if (string.IsNullOrWhiteSpace(scanDir) || !Directory.Exists(scanDir))
			{
				Console.WriteLine("❌ Invalid directory.");
				return;
			}

			string[] files;
			try
			{
				files = Directory.GetFiles(scanDir, "*", SearchOption.AllDirectories);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error accessing files: {ex.Message}");
				return;
			}

			int filesInUse = 0;
			string windowsVersion = Environment.OSVersion.VersionString;
			string userName = Environment.UserName;
			string handleExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "handle.exe");

			using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
			{
				conn.Open();
				foreach (var file in files)
				{
					string hash = "";
					try
					{
						hash = File_Helper.ComputeHash(file);
					}
					catch { }

					string inUseBy = "";
					try
					{
						var process = new Process
						{
							StartInfo = new ProcessStartInfo
							{
								FileName = handleExe,
								Arguments = $"\"{file}\"",
								RedirectStandardOutput = true,
								UseShellExecute = false,
								CreateNoWindow = true
							}
						};

						process.Start();
						string output = process.StandardOutput.ReadToEnd();
						process.WaitForExit();

						if (!output.Contains("No matching handles found"))
						{
							inUseBy = output;
							filesInUse++;
						}
					}
					catch { }

					var cmd = new SQLiteCommand(@"
                    INSERT INTO ScannedFiles 
                    (FilePath, FileName, FileHash, FileSizeBytes, LastModified, WindowsVersion, InUseBy, DateScanned)
                    VALUES (@path, @name, @hash, @size, @modified, @winver, @inuse, @scanned);", conn);

					var fi = new FileInfo(file);
					cmd.Parameters.AddWithValue("@path", file);
					cmd.Parameters.AddWithValue("@name", fi.Name);
					cmd.Parameters.AddWithValue("@hash", hash);
					cmd.Parameters.AddWithValue("@size", fi.Length);
					cmd.Parameters.AddWithValue("@modified", fi.LastWriteTimeUtc.ToString("o"));
					cmd.Parameters.AddWithValue("@winver", windowsVersion);
					cmd.Parameters.AddWithValue("@inuse", inUseBy);
					cmd.Parameters.AddWithValue("@scanned", DateTime.UtcNow.ToString("o"));
					cmd.ExecuteNonQuery();
				}

				var logCmd = new SQLiteCommand(@"
                INSERT INTO ScanLog (Timestamp, DirectoryScanned, FilesScanned, FilesInUse, UserName)
                VALUES (@ts, @dir, @total, @used, @user);", conn);
				logCmd.Parameters.AddWithValue("@ts", DateTime.UtcNow.ToString("o"));
				logCmd.Parameters.AddWithValue("@dir", scanDir);
				logCmd.Parameters.AddWithValue("@total", files.Length);
				logCmd.Parameters.AddWithValue("@used", filesInUse);
				logCmd.Parameters.AddWithValue("@user", userName);
				logCmd.ExecuteNonQuery();

				conn.Close();
			}

			Console.WriteLine("\n✅ Scan completed. Data saved to FileScan.db");
		}
	}
}