using System;
using System.Windows;

namespace BTDTool.ViewModels;

internal class AppToastWindowViewModel : ViewModelBase, IDisposable
{
	private string _MainTitle = "";

	private string _MainContent = "";

	private int _ImageIndex;

	private Visibility _Button1Visibility;

	private string _Button1Caption = "";

	private Visibility _Button2Visibility = Visibility.Collapsed;

	private string _Button2Caption = "";

	private Visibility _Button3Visibility = Visibility.Collapsed;

	private string _Button3Caption = "";

	public int iResult;

	public string MainTitle
	{
		get
		{
			return _MainTitle;
		}
		set
		{
			_MainTitle = value;
			OnPropertyChanged("MainTitle");
		}
	}

	public string MainContent
	{
		get
		{
			return _MainContent;
		}
		set
		{
			_MainContent = value;
			OnPropertyChanged("MainContent");
		}
	}

	public int ImageIndex
	{
		get
		{
			return _ImageIndex;
		}
		set
		{
			_ImageIndex = value;
			OnPropertyChanged("ImageIndex");
		}
	}

	public Visibility Button1Visibility
	{
		get
		{
			return _Button1Visibility;
		}
		set
		{
			_Button1Visibility = value;
			OnPropertyChanged("Button1Visibility");
		}
	}

	public string Button1Caption
	{
		get
		{
			return _Button1Caption;
		}
		set
		{
			_Button1Caption = value;
			OnPropertyChanged("Button1Caption");
			Button1Visibility = ((value == "") ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public Visibility Button2Visibility
	{
		get
		{
			return _Button2Visibility;
		}
		set
		{
			_Button2Visibility = value;
			OnPropertyChanged("Button2Visibility");
		}
	}

	public string Button2Caption
	{
		get
		{
			return _Button2Caption;
		}
		set
		{
			_Button2Caption = value;
			OnPropertyChanged("Button2Caption");
			Button2Visibility = ((value == "") ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public Visibility Button3Visibility
	{
		get
		{
			return _Button3Visibility;
		}
		set
		{
			_Button3Visibility = value;
			OnPropertyChanged("Button3Visibility");
		}
	}

	public string Button3Caption
	{
		get
		{
			return _Button3Caption;
		}
		set
		{
			_Button3Caption = value;
			OnPropertyChanged("Button3Caption");
			Button3Visibility = ((value == "") ? Visibility.Collapsed : Visibility.Visible);
		}
	}

	public void SetButtonCaptions(string bt1 = "", string bt2 = "", string bt3 = "")
	{
		Button1Caption = bt1;
		Button2Caption = bt2;
		Button3Caption = bt3;
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}
}
