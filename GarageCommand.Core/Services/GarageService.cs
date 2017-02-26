using System;
using System.Net.Mqtt;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using Newtonsoft.Json;
using SystemConfiguration;
using CoreFoundation;
using System.Timers;

namespace GarageCommand.Core.Services
{
	public class ReachabilityManager : IDisposable
	{
		bool _isReachable;
		readonly NetworkReachability _reachability;

		public string IpAddress { get; private set; }
		public NetworkReachability.Notification Callback { get; private set; }

		public bool IsReachable
		{
			get
			{
				var flags = NetworkReachabilityFlags.Reachable;
				_reachability.TryGetFlags(out flags);
				return flags.HasFlag(NetworkReachabilityFlags.Reachable) | flags.HasFlag(NetworkReachabilityFlags.IsDirect);
			}
		}

		public ReachabilityManager(string ipAddress, NetworkReachability.Notification callback)
		{
			Callback = callback;
			IpAddress = ipAddress;

			var flags = NetworkReachabilityFlags.Reachable;
			_reachability = new NetworkReachability("192.168.1.150");
			_reachability.TryGetFlags(out flags);
			_reachability.SetNotification(Callback);
			_reachability.Schedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
		}

		public void Dispose()
		{
			if (_reachability != null)
			{
				_reachability.Unschedule(CFRunLoop.Current, CFRunLoop.ModeDefault);
				_reachability.Dispose();
			}
		}
	}

	public class GarageService : IDisposable
	{
		IMqttClient _client;
		readonly MqttConfiguration _config;
		readonly Timer _connectingTimer;
		readonly string _deviceId;
		readonly ReachabilityManager _reachabilityManager;

		public event EventHandler<GaragesChangedEventArgs> GaragesChanged;
		public event EventHandler<ConnectionStatusEventArgs> ConnectionChanged;

		public GarageService()
		{
			_reachabilityManager = new ReachabilityManager("192.168.1.150", HandleReachabilityChanged);
			_connectingTimer = new Timer(15000);
			_connectingTimer.Elapsed += HandledTimerElapsed;

			_deviceId = Guid.NewGuid().ToString();
			_config = new MqttConfiguration {
				MaximumQualityOfService = MqttQualityOfService.AtMostOnce,
				Port = 1883,
				KeepAliveSecs = 60
			};
		}

		public async Task Connect()
		{
			var ipAddress = "192.168.1.150";

			try
			{
				_connectingTimer.Start();
				_client = await MqttClient.CreateAsync(ipAddress, _config);
				_client.Disconnected += HandleDisconnected;
				Console.WriteLine($"Connecting to {ipAddress}");

				await _client.ConnectAsync(new MqttClientCredentials("yay"), null, true);
				_connectingTimer.Stop();
				await _client.SubscribeAsync("/garage/status", MqttQualityOfService.AtMostOnce);
				await _client.SubscribeAsync($"/garage/response/{_deviceId}", MqttQualityOfService.AtMostOnce);

				var statusObserver = Observer.Create<MqttApplicationMessage>((message) =>
				{
					var payload = Encoding.ASCII.GetString(message.Payload);
					var garages = JsonConvert.DeserializeObject<Garages>(payload);
					Console.WriteLine($"Received message on topic [{message.Topic}] with [{garages}]");
					GaragesChanged?.Invoke(this, new GaragesChangedEventArgs(garages));
				}, (exception) =>
				{
					Console.WriteLine($"Shit broke: {exception.Message}");
				});
				_client.MessageStream.SubscribeSafe(statusObserver);

				var requestMessage = new MqttApplicationMessage($"/garage/request/{_deviceId}", new byte[] { });
				await _client.PublishAsync(requestMessage, MqttQualityOfService.AtMostOnce);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Connecting failed due to [{ex.Message}]");
				_connectingTimer.Stop();
				ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected()));
			}

		}

		public void Disconnect()
		{
			Console.WriteLine($"Disconnecting");
			Task.Factory.StartNew(async () =>
			{
				if (_client != null)
				{
					try
					{
						ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(false));

						await _client.UnsubscribeAsync(
							"/garage/status", 
							$"/garage/response/{_deviceId}"
						);

						await _client.DisconnectAsync();
						Console.WriteLine($"Disconnected");
					}
					catch (Exception ex)
					{
						Console.WriteLine($"Disconnecting failed due to [{ex.Message}]");
					}
				}
				else
				{
					ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected()));
				}
			});
		}

		public bool IsConnected()
		{
			return (_reachabilityManager?.IsReachable ?? false) && (_client?.IsConnected ?? false);
		}

		public async Task Toggle(string side)
		{
			if (IsConnected())
			{
				Console.WriteLine($"Toggling {side} garage");
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

		public void Dispose()
		{
			if (_client != null)
			{
				_client.Disconnected -= HandleDisconnected;
				_client.Dispose();
			}
			if (_connectingTimer != null)
			{
				_connectingTimer.Elapsed -= HandledTimerElapsed;
				_connectingTimer.Dispose();
			}
			if (_reachabilityManager != null)
			{
				_reachabilityManager.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		void HandleDisconnected(object sender, MqttEndpointDisconnected e)
		{
			Console.WriteLine($"Handling Disconnected Event because of {e.Message}");
			ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected()));
		}

		void HandleReachabilityChanged(NetworkReachabilityFlags flags)
		{
			if (flags.HasFlag(NetworkReachabilityFlags.Reachable) && !IsConnected())
			{
				Task.Factory.StartNew(async () => await Connect());
			}
			else if (!flags.HasFlag(NetworkReachabilityFlags.Reachable) && IsConnected())
			{
				Disconnect();
			}
			else
			{
				ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected()));
			}
		}

		void HandledTimerElapsed(object sender, ElapsedEventArgs e)
		{
			_connectingTimer.Stop();
			if (!IsConnected())
			{
				Disconnect();
			}
		}
	}
}