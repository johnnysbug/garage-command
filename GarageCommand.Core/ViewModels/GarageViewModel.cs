using System;
using System.Threading.Tasks;
using GarageCommand.Core.Services;

namespace GarageCommand.Core.ViewModels
{

	public class GarageViewModel : ObservableBase
	{
		const string CONNECTING_STATUS = "CONNECTING...";
		const string CONNECTED_STATUS = "CONNECTED  :D";
		const string NOT_CONNECTED_STATUS = "DISCONNECTED :(";

		readonly GarageService _service;
		bool _isConnected;
		string _status;
		Garages _garages;

		public GarageViewModel()
		{
			_service = new GarageService();
			_service.GaragesChanged += HandleStatusChanged;
			_service.ConnectionChanged += HandleConnectionChanged;
		}

		public bool IsConnected
		{
			get { return _isConnected; }
			set { SetField(ref _isConnected, value); }
		}

		public string Status
		{
			get { return _status; }
			set { SetField(ref _status, value); }
		}

		public string LeftGarageStatus => IsConnected && Garages?.LeftGarage?.Status == GarageStatus.Open
											? "Opened"
											: IsConnected && Garages?.LeftGarage?.Status == GarageStatus.Closed
												? "Closed"
												: "Unknown";

		public string RightGarageStatus => IsConnected && Garages?.RightGarage?.Status == GarageStatus.Open
											? "Opened"
											: IsConnected && Garages?.RightGarage?.Status == GarageStatus.Closed
												? "Closed"
												: "Unknown";

		public Garages Garages
		{
			get
			{
				return _garages;
			}
			set
			{
				SetField(ref _garages, value);
			}
		}

		public async Task Initialize()
		{
			Console.WriteLine($"ViewModel Initialize");
			Status = CONNECTING_STATUS;
			await _service.Connect();
			Status = _service.IsConnected() ? CONNECTED_STATUS : NOT_CONNECTED_STATUS;
		}

		public void Disconnect()
		{
			Console.WriteLine($"ViewModel Disconnect");
			_service.Disconnect();
		}

		public async Task Toggle(string side)
		{
			if (!IsConnected)
			{
				await Initialize();
			}
			if (IsConnected)
			{
				await _service.Toggle(side);
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				Console.WriteLine($"Disposing {GetType().Name}");
				if (_service != null)
				{
					_service.Disconnect();
					_service.GaragesChanged -= HandleStatusChanged;
					_service.Dispose();
				}
			}
			base.Dispose(disposing);
		}

		void HandleStatusChanged(object sender, GaragesChangedEventArgs e)
		{
			Console.WriteLine($"ViewModel handling StatusChanged event with [{e.Garages}]");
			Garages = e.Garages;
			IsConnected = _service.IsConnected();
			Status = _service.IsConnected() ? CONNECTED_STATUS : NOT_CONNECTED_STATUS;
		}

		void HandleConnectionChanged(object sender, ConnectionStatusEventArgs e)
		{
			Console.WriteLine($"ViewModel handling ConnectionChanged event with [IsConnected={e.IsConnected}]");
			IsConnected = e.IsConnected;
			Status = IsConnected ? CONNECTED_STATUS : NOT_CONNECTED_STATUS;
		}
	}
}