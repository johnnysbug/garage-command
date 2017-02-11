using System.Threading.Tasks;

namespace GarageCommand.iOS
{

	public class GarageViewModel : ObservableBase
	{
		const string CONNECTING_STATUS = "Connecting...";
		const string CONNECTED_STATUS = "Connected";

		readonly GarageService _service;
		Garages _garages;

		public GarageViewModel()
		{
			_service = new GarageService();
			_service.StatusChanged += HandleStatusChanged;
		}

		public bool IsConnected => _service.IsConnected();
		public string Status => IsConnected ? CONNECTED_STATUS : CONNECTING_STATUS;

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
			await _service.Connect();
			OnPropertyChanged("IsConnected");
			OnPropertyChanged("Status");
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

		void HandleStatusChanged(object sender, GaragesStatusEventArgs e)
		{
			Garages = e.Garages;
		}
	}
}