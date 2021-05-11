using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GitHub;

namespace ProvinceHelper.Updater {
	public partial class UpdateWindow {
		private StringBuilder builder = new StringBuilder();

		public UpdateWindow( string downloadLocation ) {
			var updateHandler = new UpdateHandler();

			InitializeComponent();

			Task.Run( () => {
				try {
					updateHandler.InstallUpdate( WriteToAction, ChangeProgress, downloadLocation );
				} catch ( Exception e ) {
					WriteToAction( $"\n\nSomething happened while updating:\nException:\t{e.GetType().FullName}\nMessage:\t{e.Message}\nStacktrace:\n{e.StackTrace}" );
				} finally {
					updateHandler.Dispose();
				}
				
				var result = MessageBox.Show(
					"Would you like to start Province Helper?",
					"Resume?",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question );

				if ( result == MessageBoxResult.No ) {
					Application.Current.Shutdown();

					return;
				}

				var exePath = Path.Combine( downloadLocation, "ProvinceHelper.exe" );

				var resumeProc = new Process() {
					StartInfo = {
						FileName = exePath
					}
				};

				resumeProc.Start();

				Application.Current.Shutdown();
			} );
		}

		private void WriteToAction( string add ) {
			Dispatcher.Invoke( () => {
				builder = builder.AppendLine( add );

				ActionBox.Text = builder.ToString();
			} );
		}

		private void ChangeProgress( float progress ) {
			Dispatcher.Invoke( () => ProgressBar.Value = progress );
		}
	}
}
