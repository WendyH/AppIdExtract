/* This code is released under WTFPL Version 2 (http://www.wtfpl.net/). Created by WendyH. */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace AppIdExtract {
	class Program {
		public static string InFile    = "";
		public static string OutFile   = "AppIdExtract.out";
		public static long   SkipBytes = 0;
		public static double Speed     = 0;
		public static double Completed = 0;
		public static Stopwatch Watch  = new Stopwatch();

		public static void Main(string[] args) {
			ShowHeader("AppIdExtract by WendyH v <c:White>" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

			if (args.Length > 0) InFile  = args[0];
			if (args.Length > 1) OutFile = args[1];

			if (args.Length   == 0  ) Program.Quit("<c:Red>Usage:</c>\nAppIdExtract.exe <c:DarkCyan>inputfile</c> [<c:DarkCyan>outputfile</c>]");
			if (InFile.Length == 0  ) Program.Quit("<c:Red>Not specified <c:DarkCyan>input</c> file.");
			if (!File.Exists(InFile)) Program.Quit("<c:Red>Not exists <c:DarkCyan>input</c> file.");

			Watch.Start();

			Regex regex = new Regex(@"store/apps/details\?id=([\w\.\d_]+)[^>]+title=", RegexOptions.Compiled);

			Message("Reading file: <c:DarkCyan>" + InFile);

			string line = "";
			int    apps = 0;
			long   size = 0;
			long   step = 0;
			MatchCollection mc;

			using (StreamWriter writer = File.CreateText(OutFile)) {
				using (StreamReader reader = File.OpenText(InFile)) {
					Message("File size   : <c:DarkCyan>" + reader.BaseStream.Length);
					while ((line = reader.ReadLine()) != null) {
						long part = Watch.ElapsedMilliseconds / 1000;
                        if (part != step) {
							step = part;
							Message(GetConsoleStatusLine(apps, reader.BaseStream.Length, reader.BaseStream.Position));
						}
						mc = regex.Matches(line);
						foreach (Match m in mc) {
							writer.WriteLine(m.Groups[1].Value);
							apps++;
						}
					}
				}
				size = writer.BaseStream.Length;
			}
			Message("");
			Program.Quit("Done. Saved results in <c:DarkCyan>" + OutFile + "</c>. Found apps: <c:Magenta>" + apps + "</c> File size: <c:Magenta>" + size);
		}

		/// <summary>
		/// Get line of status current state of ou for console output
		/// </summary>
		/// <param name="len">Length of line in chars</param>
		/// <returns>Returns the string with progress, speed and time remaining.</returns>
		public static string GetConsoleStatusLine(int apps, long totalBytes, long bytesRead, int len = 64) {
			long totalMS = Watch.ElapsedMilliseconds;
			if ((totalBytes > 0) && (totalMS > 0)) {
				// BytesRead/totalMS is in bytes/ms. Convert to kb/sec.
				Speed     = ((bytesRead - SkipBytes) * 1000.0f) / (totalMS * 1024.0f);
				Completed = bytesRead * 100.0f / totalBytes;
			}
			int speedLen = 15;
			int timeLen  = 12;
			int progressMaxLen = len - speedLen - timeLen - 3;
			int progressLen    = (int)Math.Round(progressMaxLen * Completed / 100);
			string progress    = "[".PadRight(progressLen + 1, '#').PadRight(progressMaxLen + 1, '.') + "]";

			string speed = " ";
			if (Speed > 1024) speed += Math.Round(Speed / 1024, 1) + " MB/sec";
			else              speed += Math.Round(Speed       , 1) + " KB/sec";
			speed = speed.Length > speedLen ? speed.Substring(0, speedLen) : speed.PadRight(speedLen);

			double secLeft = totalMS / 1000.0f;
			int secRemain = 0;
			if (((bytesRead - SkipBytes) > 0) && (totalBytes > 0))
				secRemain = (int)(secLeft * (totalBytes - bytesRead) / (bytesRead - SkipBytes));

			string time = "";
			if      (secRemain == 0  ) time = "";
			else if (secRemain < 60  ) time = secRemain + "s";
			else if (secRemain < 3600) time = String.Format("{0}m {1}s", secRemain / 60, secRemain % 60);
			else                       time = String.Format("{0}h {1}m {2}s", secRemain / 3600, (secRemain / 60) % 60, secRemain % 60);
			if (time.Length > 0) time = " (" + time + ")";
			time = time.Length > timeLen ? time.Substring(0, timeLen) : time.PadRight(timeLen);

			return "Apps: " + apps + " " + (progress + speed + time).PadRight(len - 1) + "\r";
		}

		/// <summary>
		/// Quit the program.
		/// </summary>
		/// <param name="msg">Message with quit. Optional.</param>
		public static void Quit(string msg = "") {
			Message(msg);
			if (Watch != null) {
				Watch.Stop();
				Program.Message("\n\r<c:DarkCyan>Time elapsed: <c:DarkGray>" + Watch.Elapsed);
			}
			Environment.Exit(0);
		}

		/// <summary>
		/// Show program header
		/// </summary>
		/// <param name="header">Header string</param>
		public static void ShowHeader(string header) {
			string h = Regex.Replace(header, @"(<c:\w+>|</c>)", "");
			int width = (Console.WindowWidth / 2 + h.Length / 2);

			Program.Message(String.Format("\n{0," + width.ToString() + "}\n", header));
		}

		/// <summary>
		/// Output message with color tags supported.
		/// </summary>
		/// <param name="msg">Message string</param>
		public static void Message(string msg, bool noNewLine = false) {
			Console.ForegroundColor = ConsoleColor.Gray;
			List<ConsoleColor> colorsStack = new List<ConsoleColor>();
			string[] chars = msg.Split('<'); string spChar = "";
			foreach (string s in chars) {
				string sText = s;
				Match m = Regex.Match(s, @"^c:(\w+)>");
				if (m.Success) {
					sText = s.Replace(m.Groups[0].Value, "");
					try {
						colorsStack.Add(Console.ForegroundColor);
						Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), m.Groups[1].Value);
					} catch { };

				} else if (Regex.IsMatch(s, @"^/c>")) {
					if (colorsStack.Count > 0) {
						Console.ForegroundColor = colorsStack[colorsStack.Count - 1];
						colorsStack.RemoveAt(colorsStack.Count - 1);
					} else
						Console.ResetColor();
					sText = s.Substring(3);

				} else sText = spChar + sText;
				Console.Write(sText);
				spChar = "<";
			}
			if (!noNewLine && !msg.EndsWith("\r")) Console.Write("\n\r");
			Console.ResetColor();
		}

	}

}
