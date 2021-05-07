using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Ookii.Dialogs.Wpf;
using ProvinceHelper.Utilities;
using Path = System.IO.Path;

namespace ProvinceHelper {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow {
		private static readonly byte[] MAGIC_BYTES = new byte[] {0xe6, 0x21};

		private string file0Content;
		private string file1Content;

		private Logging                  log = Logging.Singleton;
		private VistaFolderBrowserDialog dirDialog;
		private SaveFileDialog           fDialog;
		private OpenFileDialog           oDialog;

		public MainWindow() {
			var eu4Loc = FindGameLocation();

			dirDialog = new VistaFolderBrowserDialog {
				RootFolder          = Environment.SpecialFolder.Desktop,
				ShowNewFolderButton = false
			};

			fDialog = new SaveFileDialog {
				Title            = "Save File",
				AddExtension     = true,
				DefaultExt       = "savespace",
				CreatePrompt     = true,
				InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.Desktop ),
				OverwritePrompt  = true,
				CheckPathExists  = true,
				DereferenceLinks = true
			};

			oDialog = new OpenFileDialog {
				Multiselect      = false,
				AddExtension     = false,
				InitialDirectory = Environment.GetFolderPath( Environment.SpecialFolder.Desktop ),
				CheckFileExists  = true,
				CheckPathExists  = true,
			};

			InitializeComponent();

			EuivLocationBox.Text = string.IsNullOrWhiteSpace( eu4Loc ) ? string.Empty : eu4Loc;
		}

		private string FindGameLocation() {
			var splitChar        = new[] {' ', '\t'};
			var steamPath        = string.Empty;
			var libraryLocations = new List<string>();

			try {
				using ( var regKey = Registry.LocalMachine.OpenSubKey( "SOFTWARE\\WOW6432Node\\Valve\\Steam" ) ) {
					if ( regKey == null ) {
						log.Warning( "Could not find Steam Registry Key." );

						return null;
					}

					steamPath = regKey.GetValue( "InstallPath" ) as string;
				}
			} catch ( Exception e ) {
				log.Error( "Something happened while trying to find Steam.", e );

				MessageBox.Show( $"Critically failed to access Steam location.\n{e.GetType().FullName}\n{e.Message}", "Failed to get Steam location.", MessageBoxButton.OK, MessageBoxImage.Error );
			}

			if ( string.IsNullOrWhiteSpace( steamPath ) ) {
				return null;
			}

			libraryLocations.Add( Path.Combine( steamPath, "steamapps", "common" ) );

			var libVdf = Path.Combine( steamPath, "steamapps", "libraryfolders.vdf" );

			// Open's the library file that Steam creates.
			using ( var fStream = new FileStream( libVdf, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ) ) {
				using ( var fReader = new StreamReader( fStream ) ) {
					_ = fReader.ReadLine();
					_ = fReader.ReadLine();

					do {
						var line = fReader.ReadLine()?.Trim().Split( splitChar, StringSplitOptions.RemoveEmptyEntries );

						if ( line == null ) {
							break;
						}

						if ( line[0].StartsWith( "}" ) ) {
							break;
						}

						if ( !int.TryParse( line[0].Replace( "\"", "" ), out _ ) ) {
							continue;
						}

						libraryLocations.Add(
							Path.Combine(
								line[1].Replace( "\"", "" ).Replace( "\\\\", "\\" ),
								"steamapps",
								"common"
							)
						);
					} while ( true );
				}
			}

			log.Normal( "Looking for the game's directory." );

			foreach ( var libraryLocation in libraryLocations ) {
				foreach ( var gameDir in new DirectoryInfo( libraryLocation ).EnumerateDirectories( "*", SearchOption.TopDirectoryOnly ) ) {
					if ( gameDir.Name == "Europa Universalis IV" ) {
						return gameDir.FullName;
					}
				}
			}

			return null;
		}

