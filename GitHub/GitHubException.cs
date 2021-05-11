using System;

namespace GitHub {
	public class GitHubException : Exception {

		internal GitHubException( Exception innerException ) : this( innerException.Message, innerException ) {
			
		}

		internal GitHubException( string message, Exception innerException ) : base( message, innerException ) {
			
		}
		
	}
}
