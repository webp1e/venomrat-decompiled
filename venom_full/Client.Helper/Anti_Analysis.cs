using System;
using System.Collections;
using System.Linq;
using System.Management;

namespace Client.Helper;

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
