using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace BTDTool.Views;

public class GifImage : Image
{
	private bool _isInitialized;

	private GifBitmapDecoder _gifDecoder;

	private Int32Animation _animation;

	public static readonly DependencyProperty FrameIndexProperty;

	public static readonly DependencyProperty AutoStartProperty;

	public static readonly DependencyProperty GifSourceProperty;

	public static readonly DependencyProperty RepeatProperty;

	public static EventHandler animationCompletedHandler;

	public int FrameIndex
	{
		get
		{
			return (int)GetValue(FrameIndexProperty);
		}
		set
		{
			SetValue(FrameIndexProperty, value);
		}
	}

	public bool AutoStart
	{
		get
		{
			return (bool)GetValue(AutoStartProperty);
		}
		set
		{
			SetValue(AutoStartProperty, value);
		}
	}

	public string GifSource
	{
		get
		{
			return (string)GetValue(GifSourceProperty);
		}
		set
		{
			SetValue(GifSourceProperty, value);
		}
	}

	public int Repeat
	{
		get
		{
			return (int)GetValue(RepeatProperty);
		}
		set
		{
			SetValue(RepeatProperty, value);
		}
	}

	public event EventHandler Completed
	{
		add
		{
			animationCompletedHandler = value;
		}
		remove
		{
			if (animationCompletedHandler != null && _animation != null)
			{
				_animation.Completed -= animationCompletedHandler;
			}
		}
	}

	private void Initialize()
	{
		_gifDecoder = new GifBitmapDecoder(new Uri("pack://application:,,," + GifSource), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
		int seconds = _gifDecoder.Frames.Count / 20;
		int milliseconds = (int)(((double)_gifDecoder.Frames.Count / 20.0 - (double)(_gifDecoder.Frames.Count / 20)) * 1000.0);
		TimeSpan timeSpan = new TimeSpan(0, 0, 0, seconds, milliseconds);
		Duration duration = new Duration(timeSpan);
		RepeatBehavior repeatBehavior = ((Repeat == -1) ? RepeatBehavior.Forever : new RepeatBehavior((Repeat + 1) * timeSpan));
		_animation = new Int32Animation(0, _gifDecoder.Frames.Count - 1, duration);
		_animation.RepeatBehavior = repeatBehavior;
		if (animationCompletedHandler != null)
		{
			_animation.Completed += animationCompletedHandler;
		}
		base.Source = _gifDecoder.Frames[0];
		_isInitialized = true;
	}

	static GifImage()
	{
		FrameIndexProperty = DependencyProperty.Register("FrameIndex", typeof(int), typeof(GifImage), new UIPropertyMetadata(0, ChangingFrameIndex));
		AutoStartProperty = DependencyProperty.Register("AutoStart", typeof(bool), typeof(GifImage), new UIPropertyMetadata(false, AutoStartPropertyChanged));
		GifSourceProperty = DependencyProperty.Register("GifSource", typeof(string), typeof(GifImage), new UIPropertyMetadata(string.Empty, GifSourcePropertyChanged));
		RepeatProperty = DependencyProperty.Register("Repeat", typeof(int), typeof(GifImage), new UIPropertyMetadata(0, GifSourcePropertyChanged));
		animationCompletedHandler = null;
		UIElement.VisibilityProperty.OverrideMetadata(typeof(GifImage), new FrameworkPropertyMetadata(VisibilityPropertyChanged));
	}

	private static void VisibilityPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if ((Visibility)e.NewValue == Visibility.Visible)
		{
			((GifImage)sender).StartAnimation();
		}
		else
		{
			((GifImage)sender).StopAnimation();
		}
	}

	private static void ChangingFrameIndex(DependencyObject obj, DependencyPropertyChangedEventArgs ev)
	{
		GifImage obj2 = obj as GifImage;
		obj2.Source = obj2._gifDecoder.Frames[(int)ev.NewValue];
	}

	private static void AutoStartPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		if ((bool)e.NewValue)
		{
			(sender as GifImage).StartAnimation();
		}
	}

	private static void GifSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		(sender as GifImage).Initialize();
	}

	public void StartAnimation()
	{
		if (!_isInitialized)
		{
			Initialize();
		}
		BeginAnimation(FrameIndexProperty, _animation);
	}

	public void StopAnimation()
	{
		BeginAnimation(FrameIndexProperty, null);
	}
}
