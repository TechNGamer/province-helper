using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace GitHub {
	public class UpdateHandler : IDisposable {
		public const byte MAJOR = 1;
		public const byte MINOR = 0;
		public const byte PATCH = 0;

		public const string REPO_API = "https://api.github.com/repos/TechNGamer/province-helper/releases/latest";

		private HttpClient client;

		public UpdateHandler() {
			var handler = new HttpClientHandler {
				AllowAutoRedirect = true
			};

			client = new HttpClient( handler );

			client.DefaultRequestHeaders.Add( "User-Agent", "TechNGamer/Province_Helper" );
		}

		~UpdateHandler() {
			client?.Dispose();
		}

		public bool UpdateAvailable() => AsyncUpdateAvailable().GetAwaiter().GetResult();

		public async Task<bool> AsyncUpdateAvailable() {
			var release = await GetLatestRelease();

			// If release is null down here, that means something happened that caused it not to serialize.
			if ( release == default ) {
				throw new GitHubException( "A parsing error occured.", null );
			}

			var versionStr = release.TagName.Split( '.' );

			return int.Parse( versionStr[0] ) > MAJOR || int.Parse( versionStr[1] ) > MINOR || int.Parse( versionStr[2] ) > PATCH;
		}

		public async Task InstallUpdate( Action<string> output, Action<float> progress, string location ) {
			output?.Invoke( "Grabbing latest release." );
			
			var release = await GetLatestRelease();
			
			
		}

		public void Dispose() {
			client.Dispose();

			client = default;

			GC.SuppressFinalize( this );
		}

		private async Task<Release> GetLatestRelease() {
			const string HTTP_ERROR_MSG = "Network issue has occured while trying to see if updates are available.";
			const string JSON_ERROR_MSG = "There is a problem with the JSON.";
			Release      release;

			try {
				var json = await client.GetStringAsync( REPO_API );

				release = JsonSerializer.Deserialize<Release>( json );
			} catch ( HttpRequestException e ) {
				throw new GitHubException( HTTP_ERROR_MSG, e );
			} catch ( ArgumentNullException e ) {
				throw new GitHubException( JSON_ERROR_MSG, e );
			} catch ( JsonException e ) {
				throw new GitHubException( JSON_ERROR_MSG, e );
			} catch ( NotSupportedException e ) {
				throw new GitHubException( JSON_ERROR_MSG, e );
			}

			return release;
		}
	}
}
