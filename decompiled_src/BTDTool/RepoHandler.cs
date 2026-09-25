using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using BTDTool.ViewModels;

namespace BTDTool;

internal class RepoHandler
{
	public class VersionResp
	{
		public bool success { get; set; }

		public List<string>? data { get; set; }

		public uint GetLatestVersionAsU32()
		{
			uint num = 0u;
			if (success && data != null)
			{
				foreach (string datum in data)
				{
					string[] array = datum.Split('.');
					uint num2 = (uint)(1000000 * Convert.ToInt16(array[0]) + 1000 * Convert.ToInt16(array[1]) + Convert.ToInt16(array[2]));
					if (num2 > num)
					{
						num = num2;
					}
				}
			}
			return num;
		}
	}

	public class Compatibility
	{
		public string min { get; set; }

		public string max { get; set; }
	}

	public class Package
	{
		public string size { get; set; }

		public string type { get; set; }

		public string identifier { get; set; }

		public string version { get; set; }

		public uint version_int { get; set; }

		public ulong ts { get; set; }

		public string url { get; set; }
	}

	public class ManifestData
	{
		public Compatibility smartctrl_compatibility { get; set; }

		public string systemRelease { get; set; }

		public List<Package> packages { get; set; }

		public byte[] extras { get; set; }
	}

	public class VersionManifest
	{
		public bool success { get; set; }

		public ManifestData data { get; set; }

		public string GetVersionNumber()
		{
			if (data != null)
			{
				return data.systemRelease;
			}
			return "";
		}

		public string GetManifestDetails()
		{
			string text = "";
			if (data != null)
			{
				text = "This version contains " + data.packages.Count + " file(s) :" + Environment.NewLine;
				foreach (Package package in data.packages)
				{
					text = text + package.url + Environment.NewLine + ((package.size == null) ? "" : (package.size + " byte(s)" + Environment.NewLine));
				}
			}
			return text;
		}

		public string GetDFUFileURL()
		{
			string result = "";
			if (data != null)
			{
				foreach (Package package in data.packages)
				{
					if (package.url.Trim().Substring(package.url.Trim().Length - 4).ToUpper()
						.Equals(".BIN"))
					{
						result = package.url.Trim();
						break;
					}
				}
			}
			return result;
		}
	}

	public class ReleaseNotes
	{
		public string en { get; set; }

		public string de { get; set; }

		public string fr { get; set; }

		public string es { get; set; }

		public string ru { get; set; }

		public string ko { get; set; }

		public string zh { get; set; }

		public string jp { get; set; }

		public string zz { get; set; }
	}

	public class ExtrasData
	{
		public ReleaseNotes release_notes { get; set; }

		public int bt_stack { get; set; }
	}

	public class Extras
	{
		public bool success { get; set; }

		public ExtrasData data { get; set; }
	}

	public static string PRODSKUCODE;

	public static string APIBASEURL;

	public static readonly HttpClient httpClient;

	public static HttpStatusCode LastErrorCode;

	public static VersionResp versionResp;

	public static VersionManifest versionManifest;

	public static Extras extras;

	public static string DFUFileLocalPath;

	public static MainAppWindowViewModel mf;

	static RepoHandler()
	{
		PRODSKUCODE = "";
		APIBASEURL = "";
		mf = null;
		if (httpClient == null)
		{
			httpClient = new HttpClient();
		}
		LastErrorCode = (HttpStatusCode)0;
		DFUFileLocalPath = "";
	}

	public static void SetBaseURL(string strBaseURL)
	{
		APIBASEURL = strBaseURL;
		if (mf != null)
		{
			mf.addLogEntry("Base URL: " + APIBASEURL);
		}
	}

	public static void SetSKUCode(string skucode)
	{
		PRODSKUCODE = skucode;
	}

	public static void SetCaller(MainAppWindowViewModel m)
	{
		mf = m;
	}

	public static void Reset()
	{
		versionResp = null;
		versionManifest = null;
		extras = null;
		DFUFileLocalPath = "";
	}

	public static async void DownloadDFUFileFromRepo()
	{
		string text = "";
		if (versionManifest != null && versionManifest.data != null)
		{
			foreach (Package package in versionManifest.data.packages)
			{
				if (package.url.Trim().Substring(package.url.Trim().Length - 4).ToUpper()
					.Equals(".BIN"))
				{
					text = package.url.Trim();
					if (package.size != null)
					{
						int.Parse(package.size);
					}
					break;
				}
			}
		}
		try
		{
			if (text != "")
			{
				string localfilepath = AppDomain.CurrentDomain.BaseDirectory + Path.GetFileName(text);
				if (File.Exists(localfilepath))
				{
					File.Delete(localfilepath);
				}
				DFUFileLocalPath = "";
				File.WriteAllBytes(localfilepath, await httpClient.GetByteArrayAsync(text));
				mf.addLogEntry("Remote downloaded: " + localfilepath);
				DFUFileLocalPath = localfilepath;
			}
		}
		catch (Exception ex)
		{
			mf.addLogEntry(ex.ToString());
			mf.VariableDFUNotification = "";
			mf.VariableDFUNotificationSub = "";
			mf.PostEvent(_EVENT.NETWORK_ERROR);
			LastErrorCode = HttpStatusCode.GatewayTimeout;
		}
	}

