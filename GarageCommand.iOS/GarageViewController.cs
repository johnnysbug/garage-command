using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Foundation;
using GarageCommand.Core;
using GarageCommand.Core.ViewModels;
using UIKit;

namespace GarageCommand.iOS
{
	public class GarageViewController : UIViewController
	{
		const float MARGIN = 10;
		const float SPACING = 5;

		NSObject _didBecomeActiveObserver;

		UIStackView _buttonStack;
		UIButton _leftButton;
		UILabel _leftLetter;
		UILabel _leftStatus;

		UIButton _rightButton;
		UILabel _rightLetter;
		UILabel _rightStatus;

		UILabel _connectionStatus;

		GarageViewModel _garageViewModel;

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			var maxWidth = UIScreen.MainScreen.Bounds.Width - (MARGIN + SPACING + MARGIN);
			var buttonHeight = maxWidth * 2;

			_garageViewModel = new GarageViewModel();

			_buttonStack = new UIStackView();
			_leftButton = new UIButton(UIButtonType.Custom);
			_rightButton = new UIButton(UIButtonType.Custom);
			_leftStatus = new UILabel();
			_rightStatus = new UILabel();
			_connectionStatus = new UILabel();

			_leftButton.HeightAnchor.ConstraintEqualTo(buttonHeight).Active = true;
			_leftButton.WidthAnchor.ConstraintEqualTo(maxWidth / 2).Active = true;
			_rightButton.HeightAnchor.ConstraintEqualTo(buttonHeight).Active = true;
			_rightButton.WidthAnchor.ConstraintEqualTo(maxWidth / 2).Active = true;
			_connectionStatus.WidthAnchor.ConstraintEqualTo(maxWidth).Active = true;

			_buttonStack.ContentMode = UIViewContentMode.ScaleAspectFill;
			_buttonStack.Alignment = UIStackViewAlignment.Fill;
			_buttonStack.Distribution = UIStackViewDistribution.FillEqually;
			_buttonStack.Axis = UILayoutConstraintAxis.Horizontal;
			_buttonStack.TranslatesAutoresizingMaskIntoConstraints = false;
			_buttonStack.Spacing = MARGIN / 2;

			_buttonStack.AddArrangedSubview(_leftButton);
			_buttonStack.AddArrangedSubview(_rightButton);

			View.BackgroundColor = UIColor.White;

			_leftLetter = new UILabel {
				Text = "L",
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Font = UIFont.SystemFontOfSize(150, UIFontWeight.Semibold),
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_leftLetter.Layer.Opacity = 0.4f;

			_leftStatus = new UILabel {
				Text = "Unknown",
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Font = UIFont.SystemFontOfSize(16.0f, UIFontWeight.Regular),
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			_leftButton.BackgroundColor = UIColor.FromRGB(65, 217, 73);

			_rightLetter = new UILabel {
				Text = "R",
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Font = UIFont.SystemFontOfSize(150, UIFontWeight.Semibold),
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			_rightLetter.Layer.Opacity = 0.4f;

			_rightStatus = new UILabel {
				Text = "Unknown",
				TextAlignment = UITextAlignment.Center,
				TextColor = UIColor.White,
				Font = UIFont.SystemFontOfSize(16.0f, UIFontWeight.Regular),
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			_rightButton.BackgroundColor = UIColor.FromRGB(247, 210, 0);

			_connectionStatus.Layer.Opacity = 0;
			_buttonStack.Layer.Opacity = 0;

			_connectionStatus.Text = _garageViewModel.Status;
			_connectionStatus.TextAlignment = UITextAlignment.Center;
			_connectionStatus.Font = UIFont.BoldSystemFontOfSize(16.0f);

			_leftButton.AddSubviews(_leftLetter, _leftStatus);
			_rightButton.AddSubviews(_rightLetter, _rightStatus);

			View.AddSubviews(_buttonStack, _connectionStatus);

			View.ConstrainLayout(() =>
				_buttonStack.Frame.GetCenterY() == View.Frame.GetCenterY() &&
				_buttonStack.Frame.Left == View.Frame.Left + MARGIN &&
				_buttonStack.Frame.Right == View.Frame.Left - MARGIN &&
				_buttonStack.Frame.Height == View.Frame.Height * 0.5f &&

				_connectionStatus.Frame.Top == _buttonStack.Frame.Top * 0.5f &&
				_connectionStatus.Frame.GetCenterX() == View.Frame.GetCenterX() &&
				_connectionStatus.Frame.Height == 40
			);

			_leftButton.ConstrainLayout(() =>
				_leftLetter.Frame.GetCenterY() == _leftButton.Frame.GetCenterY() &&
				_leftLetter.Frame.GetCenterX() == _leftButton.Frame.GetCenterX() &&

				_leftStatus.Frame.GetCenterX() == _leftButton.Frame.GetCenterX() &&
				_leftStatus.Frame.Top == _leftLetter.Frame.Bottom - 20.0f
			);

			_rightButton.ConstrainLayout(() =>
				_rightLetter.Frame.GetCenterY() == _rightButton.Frame.GetCenterY() &&
				_rightLetter.Frame.GetCenterX() == _rightButton.Frame.GetCenterX() &&

				_rightStatus.Frame.GetCenterX() == _rightButton.Frame.GetCenterX() &&
				_rightStatus.Frame.Top == _rightLetter.Frame.Bottom - 20.0f
			);

			_didBecomeActiveObserver = NSNotificationCenter.DefaultCenter.AddObserver(UIApplication.DidBecomeActiveNotification, HandleDidBecomeActiveNotification);
		}

		nfloat GetOffsetForCenter(nfloat parentHeight, nfloat childHeight)
		{
			return parentHeight * 0.5f - childHeight * 0.5f;
		}

		public override void ViewDidLayoutSubviews()
		{
			base.ViewDidLayoutSubviews();
			_leftButton.Layer.CornerRadius = _leftButton.Frame.Width / 2;
			_rightButton.Layer.CornerRadius = _rightButton.Frame.Width / 2;
		}

		public override void ViewWillAppear(bool animated)
		{
			base.ViewWillAppear(animated);

			_garageViewModel.PropertyChanged += HandlePropertyChanged;
			_leftButton.TouchUpInside += LeftButton_TouchUpInside;
			_rightButton.TouchUpInside += RightButton_TouchUpInside;
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);
			UIView.AnimateNotify(0.4d, 0.0d, UIViewAnimationOptions.CurveEaseInOut, () =>
			{
				_connectionStatus.Layer.Opacity = 1f;
				_buttonStack.Layer.Opacity = 1f;
			}, (finished) => { });
		}

		public override void ViewWillDisappear(bool animated)
		{
			_garageViewModel.Disconnect();
			_leftButton.TouchUpInside -= LeftButton_TouchUpInside;
			_rightButton.TouchUpInside -= RightButton_TouchUpInside;
			_garageViewModel.PropertyChanged -= HandlePropertyChanged;

			if (_didBecomeActiveObserver != null)
			{
				NSNotificationCenter.DefaultCenter.RemoveObserver(_didBecomeActiveObserver);
				_didBecomeActiveObserver = null;
			}

			base.ViewWillDisappear(animated);
		}

		void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Console.WriteLine($"ViewController handling PropertyChanged for [{e.PropertyName}]");

			BeginInvokeOnMainThread(() =>
			{
				switch (e.PropertyName)
				{
					case "IsConnected":
					case "Status":
					case "Garages":
						UpdateUI();
						break;
				}
			});
		}

		void LeftButton_TouchUpInside(object sender, EventArgs e)
		{
			var alert = UIAlertController.Create("Left Garage", null, UIAlertControllerStyle.ActionSheet);
			var currentStatus = _garageViewModel.Garages.LeftGarage.Status;

			alert.AddAction(UIAlertAction.Create(currentStatus == GarageStatus.Closed ? "Open" : "Close", UIAlertActionStyle.Default, (obj) =>
			{
				Task.Factory.StartNew(async () =>
				{
					await _garageViewModel.Toggle("left");
				});
			}));

			alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));

