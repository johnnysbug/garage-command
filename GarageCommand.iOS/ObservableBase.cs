using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System;

namespace GarageCommand.iOS
{
	/// <summary>
	/// A thin base class that provides a simple property change notification mechanism so
	/// that the object can easily participate in data binding.
	/// </summary>
	public abstract class ObservableBase : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
	{
		public bool Initialized { get; set; }

		/// <summary>
		/// Sets the value of the <paramref name="field"/> to the new value and raises the appropriate
		/// property change notifications.
		/// </summary>
		/// <typeparam name="T">The type of the field to be set.</typeparam>
		/// <param name="field">The field to set, passed by reference.</param>
		/// <param name="newValue">The new value to set on the field.</param>
		/// <param name="propertyName">The name of the property that will be communicated in the property change event.</param>
		/// <returns>True if the field was changed, false if the new value was the same as the old value.</returns>
		protected bool SetField<T>(ref T field, T newValue, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, newValue))
			{
				return false;
			}

			OnPropertyChanging(propertyName);

			field = newValue;

			OnPropertyChanged(propertyName);

			return true;
		}

		/// <summary>
		/// Raises the <see cref="PropertyChanging"/> event.
		/// </summary>
		/// <param name="propertyName">The name of the property that is changing.</param>
		protected virtual void OnPropertyChanging([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanging;
			if (handler != null)
			{
				handler(this, new PropertyChangingEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Raises the <see cref="PropertyChanged"/> event.
		/// </summary>
		/// <param name="propertyName">The name of the property that changed.</param>
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		protected virtual void Dispose(bool disposing) { }

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Event that is raised before the property is changed.
		/// </summary>
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>
		/// Event that is raised after a property is changed.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		~ObservableBase()
		{
			Dispose(false);
		}
	}
}