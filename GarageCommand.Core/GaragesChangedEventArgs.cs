using System;

namespace GarageCommand.Core
{
	public class GaragesChangedEventArgs : EventArgs
	{
		public Garages Garages { get; private set; }

		public GaragesChangedEventArgs(Garages garages)
		{
			Garages = garages;
		}
	}
}