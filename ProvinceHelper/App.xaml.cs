using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using GitHub;
using ProvinceHelper.Updater;

namespace ProvinceHelper {
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App {
		private static string CopyToTemp() {
			var dir    = new FileInfo( Process.GetCurrentProcess().MainModule.FileName ).Directory;
			var tmpDir = Path.Combine( Path.GetTempPath(), Guid.NewGuid().ToString( "N" ) );

			Directory.CreateDirectory( tmpDir );

			CopyTo( dir, tmpDir );

			return tmpDir;

			static void CopyTo( DirectoryInfo original, string dest ) {
				foreach ( var fsInfo in original.EnumerateFileSystemInfos( "*", SearchOption.TopDirectoryOnly ) ) {
					if ( fsInfo is DirectoryInfo di ) {
						var sub = Directory.CreateDirectory( Path.Combine( dest, di.Name ) ).FullName;

						CopyTo( di, sub );
					} else if ( fsInfo is FileInfo fi ) {
						try {
							fi.CopyTo( Path.Combine( dest, fi.Name ), true );
						} catch ( Exception e ) {
							MessageBox.Show( $"Exception:\t{e.GetType().FullName}\nMessage:\t{e.Message}" );

							throw;
						}
					}
				}
			}
		}

		protected override void OnStartup( StartupEventArgs e ) {
			/* This check is here because I use XZ Tarballs for the self-contained builds.
			 * Programming for tarballs can be annoying, so for now I am opting out of having this program extract tarballs.
			 * I mainly use Linux as my OS of choice, so Tarballs feel natual to me. */
			if ( DotNetExist() ) {
				if ( e.Args[0] == "--update" ) {
					ResumeUpdate( e.Args );
				} else {
					Task.Run( Updates );
				}
			}

			base.OnStartup( e );
		}

		private void ResumeUpdate( IReadOnlyList<string> args ) {
			var updateLoc = args[1];

			MainWindow = new UpdateWindow( updateLoc );
		}

		private void Updates() {
			using var updateHandler = new UpdateHandler();

			if ( false ) {
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

			var tmpPath = CopyToTemp();

			var updateProc = new Process() {
				StartInfo = {
					FileName  = Path.Combine( tmpPath, "ProvinceHelper.exe" ),
					Arguments = $"--update \"{new FileInfo( Process.GetCurrentProcess().MainModule.FileName ).Directory.FullName}\""
				}
			};

			updateProc.Start();

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
