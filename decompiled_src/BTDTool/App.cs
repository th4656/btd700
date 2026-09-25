using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.IO.IsolatedStorage;
using System.Windows;
using BTDTool.ViewModels;
using BTDTool.Views;

namespace BTDTool;

public class App : Application
{
	private const ushort usVersionMajor = 1;

	private const byte byVersionMinor = 0;

	private const byte byVersionTest = 5;

	private const string _filename = "App.data";

	private MainAppWindow? mainWindow;

	private MainAppWindowViewModel? mainWindowDataContext;

	private bool _contentLoaded;

	public App()
	{
		base.Properties["SelectedLanguage"] = "English";
	}

	private void Application_Startup(object sender, StartupEventArgs e)
	{
		string processName = Process.GetCurrentProcess().ProcessName;
		_ = Process.GetCurrentProcess().Id;
		if (Process.GetProcessesByName(processName).Length > 1)
		{
			Application.Current.Shutdown();
			return;
		}
		IsolatedStorageFile userStoreForDomain = IsolatedStorageFile.GetUserStoreForDomain();
		try
		{
			if (userStoreForDomain.FileExists("App.data"))
			{
				using IsolatedStorageFileStream stream = userStoreForDomain.OpenFile("App.data", FileMode.Open, FileAccess.Read);
				using StreamReader streamReader = new StreamReader(stream);
				while (!streamReader.EndOfStream)
				{
					string[] array = streamReader.ReadLine().Split(new char[1] { ',' });
					base.Properties[array[0]] = array[1];
				}
			}
		}
		catch (DirectoryNotFoundException)
		{
		}
		catch (IsolatedStorageException)
		{
		}
		int i = 3;
		char c = 'P';
		bool a = false;
		byte[] array2 = null;
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int j = 0; j < commandLineArgs.Length; j++)
		{
			string text = commandLineArgs[j].Trim().ToUpper();
			if (!text.StartsWith("-FID:") || !(text = text.Substring(text.IndexOf(':') + 1)).StartsWith("0X"))
			{
				continue;
			}
			uint num = Convert.ToUInt32(text.Substring(2), 16);
			if (num != 0)
			{
				array2 = new byte[4];
				for (int k = 0; k < 4; k++)
				{
					array2[3 - k] = (byte)(num % 256);
					num /= 256;
				}
			}
		}
		mainWindow = new MainAppWindow(65541uL);
		mainWindowDataContext = new MainAppWindowViewModel(mainWindow, i, c, a, array2);
		mainWindow.DataContext = mainWindowDataContext;
		mainWindow.Show();
	}

	private void Application_Exit(object sender, ExitEventArgs e)
	{
		if (mainWindowDataContext == null)
		{
			return;
		}
		using IsolatedStorageFileStream stream = IsolatedStorageFile.GetUserStoreForDomain().OpenFile("App.data", FileMode.Create, FileAccess.Write);
		using StreamWriter streamWriter = new StreamWriter(stream);
		foreach (string key in base.Properties.Keys)
		{
			streamWriter.WriteLine("{0},{1}", key, base.Properties[key]);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			base.Startup += Application_Startup;
			base.Exit += Application_Exit;
			Uri resourceLocator = new Uri("/Sennheiser Dongle Control;V1.0.5;component/app.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[STAThread]
	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "9.0.8.0")]
	public static void Main()
	{
		App app = new App();
		app.InitializeComponent();
		app.Run();
	}
}
