using System;

namespace BTDTool;

public class BTD700BroadcastContext
{
	public _BTD700_BROADCAST_STATE broadcastState;

	public _BTD700_BROADCAST_QUALITY broadcastQuality;

	public _BTD700_BROADCAST_ENCRYPTION broadcastEncrypt;

	public byte[] broadcastEncKey;

	public byte[] broadcastName;

	public bool bBackupValid;

	public byte[] broadcastInfoAsByteArray => new byte[3]
	{
		(byte)broadcastState,
		(byte)broadcastQuality,
		(byte)broadcastEncrypt
	};

	public BTD700BroadcastContext()
	{
		broadcastEncrypt = _BTD700_BROADCAST_ENCRYPTION.BCAST_ENCR_OFF;
		broadcastEncKey = new byte[0];
		broadcastName = new byte[0];
		bBackupValid = false;
	}

	public void Reset()
	{
		broadcastEncrypt = _BTD700_BROADCAST_ENCRYPTION.BCAST_ENCR_OFF;
		broadcastEncKey = new byte[0];
		broadcastName = new byte[0];
		bBackupValid = false;
	}

	public void ImportBroadcastContext(BTD700Context btdctx, bool isBackup = false)
	{
		broadcastState = btdctx.broadcastState;
		broadcastQuality = btdctx.broadcastQuality;
		broadcastEncrypt = btdctx.broadcastEncrypt;
		broadcastName = new byte[0];
		if (btdctx.broadcastName != null && btdctx.broadcastName.Length != 0)
		{
			Array.Copy(btdctx.broadcastName, broadcastName = new byte[btdctx.broadcastName.Length], btdctx.broadcastName.Length);
		}
		broadcastEncKey = new byte[0];
		if (btdctx.broadcastEncKey != null && btdctx.broadcastEncKey.Length != 0)
		{
			Array.Copy(btdctx.broadcastEncKey, broadcastEncKey = new byte[btdctx.broadcastEncKey.Length], btdctx.broadcastEncKey.Length);
		}
		bBackupValid = isBackup;
	}

	public void SetBroadcastInfo(byte pubstatus, byte quality, byte encstatus)
	{
		broadcastState = (_BTD700_BROADCAST_STATE)pubstatus;
		broadcastQuality = (_BTD700_BROADCAST_QUALITY)quality;
		broadcastEncrypt = (_BTD700_BROADCAST_ENCRYPTION)encstatus;
	}

	public void SetBroadcastName(byte[] name = null)
	{
		broadcastName = new byte[0];
		if (name != null && name.Length != 0)
		{
			Array.Copy(name, broadcastName = new byte[name.Length], name.Length);
		}
	}

	public void SetBroadcastEncKey(byte[] encKey = null)
	{
		broadcastEncKey = new byte[0];
		if (encKey != null && encKey.Length != 0)
		{
			Array.Copy(encKey, broadcastEncKey = new byte[encKey.Length], encKey.Length);
		}
	}
}
