using System;

namespace GarageCommand.Core
{	
	public class ConnectionStatusEventArgs : EventArgs
	{
		public bool IsConnected { get; private set; }

		public ConnectionStatusEventArgs() : this(false) { }

		public ConnectionStatusEventArgs(bool isConnected)
		{
			IsConnected = isConnected;
		}
	}
}