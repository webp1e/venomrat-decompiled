using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using Microsoft.Win32;

namespace Client;

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
