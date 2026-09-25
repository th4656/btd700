using System;

namespace BTDTool;

public static class HostBuilder
{
	public const byte BTD700_REPORT_ID = 52;

	public static byte[] CmdBytes(_BTD700_HOSTCMD cmd, byte[]? args = null)
	{
		byte[] array = new byte[4 + ((args != null) ? args.Length : 0)];
		array[0] = 52;
		array[1] = 254;
		array[2] = (byte)cmd;
		array[3] = (byte)((args != null) ? ((uint)args.Length) : 0u);
		if (args != null)
		{
			Array.Copy(args, 0, array, 4, args.Length);
		}
		return array;
	}

	public static byte[] RespBytes(_BTD700_DONGLECMD cmd)
	{
		return new byte[4]
		{
			52,
			253,
			(byte)cmd,
			0
		};
	}
}
