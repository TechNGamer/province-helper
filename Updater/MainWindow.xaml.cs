using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GitHub;
using Path = System.IO.Path;

namespace Updater {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow {
		private StringBuilder builder = new StringBuilder();

		public MainWindow( string downloadLocation ) {
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
					"Resume",
					MessageBoxButton.YesNo,
					MessageBoxImage.Question );

				if ( result == MessageBoxResult.No ) {
					Dispatcher.Invoke( Application.Current.Shutdown );

					return;
				}

				var exe = Path.Combine( downloadLocation, "ProvinceHelper.exe" );

				var resumeProc = new Process() {
					StartInfo = {
						FileName = exe,
						UseShellExecute = true
					}
				};

				resumeProc.Start();

				Dispatcher.Invoke( Application.Current.Shutdown );
			} );

			void ChangeProgress( float progress ) {
				Dispatcher.Invoke( () => ProgressBar.Value = progress );
			}

			void WriteToAction( string add ) {
				Dispatcher.Invoke( () => {
					builder = builder.AppendLine( add );

					StatusOutput.Text = builder.ToString();
				} );
			}
		}
	}
}