	public static async void GetAvailableVersions()
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		try
		{
			string text = APIBASEURL + "availableSystemReleases/" + PRODSKUCODE + "?os_type=android";
			if (mf != null)
			{
				mf.addLogEntry("GetAvailableVersions() : " + text);
			}
			HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(text);
			LastErrorCode = httpResponseMessage.StatusCode;
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				Encoding uTF = Encoding.UTF8;
				string @string = uTF.GetString(await httpResponseMessage.Content.ReadAsByteArrayAsync());
				mf.addLogEntry("Success: " + @string);
				versionResp = JsonSerializer.Deserialize<VersionResp>(@string);
			}
			else
			{
				mf.addLogEntry("Unexpected response: " + (int)httpResponseMessage.StatusCode + " - " + httpResponseMessage.ReasonPhrase);
				mf.VariableDFUNotification = (int)httpResponseMessage.StatusCode + " - " + httpResponseMessage.ReasonPhrase;
				MainAppWindowViewModel mainAppWindowViewModel = mf;
				mainAppWindowViewModel.VariableDFUNotificationSub = await httpResponseMessage.Content.ReadAsStringAsync();
				mf.PostEvent(_EVENT.NETWORK_ERROR);
			}
		}
		catch (Exception ex)
		{
			mf.addLogEntry(ex.ToString());
			mf.VariableDFUNotification = "";
			mf.VariableDFUNotificationSub = "";
			mf.PostEvent(_EVENT.NETWORK_ERROR);
			LastErrorCode = HttpStatusCode.GatewayTimeout;
		}
	}

	public static string ConvertVersionFromU32ToString(uint version)
	{
		return $"{version / 1000000}.{version % 1000000 / 1000}.{version % 1000}";
	}

	public static async void GetVersionManifest(string version)
	{
		ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
		DFUFileLocalPath = "";
		try
		{
			string text = APIBASEURL + "systemRelease/" + PRODSKUCODE + "/" + version + "?os_type=android";
			if (mf != null)
			{
				mf.addLogEntry("GetVersionManifest() : " + text);
			}
			HttpResponseMessage httpResponseMessage = await httpClient.GetAsync(text);
			LastErrorCode = httpResponseMessage.StatusCode;
			if (httpResponseMessage.IsSuccessStatusCode)
			{
				Encoding uTF = Encoding.UTF8;
				versionManifest = JsonSerializer.Deserialize<VersionManifest>(uTF.GetString(await httpResponseMessage.Content.ReadAsByteArrayAsync()));
				text = APIBASEURL + "getExtras/" + PRODSKUCODE + "/" + version + "?os_type=android";
				if (mf != null)
				{
					mf.addLogEntry("GetVersionManifest() RelNote : " + text);
				}
				httpResponseMessage = await httpClient.GetAsync(text);
				LastErrorCode = httpResponseMessage.StatusCode;
				if (httpResponseMessage.IsSuccessStatusCode)
				{
					uTF = Encoding.UTF8;
					extras = JsonSerializer.Deserialize<Extras>(uTF.GetString(await httpResponseMessage.Content.ReadAsByteArrayAsync()));
					return;
				}
				mf.addLogEntry("Unexpected response: " + (int)httpResponseMessage.StatusCode + " - " + httpResponseMessage.ReasonPhrase);
				mf.VariableDFUNotification = (int)httpResponseMessage.StatusCode + " - " + httpResponseMessage.ReasonPhrase;
				MainAppWindowViewModel mainAppWindowViewModel = mf;
				mainAppWindowViewModel.VariableDFUNotificationSub = await httpResponseMessage.Content.ReadAsStringAsync();
				mf.PostEvent(_EVENT.NETWORK_ERROR);
			}
			else
			{
				mf.addLogEntry("Unexpected response: " + (int)httpResponseMessage.StatusCode + " - " + httpResponseMessage.ReasonPhrase);
				mf.VariableDFUNotification = (int)httpResponseMessage.StatusCode + " - " + httpResponseMessage.ReasonPhrase;
				MainAppWindowViewModel mainAppWindowViewModel = mf;
				mainAppWindowViewModel.VariableDFUNotificationSub = await httpResponseMessage.Content.ReadAsStringAsync();
				mf.PostEvent(_EVENT.NETWORK_ERROR);
			}
		}
		catch (Exception ex)
		{
			mf.addLogEntry(ex.ToString());
			mf.VariableDFUNotification = "";
			mf.VariableDFUNotificationSub = "";
			mf.PostEvent(_EVENT.NETWORK_ERROR);
		}
	}
}
