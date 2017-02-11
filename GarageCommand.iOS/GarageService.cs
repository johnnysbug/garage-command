using System;
using System.Net.Mqtt;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using Newtonsoft.Json;

namespace GarageCommand.iOS
{
	public class GarageService
	{
		IMqttClient _client;
		readonly MqttConfiguration _config;
		readonly string _deviceId;

		public event EventHandler<GaragesStatusEventArgs> StatusChanged = (sender, e) => { };

		public GarageService()
		{
			_deviceId = Guid.NewGuid().ToString();
			_config = new MqttConfiguration {
				MaximumQualityOfService = MqttQualityOfService.AtMostOnce,
				Port = 1883,
				KeepAliveSecs = 60
			};
		}

		public async Task Connect()
		{
			_client = await MqttClient.CreateAsync("192.168.1.150", _config);
			await _client.ConnectAsync(new MqttClientCredentials("yay"), null, true);
			await _client.SubscribeAsync("/garage/status", MqttQualityOfService.AtMostOnce);
			await _client.SubscribeAsync($"/garage/response/{_deviceId}", MqttQualityOfService.AtMostOnce);

			var statusObserver = Observer.Create<MqttApplicationMessage>((message) =>
			{
				var payload = Encoding.ASCII.GetString(message.Payload);
				var garages = JsonConvert.DeserializeObject<Garages>(payload);
				Console.WriteLine($"Received message on topic [{message.Topic}] with [{garages}]");
				StatusChanged(this, new GaragesStatusEventArgs { Garages = garages });
			}, (exception) =>
			{
				Console.WriteLine($"Shit broke: {exception.Message}");
			});
			_client.MessageStream.SubscribeSafe(statusObserver);

			var requestMessage = new MqttApplicationMessage($"/garage/request/{_deviceId}", new byte[] { });
			await _client.PublishAsync(requestMessage, MqttQualityOfService.AtMostOnce);
		}

		public bool IsConnected()
		{
			return _client != null && _client.IsConnected;
		}

		public async Task Toggle(string side)
		{
			if (IsConnected())
			{
				try
				{
					var bytes = Encoding.ASCII.GetBytes(side);
					var message = new MqttApplicationMessage("/garage/toggle", bytes);
					await _client.PublishAsync(message, MqttQualityOfService.AtMostOnce);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Unable to toggle [{side} garage] because [{ex.Message}]");
				}
			}
		}
	}

	public class GaragesStatusEventArgs : EventArgs
	{
		public Garages Garages { get; set; }
	}
}