		private void StartCopy( object sender, RoutedEventArgs e ) {
			var splitArr      = new char[] {' ', '-'};
			var userNumbers   = new Queue<int>( GetUserInputNumbers() );
			var provinceFiles = new List<FileInfo>();
			var totalNums     = userNumbers.Count;
			var copied        = 0;
			var destination   = DestBox.Text;

			InitProg.IsIndeterminate = true;
			InitProg.Minimum         = 0;
			InitProg.Maximum         = 1;

			DestButton.IsEnabled       = false;
			InitButton.IsEnabled       = false;
			EuivLocateButton.IsEnabled = false;

			provinceFiles.AddRange( new DirectoryInfo( Path.Combine( EuivLocationBox.Text, "history", "provinces" ) ).GetFiles() );

			InitProg.IsIndeterminate = false;

			Task.Run( () => {
				{
					try {
						BeginCopyProcess();
					} catch ( Exception e ) {
						log.Error( e.Message, e );
					}

					Dispatcher.Invoke( () => {
						DestButton.IsEnabled       = true;
						InitButton.IsEnabled       = true;
						EuivLocateButton.IsEnabled = true;

						MessageBox.Show( "Copying is done.", "Copy Done", MessageBoxButton.OK, MessageBoxImage.Information );
					} );
				}
			} );

			void BeginCopyProcess() {
				while ( userNumbers.TryDequeue( out var provinceNumber ) ) {
					log.Normal( $"Looking for the province with index `{provinceNumber}`." );

					for ( var i = 0; i < provinceFiles.Count; ++i ) {
						var provinceFile = provinceFiles[i];
						var numberStr    = provinceFile.Name.Split( splitArr, StringSplitOptions.RemoveEmptyEntries )[0];

						if ( !int.TryParse( numberStr, out var num ) ) {
							log.Warning( $"Failed to parse province file index. File: `{provinceFile.FullName}`" );

							provinceFiles.RemoveAt( i-- );

							continue;
						}

						if ( num != provinceNumber ) {
							continue;
						}

						log.Normal( "Found the correct province index." );

						File.Copy( provinceFile.FullName, Path.Combine( destination, provinceFile.Name ), true );

						provinceFiles.RemoveAt( i );

						Dispatcher.Invoke( () => InitProg.Value = ( double ) ++copied / totalNums );

						break;
					}
				}
			}
		}

		private void LocationSelector( object sender, RoutedEventArgs e ) {
			var result = dirDialog.ShowDialog();

			if ( result == null || !( ( bool ) result ) ) {
				return;
			}

			if ( Equals( sender, DestButton ) ) {
				DestBox.Text = dirDialog.SelectedPath;
			} else if ( Equals( sender, EuivLocateButton ) ) {
				EuivLocationBox.Text = dirDialog.SelectedPath;
			} else {
				MessageBox.Show(
					"Something happened while trying to complete an action. Send the log file to the developer please.",
					"Something happened.",
					MessageBoxButton.OK,
					MessageBoxImage.Error
				);
			}
		}

		private void OpenLog( object sender, RoutedEventArgs e ) => new Process {
			StartInfo = {
				FileName        = log.LogFile,
				UseShellExecute = true
			}
		}.Start();

		private void SaveToBinFile( object sender, RoutedEventArgs e ) {
			GetSaveLocation( out var filePath, out var includeDest );

			if ( string.IsNullOrEmpty( filePath ) ) {
				return;
			}

			using var fStream   = new FileStream( filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read );
			using var binWriter = new BinaryWriter( fStream, Encoding.Unicode );

			binWriter.Write( MAGIC_BYTES );

			// If this bit is set, it will mean that the destination path will be at the end of the file.
			binWriter.Write( includeDest );
			binWriter.Write( ProvinceList.Text.Replace( "\n", " " ) );

			if ( includeDest ) {
				binWriter.Write( DestBox.Text );
			}

			binWriter.Flush();
		}

		private void SaveToTextFile( object sender, RoutedEventArgs e ) {
			GetSaveLocation( out var filePath, out var includeDest );

			if ( string.IsNullOrEmpty( filePath ) ) {
				return;
			}

			using var fStream = new FileStream( filePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read );
			using var sWriter = new StreamWriter( fStream, Encoding.Unicode );

			fStream.SetLength( 0 );

			sWriter.Write( "Numbers=" );
			sWriter.WriteLine( ProvinceList.Text.Replace( "\n", " " ) );

			if ( includeDest ) {
				sWriter.Write( "Destination=" );
				sWriter.Write( DestBox.Text );
			}

			sWriter.Flush();
		}

		private void GetSaveLocation( out string filePath, out bool includeDest ) {
			fDialog.Filter     = "Savespace (*.savespace)|*.savespace";
			fDialog.DefaultExt = "savespace";
			fDialog.Title      = "Choose Save Location";

			var action = fDialog.ShowDialog( this );

			if ( !( bool ) action ) {
				filePath    = null;
				includeDest = false;

				return;
			}

			filePath = fDialog.FileName;

			var result = MessageBox.Show( "Would you like to include the destination path within the file?", "Add destination?", MessageBoxButton.YesNo, MessageBoxImage.Question );

			includeDest = result == MessageBoxResult.Yes;
		}