			PresentViewController(alert, true, alert.Dispose);
		}

		void RightButton_TouchUpInside(object sender, EventArgs e)
		{
			var alert = UIAlertController.Create("Right Garage", null, UIAlertControllerStyle.ActionSheet);
			var currentStatus = _garageViewModel.Garages.RightGarage.Status;

			alert.AddAction(UIAlertAction.Create(currentStatus == GarageStatus.Closed ? "Open" : "Close", UIAlertActionStyle.Default, (obj) =>
			{
				Task.Factory.StartNew(async () =>
				{
					await _garageViewModel.Toggle("right");
				});
			}));

			alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, null));

			PresentViewController(alert, true, alert.Dispose);
		}

		void HandleDidBecomeActiveNotification(NSNotification notification)
		{
			if (!_garageViewModel.IsConnected)
			{
				Task.Factory.StartNew(async () => await _garageViewModel.Initialize());
			}
			else
			{
				UpdateUI();
			}
		}

		void UpdateUI()
		{
			Console.WriteLine($"ViewController Updating UI");

			UIView.AnimateNotify(0.4d, 0.0d, UIViewAnimationOptions.CurveEaseInOut, () =>
			{
				_leftStatus.Text = _garageViewModel.LeftGarageStatus;
				_rightStatus.Text = _garageViewModel.RightGarageStatus;

				_leftButton.Enabled = _garageViewModel.IsConnected
					&& _garageViewModel.Garages?.LeftGarage?.Status != GarageStatus.Unknown;

				_rightButton.Enabled = _garageViewModel.IsConnected
					&& _garageViewModel.Garages?.RightGarage?.Status != GarageStatus.Unknown;

				_leftButton.BackgroundColor = _garageViewModel.IsConnected && _garageViewModel.Garages?.LeftGarage?.Status == GarageStatus.Open
					? UIColor.FromRGB(57, 191, 64)
					: _garageViewModel.IsConnected && _garageViewModel.Garages?.LeftGarage?.Status == GarageStatus.Closed
					? UIColor.FromRGB(65, 217, 73)
					: UIColor.FromRGB(211, 218, 212);
				_rightButton.BackgroundColor = _garageViewModel.IsConnected && _garageViewModel.Garages?.RightGarage?.Status == GarageStatus.Open
					? UIColor.FromRGB(196, 167, 0)
					: _garageViewModel.IsConnected && _garageViewModel.Garages?.RightGarage?.Status == GarageStatus.Closed
					? UIColor.FromRGB(247, 210, 0)
					: UIColor.FromRGB(211, 218, 212);

				_connectionStatus.Text = _garageViewModel.Status;
				_connectionStatus.TextColor = _garageViewModel.IsConnected ? UIColor.FromRGB(65, 217, 73) : UIColor.Black;
			}, (finished) => { });
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Console.WriteLine($"Disposing {GetType().Name}");
				if (_garageViewModel != null)
				{
					_garageViewModel.Dispose();
					_garageViewModel = null;
				}
			}
			base.Dispose(disposing);
		}
	}
}