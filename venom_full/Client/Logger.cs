using System.IO;

namespace Client;

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