		private void LoadFromFile( object sender, RoutedEventArgs e ) {
			var splitChars = new[] {'='};

			oDialog.Title      = "Open Savespace";
			oDialog.DefaultExt = "savespace";
			oDialog.Filter     = "Savespace (*.savespace)|*.savespace";

			var result = oDialog.ShowDialog( this );

			if ( !( bool ) result ) {
				return;
			}

			var magicBuffer = new byte[2];

			using var fStream = new FileStream( oDialog.FileName, FileMode.Open, FileAccess.Read, FileShare.Read );

			fStream.Read( magicBuffer );

			if ( magicBuffer[0] != MAGIC_BYTES[0] && magicBuffer[1] != MAGIC_BYTES[1] ) {
				using var reader = new StreamReader( fStream );

				fStream.Position = 0;

				while ( !reader.EndOfStream ) {
					var line = reader.ReadLine();

					if ( string.IsNullOrWhiteSpace( line ) ) {
						continue;
					}

					var pairs = line.Split( splitChars, StringSplitOptions.RemoveEmptyEntries );

					if ( pairs[0] == "Numbers" ) {
						ProvinceList.Text = pairs[1];
					} else if ( pairs[0] == "Destination" ) {
						DestBox.Text = pairs[1];
					}
				}
			} else {
				using var binReader   = new BinaryReader( fStream, Encoding.Unicode );
				var       includeDest = binReader.ReadBoolean();

				log.Normal( "Loading pre-parsed data from save file into text box." );

				ProvinceList.Text = binReader.ReadString();

				if ( includeDest ) {
					DestBox.Text = binReader.ReadString();
				}
			}
		}

		private void FileSelector( object sender, RoutedEventArgs e ) {
			oDialog.Title  = "Choose a file";
			oDialog.Filter = string.Empty;

			var result = oDialog.ShowDialog( this );

			if ( !( bool ) result ) {
				return;
			}

			// var lines = File.ReadAllLines( oDialog.FileName );
			//
			// if ( lines.Length > 7500 ) {
			// 	const string message = "File too big. Please use a file with less than 1000 lines.";
			//
			// 	log.Warning( message );
			// 	MessageBox.Show( message, "File too big.", MessageBoxButton.OK, MessageBoxImage.Exclamation );
			//
			// 	return;
			// }

			if ( Equals( sender, CompareFile0 ) ) {
				PutDataIntoTextBox( CompareFile0Content, CompareFile0Name, oDialog.FileName );
			} else if ( Equals( sender, CompareFile1 ) ) {
				PutDataIntoTextBox( CompareFile1Content, CompareFile1Name, oDialog.FileName );
			} else {
				log.Error( $"Unknown sender `{sender.GetType().FullName}`." );
			}
		}

		private void TextBoxDrop( object sender, DragEventArgs e ) {
			if ( !e.Data.GetDataPresent( DataFormats.FileDrop ) ) {
				return;
			}
		
			var file = ( ( string[] ) e.Data.GetData( ( DataFormats.FileDrop ) ) )?[0];
		
			if ( Equals( sender, CompareFile0Content ) ) {
				PutDataIntoTextBox( CompareFile0Content, CompareFile0Name, file );
			} else if ( Equals( sender, CompareFile1Content ) ) {
				PutDataIntoTextBox( CompareFile1Content, CompareFile1Name, file );
			} else {
				log.Error( "Failed to know where file got dropped." );
			}
		}

		private static void PutDataIntoTextBox( TextBox compareBox, TextBox fileBox, string file ) {
			var lines = File.ReadAllText( file );
		
		
			fileBox.Text    = file;
			compareBox.Text = lines;
		}

		private void TextBoxPreview( object sender, DragEventArgs e ) {
			e.Handled = true;
		}

		private IEnumerable<int> GetUserInputNumbers( string[] values = null ) {
			values ??= ProvinceList.Text.Split( new[] {' '}, StringSplitOptions.RemoveEmptyEntries );

			var intValues = new List<int>();

			foreach ( var value in values ) {
				if ( value.Contains( '-' ) ) {
					var ends = value.Split( new[] {'-'}, StringSplitOptions.RemoveEmptyEntries );

					if ( int.TryParse( ends[0], out var start ) && int.TryParse( ends[1], out var end ) ) {
						for ( var i = start; i <= end; ++i ) {
							intValues.Add( i );
						}
					} else {
						log.Error( "Attempted to parse range operation. Expected the value to be `num-num`.", null );
					}
				} else if ( int.TryParse( value, out var intVal ) ) {
					intValues.Add( intVal );
				} else {
					log.Error( $"The value `{value}` is not a valid integer." );
				}
			}

			return intValues;
		}
		
		

		private void ShowSingleCompare( object sender, RoutedEventArgs e ) {
			var splitValues = new char[] {'\\', '/'};
			var file0Name   = CompareFile0Name.Text;
			var file1Name   = CompareFile1Name.Text;
		
			file0Name = file0Name[( file0Name.LastIndexOfAny( splitValues ) + 1 )..];
			file1Name = file1Name[( file1Name.LastIndexOfAny( splitValues ) + 1 )..];
		
			var singleCompareWindow = new SingleCompare( CompareFile0Content.Text, file0Name, CompareFile1Content.Text, file1Name ) {
				Owner = this
			};
		
			singleCompareWindow.ShowDialog();
		}
		
		
	}
}
