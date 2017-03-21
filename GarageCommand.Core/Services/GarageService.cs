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
				Console.WriteLine($"Retrieving IsReachable [{flags}]");
				var hasFlag = flags.HasFlag(NetworkReachabilityFlags.Reachable) | flags.HasFlag(NetworkReachabilityFlags.IsDirect);
				Console.WriteLine($"Is Reachable? {hasFlag}");
				return hasFlag;
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
				Console.WriteLine($"Creating client for [{ipAddress}]");
				_connectingTimer.Start();
				_client = await MqttClient.CreateAsync(ipAddress, _config);
				_client.Disconnected += HandleDisconnected;

				Console.WriteLine($"Connecting client to [{ipAddress}]");
				await _client.ConnectAsync(new MqttClientCredentials("yay"), null, true);
				_connectingTimer.Stop();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Connecting failed due to [{ex.Message}]");
				_connectingTimer.Stop();
				ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected));
			}

			if (_client.IsConnected)
			{
				await TryToSubscribe("/garage/status");
				await TryToSubscribe($"/garage/response/{_deviceId}");

				var statusObserver = Observer.Create<MqttApplicationMessage>((message) =>
				{
					var payload = Encoding.ASCII.GetString(message.Payload);
					var garages = JsonConvert.DeserializeObject<Garages>(payload);
					Console.WriteLine($"Topic [{message.Topic}] Payload [{garages}]");
					GaragesChanged?.Invoke(this, new GaragesChangedEventArgs(garages));
				}, (exception) =>
				{
					Console.WriteLine($"Exception: {exception.Message}");
				});
				_client.MessageStream.SubscribeSafe(statusObserver);

				var requestMessage = new MqttApplicationMessage($"/garage/request/{_deviceId}", new byte[] { });
				Console.WriteLine($"Publishing message to [/garage/request/{_deviceId}]");
				await _client.PublishAsync(requestMessage, MqttQualityOfService.AtMostOnce);
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
					ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected));
				}
			});
		}

		public bool IsReachable => _reachabilityManager?.IsReachable ?? false;

		public bool IsConnected => _client?.IsConnected ?? false;

		public async Task Toggle(string side)
		{
			if (IsConnected)
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
			Console.WriteLine($"Disposing {GetType().Name}");
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

		async Task TryToSubscribe(string topic)
		{
			var isSubscribed = false;
			var retries = 3;
			while (!isSubscribed && retries != 0)
			{
				try
				{
					Console.WriteLine($"Subscribing client to [{topic}]");
					await _client.SubscribeAsync(topic, MqttQualityOfService.AtMostOnce);
					isSubscribed = true;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Subscribing failed due to [{ex.Message}]");
					isSubscribed = false;
				}
				retries--;
			}
		}

		void HandleDisconnected(object sender, MqttEndpointDisconnected e)
		{
			Console.WriteLine($"Handling Disconnected Event because of {e.Message}");
			ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected));
		}

		void HandleReachabilityChanged(NetworkReachabilityFlags flags)
		{
			Console.WriteLine($"Reachability Changed Event with {flags}");

			if (flags.HasFlag(NetworkReachabilityFlags.Reachable) && !IsConnected)
			{
				Task.Factory.StartNew(async () => await Connect());
			}
			else if (!flags.HasFlag(NetworkReachabilityFlags.Reachable) && IsConnected)
			{
				Disconnect();
			}
			else
			{
				ConnectionChanged?.Invoke(this, new ConnectionStatusEventArgs(IsConnected));
			}
		}

		void HandledTimerElapsed(object sender, ElapsedEventArgs e)
		{
			Console.WriteLine($"Handling Timer Elapsed event");
			_connectingTimer.Stop();
			if (IsReachable && !IsConnected)
			{
				Disconnect();
				if (_client != null)
				{
					_client.Disconnected -= HandleDisconnected;
				}
				Task.Factory.StartNew(async () => await Connect());
			}
		}
	}
}