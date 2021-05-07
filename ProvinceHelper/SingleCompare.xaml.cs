using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using ProvinceHelper.Utilities;

namespace ProvinceHelper {
	public partial class SingleCompare {
		private Logging log = Logging.Singleton;
		
		public SingleCompare( string file0Content, string file0Name, string file1Content, string file1Name ) {
			var splitChars = new[] {'\n'};
			var f0Lines    = file0Content.Split( splitChars );
			var f1Lines    = file1Content.Split( splitChars );
			var difference = new List<int>();
			var strBuilder = new StringBuilder();
			
			log.Normal( $"Comparing lines between `{file0Name}` and `{file1Name}`." );

			for ( var i = 0; i < f0Lines.Length && i < f1Lines.Length; ++i ) {
				if ( f0Lines[i] == f1Lines[i] ) {
					continue;
				}

				difference.Add( i );
			}
			
			log.Normal( $"Initialing UI Components." );

			InitializeComponent();
			
			log.Normal( "Checking for differences in length." );

			if ( difference.Count == 0 && f0Lines.Length == f1Lines.Length ) {
				SingleView.Text = "There is no difference between the files.";

				return;
			}
			
			log.Normal( "Showing extra lines." );

			strBuilder.Append( "Differences in `" ).Append( file0Name ).Append( "` and `" ).Append( file1Name ).AppendLine( ":" );

			foreach ( var lineNum in difference ) {
				strBuilder.Append( "\tLine `" )
					.Append( lineNum )
					.AppendLine( "` is different." )
					.Append( "\t\t`" )
					.Append( file0Name )
					.Append( "`: " )
					.AppendLine( f0Lines[lineNum].Trim() )
					.Append( "\t\t`" )
					.Append( file1Name ).Append( "`: " )
					.AppendLine( f1Lines[lineNum].Trim() );
			}

			if ( f0Lines.Length < f1Lines.Length ) {
				strBuilder.Append( "Extra Lines from file `" ).Append( file1Name ).AppendLine( "`:" );

				GrabExtraLines( strBuilder, f1Lines, f0Lines.Length - 1 );
			} else if ( f1Lines.Length < f0Lines.Length ) {
				strBuilder.Append( "Extra Lines from file `" ).Append( file0Name ).AppendLine( "`:" );

				GrabExtraLines( strBuilder, f0Lines, f1Lines.Length - 1 );
			} else {
				strBuilder.Append( "There are no extra lines." );
			}

			SingleView.Text = strBuilder.ToString().Trim();
		}

		private static void GrabExtraLines( StringBuilder strBuilder, string[] bigger, int start ) {
			for ( var i = start; i < bigger.Length; ++i ) {
				strBuilder.Append('\t').AppendLine( bigger[i] );
			}
		}
	}
}
