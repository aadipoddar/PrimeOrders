using Microsoft.Maui.Layouts;

namespace PrimeBakes.Controls;

public class MySlideView : AbsoluteLayout
{

	public View MyTrackBar
	{
		get { return (View)GetValue(TrackBarProperty); }
		set { SetValue(TrackBarProperty, value); }
	}

	public static readonly BindableProperty TrackBarProperty =
		BindableProperty.Create(
			"MyTrackBar", typeof(View), typeof(MySlideView),
			defaultValue: default(View));

	public View MyFillBar
	{
		get { return (View)GetValue(FillBarProperty); }
		set { SetValue(FillBarProperty, value); }
	}

	public static readonly BindableProperty FillBarProperty =
		BindableProperty.Create(
			"MyFillBar", typeof(View), typeof(MySlideView),
			defaultValue: default(View));

	public View MyThumb
	{
		get { return (View)GetValue(ThumbProperty); }
		set { SetValue(ThumbProperty, value); }
	}

	public static readonly BindableProperty ThumbProperty =
   BindableProperty.Create(
	   "MyThumb", typeof(View), typeof(MySlideView),
	   defaultValue: default(View));


	private PanGestureRecognizer _panGesture = new PanGestureRecognizer();
	private View _gestureListener;
	public MySlideView()
	{
		_panGesture.PanUpdated += OnPanGestureUpdated;
		SizeChanged += OnSizeChanged;

		_gestureListener = new ContentView { BackgroundColor = Colors.White, Opacity = 0.05 };
		_gestureListener.GestureRecognizers.Add(_panGesture);
	}

	public event EventHandler SlideCompleted;

	private const double _fadeEffect = 0.5;
	private const uint _animLength = 50;
	async void OnPanGestureUpdated(object sender, PanUpdatedEventArgs e)
	{
		if (MyThumb == null || MyTrackBar == null || MyFillBar == null)
			return;

		switch (e.StatusType)
		{
			case GestureStatus.Started:
				await MyTrackBar.FadeTo(_fadeEffect, _animLength);
				break;

			case GestureStatus.Running:

				var x = Math.Max(0, e.TotalX);
				var y = Math.Max(0, e.TotalY);
				if (x > (Width - MyThumb.Width))
					x = (Width - MyThumb.Width);
				if (y > (Height - MyThumb.Height)) y = (Height - MyThumb.Height);

				MyThumb.TranslationX = x;
				SetLayoutBounds((IView)MyFillBar, new Rect(0, 0, x + MyThumb.Width / 2, Height));
				break;

			case GestureStatus.Completed:
				var posX = MyThumb.TranslationX;
				SetLayoutBounds((IView)MyFillBar, new Rect(0, 0, 0, this.Height));


				await Task.WhenAll(new Task[]{
				MyTrackBar.FadeTo(1, _animLength),
				MyThumb.TranslateTo(0, 0, _animLength * 2, Easing.CubicIn),
			});

				if (posX >= (Width - MyThumb.Width - 10/* keep some margin for error*/))
					SlideCompleted?.Invoke(this, EventArgs.Empty);
				break;
		}
	}

	void OnSizeChanged(object sender, EventArgs e)
	{
		if (Width == 0 || Height == 0)
			return;
		if (MyThumb == null || MyTrackBar == null || MyFillBar == null)
			return;


		Children.Clear();

		SetLayoutFlags((IView)MyTrackBar, AbsoluteLayoutFlags.SizeProportional);
		SetLayoutBounds((IView)MyTrackBar, new Rect(0, 0, 1, 1));
		Children.Add(MyTrackBar);

		SetLayoutFlags((IView)MyFillBar, AbsoluteLayoutFlags.None);
		SetLayoutBounds((IView)MyFillBar, new Rect(0, 0, 0, this.Height));
		Children.Add(MyFillBar);

		SetLayoutFlags((IView)MyThumb, AbsoluteLayoutFlags.None);
		SetLayoutBounds((IView)MyThumb, new Rect(0, 0, this.Width / 5, this.Height));
		Children.Add(MyThumb);

		SetLayoutFlags((IView)_gestureListener, AbsoluteLayoutFlags.SizeProportional);
		SetLayoutBounds((IView)_gestureListener, new Rect(0, 0, 1, 1));
		Children.Add(_gestureListener);

	}
}
