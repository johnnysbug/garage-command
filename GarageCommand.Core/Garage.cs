﻿namespace GarageCommand.Core
{
	public class Garages
	{
		public Garage LeftGarage { get; set; }
		public Garage RightGarage { get; set; }

		public override bool Equals(object obj)
		{
			var garages = obj as Garages;

			if (garages == null)
				return false;

			return LeftGarage.Status == garages.LeftGarage.Status && RightGarage.Status == garages.RightGarage.Status;
		}

		public override int GetHashCode()
		{
			var left = LeftGarage == null ? 0 : LeftGarage.GetHashCode();
			var right = RightGarage == null ? 0 : RightGarage.GetHashCode();
			return left ^ right;
		}

		public override string ToString()
		{
			return string.Format("[LeftGarage={0}, RightGarage={1}]", LeftGarage.Status, RightGarage.Status);
		}
	}

	public class Garage
	{
		public GarageStatus Status { get; set; }
	}

	public enum GarageStatus
	{
		Closed = 0,
		Open = 1,
		Unknown
	}
}