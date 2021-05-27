using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using GitHub;

namespace ProvinceHelper {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		private Task updateTask;

		protected override void OnStartup( StartupEventArgs e ) {
			updateTask = Task.Run( Updates );

			base.OnStartup( e );
		}

		protected override void OnExit( ExitEventArgs e ) {
			updateTask.Wait();

			base.OnExit( e );
		}

		private void Updates() {
			using var updateHandler = new UpdateHandler();

			if ( !updateHandler.UpdateAvailable() ) {
				return;
			}

			if ( !DotNetExist() ) {
				MessageBox.Show(
					"There is an update available, but because the runtime is not installed, you have to manually install the update.",
					"Update Available",
					MessageBoxButton.OK,
					MessageBoxImage.Information );

				return;
			}

			var result = MessageBox.Show(
				"There is an update available, would you like to install it now?",
				"Update Available",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question );

			if ( result == MessageBoxResult.No ) {
				return;
			}

			Dispatcher.Invoke( () => MainWindow?.Close() );

			var tmpPath = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString( "N" ) );
			var myDir   = Path.GetDirectoryName( Process.GetCurrentProcess().MainModule.FileName );

			if ( !Directory.Exists( tmpPath ) ) {
				_ = Directory.CreateDirectory( tmpPath );
			}

			File.Copy(
				Path.Combine(
					myDir,
					"Updater.exe"
				),
				Path.Combine( tmpPath, "Updater.exe" ),
				true );
			//File.Copy( Path.Combine( myDir, "GitHub.dll" ), Path.Combine( tmpPath, "GitHub.dll" ) );

			var updateProc = new Process() {
				StartInfo = {
					FileName         = Path.Combine( tmpPath, "Updater.exe" ),
					WorkingDirectory = tmpPath,
					Arguments        = $"\"{myDir}\"",
				}
			};

			try {
				updateProc.Start();
				updateProc.WaitForExit();
			} catch ( Exception e ) {
				Debug.WriteLine( e );
				throw;
			}

			Shutdown( 1 );
		}

		private static bool DotNetExist() {
			var dotNetPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFiles ), "dotnet", "dotnet.exe" );

			// This checks to see if it is in Program Files.
			if ( File.Exists( dotNetPath ) ) {
				return true;
			}

			/* Checks to see if the OS is x86 or not. Since `Program Files` on x86 is `Program Files (x86)` on x64 Windows.
			 * It also means that there is no x64 program folder, so `Program Files` is the only program folder. */
			if ( RuntimeInformation.OSArchitecture == Architecture.X86 ) {
				return false;
			}

			// Grabs the x86 folder on x64 Windows.
			dotNetPath = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.ProgramFilesX86 ), "dotnet", "dotnet.exe" );

			// Returns if that file exists or not.
			return File.Exists( dotNetPath );
		}
	}
}
