using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace ProvinceHelper.Utilities {
	public class Logging {
		private enum ErrorLevel : byte {
			Normal  = 0,
			Warning = 1,
			Error   = 2
		}

		private readonly struct LogMessage {
			public readonly string     message;
			public readonly ErrorLevel level;
			public readonly DateTime   time;
			public readonly Exception  exception;

			public LogMessage( string message, ErrorLevel level, DateTime time, Exception e = null ) {
				this.message   = message;
				this.level     = level;
				this.time      = time;
				this.exception = e;
			}
		}

		public static Logging Singleton {
			get {
				if ( _singleton == null ) {
					_singleton = new Logging();
				}

				return _singleton;
			}
		}

		private static Logging _singleton;

		private FileStream                  logFileStream;
		private StreamWriter                logWriter;
		private ManualResetEvent            newLogEvent = new ManualResetEvent( false );
		private ConcurrentQueue<LogMessage> logQueue    = new ConcurrentQueue<LogMessage>();
		private CancellationTokenSource     tokenSource = new CancellationTokenSource();

		public string LogFile { get; }

		private Logging() {
			LogFile = Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".pc", DateTime.Now.ToString( "yyyy-M-d_HH-MM-ss" ) + ".log" );

			var progDataDir = new FileInfo( LogFile ).Directory;

			if ( progDataDir != null && !progDataDir.Exists ) {
				progDataDir.Create();
			}

			logFileStream = new FileStream( LogFile, FileMode.Create, FileAccess.Write, FileShare.Read );
			logWriter     = new StreamWriter( logFileStream, Encoding.Unicode );

			var logTask = new Task( () => WriteLogLoop( tokenSource.Token ), CancellationToken.None, TaskCreationOptions.LongRunning );

			logTask.Start();

			Application.Current.Exit += ( _, args ) => {
				tokenSource.Cancel( false );

				try {
					logTask.Wait();
				} catch ( TaskCanceledException ) {
				}
				
				logWriter.Flush();
				logWriter.Dispose();
				
				logFileStream.Flush();
				logFileStream.Dispose();
			};
		}

		public void Normal( string message ) => EnqueueLog( message, ErrorLevel.Normal, null );

		public void Warning( string message, Exception e = null ) => EnqueueLog( message, ErrorLevel.Warning, e );

		public void Error( string message, Exception e = null ) {
			if ( e == null && message == null ) {
				throw new ArgumentNullException( nameof( message ) + ' ' + nameof( e ), "Expected either exception is null or message to not be null." );
			}

			if ( string.IsNullOrWhiteSpace( message ) ) {
				message = e.Message;
			}

			EnqueueLog( message, ErrorLevel.Error, e );

			MessageBox.Show( $"An error occured, please send the log file to the programmer to notify them of this problem.", "An error occured.", MessageBoxButton.OK, MessageBoxImage.Error );
		}

		private void EnqueueLog( string message, ErrorLevel level, Exception e = null ) {
			var newLog = new LogMessage( message, level, DateTime.Now, e );

			logQueue.Enqueue( newLog );
			newLogEvent.Set();
		}

		private void WriteLogLoop( CancellationToken token ) {
			var handler = new WaitHandle[] {newLogEvent, token.WaitHandle};

			while ( !token.IsCancellationRequested ) {
				var action = WaitHandle.WaitAny( handler );

				switch ( action ) {
					case 0:
						newLogEvent.Reset();
						EmptyLogQueue();
						break;
					case 1:
						EmptyLogQueue();
						return;
					default:
						Singleton.Error( "Unknown action has occured.", new Exception( "Unknown action has occured within the logging method." ) );
						break;
				}
			}
		}

		private void EmptyLogQueue() {
			while ( logQueue.TryDequeue( out var log ) ) {
				var sBuilder    = new StringBuilder();
				var nlBeginning = BeginningNewLine( log );

				sBuilder.Append( nlBeginning ).Append( "Message:\t" ).AppendLine( log.message );

				if ( log.exception != null ) {
					var e = log.exception;

					do {
						sBuilder.Append( nlBeginning ).Append( "Exception:\t" ).AppendLine( e.GetType().FullName )
							.Append( nlBeginning ).AppendLine( "Stacktrace" ).AppendLine( e.StackTrace );

						if ( e.InnerException != null ) {
							sBuilder.Append( nlBeginning ).Append( '-', 10 ).Append( "INNER EXCEPTION" ).Append( '-', 10 ).AppendLine();
						}

						e = e.InnerException;
					} while ( e != null );
				}

				lock ( logFileStream ) {
					lock ( logWriter ) {
						logWriter.Write( sBuilder.ToString() );
					}

					logWriter.Flush();
				}
			}

			string BeginningNewLine( LogMessage log ) {
				var sBuilder = new StringBuilder();

				if ( log.level == ErrorLevel.Normal ) {
					sBuilder.Append( "[NORM" );
				} else if ( log.level == ErrorLevel.Warning ) {
					sBuilder.Append( "[WARN" );
				} else {
					sBuilder.Append( "[ERROR" );
				}

				sBuilder.Append( '/' ).Append( log.time.ToString( "s" ) ).Append( "]:\t" );

				return sBuilder.ToString();
			}
		}
	}
}
