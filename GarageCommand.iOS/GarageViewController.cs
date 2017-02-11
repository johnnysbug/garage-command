using System;
using UIKit;
using System.Threading.Tasks;
using System.ComponentModel;

namespace GarageCommand.iOS
{
	public class GarageViewController : UIViewController
	{
		const float TOP_MARGIN = 60;
		const float MARGIN = 20;
		const float SPACING = 20;

		UIStackView _buttonStack;
		UIStackView _statusStack;
		UIButton _leftButton;
		UIButton _rightButton;
		UILabel _leftStatus;
		UILabel _rightStatus;
		UILabel _connectionStatus;

		Action _action;
		GarageViewModel _garageViewModel;

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();

			_garageViewModel = new GarageViewModel();

			var maxWidth = UIScreen.MainScreen.Bounds.Width - (MARGIN + SPACING + MARGIN);
			var maxHeight = UIScreen.MainScreen.Bounds.Height - (TOP_MARGIN + SPACING + SPACING + TOP_MARGIN);

			var buttonHeight = maxWidth / 2;

			_buttonStack = new UIStackView();
			_statusStack = new UIStackView();
			_leftButton = new UIButton(UIButtonType.System);
			_rightButton = new UIButton(UIButtonType.System);
			_leftStatus = new UILabel();
			_rightStatus = new UILabel();
			_connectionStatus = new UILabel();

			_leftButton.HeightAnchor.ConstraintGreaterThanOrEqualTo(buttonHeight).Active = true;
			_leftButton.WidthAnchor.ConstraintGreaterThanOrEqualTo(maxWidth / 2).Active = true;
			_rightButton.HeightAnchor.ConstraintGreaterThanOrEqualTo(buttonHeight).Active = true;
			_rightButton.WidthAnchor.ConstraintGreaterThanOrEqualTo(maxWidth / 2).Active = true;
			_leftStatus.WidthAnchor.ConstraintGreaterThanOrEqualTo(maxWidth / 2).Active = true;
			_rightStatus.WidthAnchor.ConstraintGreaterThanOrEqualTo(maxWidth / 2).Active = true;
			_connectionStatus.WidthAnchor.ConstraintGreaterThanOrEqualTo(maxWidth).Active = true;

			_buttonStack.ContentMode = UIViewContentMode.ScaleAspectFill;
			_buttonStack.Alignment = UIStackViewAlignment.Fill;
			_buttonStack.Distribution = UIStackViewDistribution.FillEqually;
			_buttonStack.Axis = UILayoutConstraintAxis.Horizontal;
			_buttonStack.TranslatesAutoresizingMaskIntoConstraints = false;
			_buttonStack.Spacing = SPACING;

			_statusStack.ContentMode = UIViewContentMode.ScaleAspectFill;
			_statusStack.Alignment = UIStackViewAlignment.Fill;
			_statusStack.Distribution = UIStackViewDistribution.FillEqually;
			_statusStack.Axis = UILayoutConstraintAxis.Horizontal;
			_statusStack.TranslatesAutoresizingMaskIntoConstraints = false;
			_statusStack.Spacing = SPACING;

			_buttonStack.AddArrangedSubview(_leftButton);
			_buttonStack.AddArrangedSubview(_rightButton);
			_statusStack.AddArrangedSubview(_leftStatus);
			_statusStack.AddArrangedSubview(_rightStatus);

			View.BackgroundColor = UIColor.LightGray;
			_leftButton.BackgroundColor = UIColor.White;
			_rightButton.BackgroundColor = UIColor.White;

			_leftButton.SetTitle("Left Garage", UIControlState.Normal);
			_rightButton.SetTitle("Right Garage", UIControlState.Normal);

			_connectionStatus.Text = _garageViewModel.Status;

			View.AddSubviews(_buttonStack, _statusStack, _connectionStatus);

			View.ConstrainLayout(() =>
				_buttonStack.Frame.Top == View.Frame.Top + 60 &&
				_buttonStack.Frame.Left == View.Frame.Left + MARGIN &&
				_buttonStack.Frame.Right == View.Frame.Left - MARGIN &&
				_buttonStack.Frame.Height == 100 &&
				_statusStack.Frame.Top == _buttonStack.Frame.Bottom + SPACING &&
				_statusStack.Frame.Left == View.Frame.Left + MARGIN &&
				_statusStack.Frame.Right == View.Frame.Left - MARGIN &&
				_statusStack.Frame.Height == 30 &&
				_connectionStatus.Frame.Top == _statusStack.Frame.Bottom + SPACING &&
				_connectionStatus.Frame.Left == View.Frame.Left + MARGIN &&
				_connectionStatus.Frame.Right == View.Frame.Left - MARGIN &&
				_connectionStatus.Frame.Height == 40
			);

			_action = () =>
			{
				BeginInvokeOnMainThread(() =>
				{
					_leftStatus.Text = _garageViewModel.Garages?.LeftGarage?.Status == GarageStatus.Closed
						? "Closed"
						: _garageViewModel.Garages?.LeftGarage?.Status == GarageStatus.Open
						? "Opened"
						: "Unknown";
					_rightStatus.Text = _garageViewModel.Garages?.RightGarage?.Status == GarageStatus.Closed
						? "Closed"
						: _garageViewModel.Garages?.RightGarage?.Status == GarageStatus.Open
						? "Opened"
						: "Unknown";
					_connectionStatus.Text = _garageViewModel.Status;
					_connectionStatus.TextColor = _garageViewModel.IsConnected ? UIColor.Green : UIColor.Orange;
					_leftButton.Enabled = _garageViewModel.IsConnected && _garageViewModel.Garages?.LeftGarage?.Status != GarageStatus.Unknown;
					_rightButton.Enabled = _garageViewModel.IsConnected && _garageViewModel.Garages?.RightGarage?.Status != GarageStatus.Unknown;
				});
			};
		}

		public override void ViewDidAppear(bool animated)
		{
			base.ViewDidAppear(animated);

			UIView.Animate(0.25, () =>
			{
				_buttonStack.LayoutIfNeeded();
			});

			_garageViewModel.PropertyChanged += HandlePropertyChanged;

			Task.Factory.StartNew(async () => await _garageViewModel.Initialize());

			_leftButton.TouchUpInside += LeftButton_TouchUpInside;
			_rightButton.TouchUpInside += RightButton_TouchUpInside;
		}

		public override void ViewWillDisappear(bool animated)
		{
			_leftButton.TouchUpInside -= LeftButton_TouchUpInside;
			_rightButton.TouchUpInside -= RightButton_TouchUpInside;
			_garageViewModel.PropertyChanged += HandlePropertyChanged;
			base.ViewWillDisappear(animated);
		}

		void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			Console.WriteLine(e.PropertyName);
			switch (e.PropertyName)
			{
				case "IsConnected":
				case "Status":
				case "Garages":
					_action();
					break;
			}
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
	}
}