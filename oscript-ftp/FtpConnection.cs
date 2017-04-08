using System;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.HostedScript.Library.Http;
using ScriptEngine.HostedScript.Library;
using System.Net;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace oscriptFtp
{
	[ContextClass("FTPСоединение")]
	public sealed class FtpConnection : AutoContext<FtpConnection>
	{
		public FtpConnection(
			string server,
			int port = 0,
			string userName = null,
			string password = null,
			InternetProxyContext proxy = null,
			bool passiveConnection = false,
			int timeout = 0,
			IValue secureConnection = null)
		{
			Server = server;
			Port = port;
			User = userName;
			Password = password;
			Proxy = proxy;
			PassiveMode = passiveConnection;
			Timeout = timeout;
			SecureConnection = secureConnection;
		}

		[ContextProperty("ЗащищенноеСоединение")]
		public IValue SecureConnection { get; }

		[ContextProperty("Пароль")]
		public string Password { get; }

		[ContextProperty("ПассивныйРежим")]
		public bool PassiveMode { get; }

		[ContextProperty("Пользователь")]
		public string User { get; }

		[ContextProperty("Порт")]
		public int Port { get; }

		[ContextProperty("Прокси")]
		public InternetProxyContext Proxy { get; }

		[ContextProperty("Сервер")]
		public string Server { get; }

		[ContextProperty("Таймаут")]
		public int Timeout { get; }

		int GetPort()
		{
			if (Port != 0)
			{
				return Port;
			}
			return SecureConnection != null ? 990 : 21;
		}

		Uri GetUri(string path)
		{
			var builder = new UriBuilder("ftp", Server, GetPort(), path);
			return builder.Uri;
		}

		FtpWebRequest GetRequest(string path)
		{
			var request = (FtpWebRequest)WebRequest.Create(GetUri(path));
			if (!string.IsNullOrEmpty(User))
			{
				try
				{
					request.UseDefaultCredentials = false;
				}
				catch (NotImplementedException)
				{
				}
				request.Credentials = new NetworkCredential(User, Password);
			}

			request.UsePassive = PassiveMode;
			request.UseBinary = true;
			request.Timeout = Timeout == 0 ? -1 : Timeout * 1000;

			return request;
		}

		void ListFiles(string path, out IList<string> directories, out IList<string> files)
		{
			directories = new List<string>();
			files = new List<string>();

			// дикий костыль: сначала берём список имён
			// потом берём расширенный список и в нём ищем имена


			var unresolvedNames = new List<string>();
			{
				var request = GetRequest(path);
				request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;

				var response = (FtpWebResponse)request.GetResponse();
				var reader = new StreamReader(response.GetResponseStream());
				var line = reader.ReadLine();
				while (line != null)
				{
					if (!string.IsNullOrEmpty(line))
					{
						unresolvedNames.Add(line);
					}
					line = reader.ReadLine();
				}
			}

			var names = new List<string>();
			{
				var request = GetRequest(path);
				request.Method = WebRequestMethods.Ftp.ListDirectory;

				var response = (FtpWebResponse)request.GetResponse();
				var reader = new StreamReader(response.GetResponseStream());
				var line = reader.ReadLine();
				while (line != null)
				{
					if (!string.IsNullOrEmpty(line))
					{
						names.Add(line);
					}
					line = reader.ReadLine();
				}
			}

			var data = FtpCrutch.MatchLists(unresolvedNames, names);
			foreach (var el in data)
			{
				if (el.Value.StartsWith("d", StringComparison.Ordinal))
				{
					directories.Add(el.Key);
				}
				else
				if (el.Value.StartsWith("-", StringComparison.Ordinal))
				{
					files.Add(el.Key);
				}
			}
		}

		[ContextMethod("НайтиФайлы")]
		public ArrayImpl FindFiles(string path, string mask = null, bool recursive = true)
		{
			var result = ArrayImpl.Constructor() as ArrayImpl;

			if (!path.EndsWith("/", StringComparison.Ordinal))
			{
				path += "/";
			}

			IList<string> files, directories;

			try
			{
				ListFiles(path, out directories, out files);
			}
			catch (System.Net.ProtocolViolationException)
			{
				return result;
			}
			catch
			{
				throw;
			}

			foreach (var dirName in directories)
			{
				var dirEntry = new FtpFile(path, dirName, isDir: true);
				result.Add(dirEntry);
				if (recursive)
				{
					var filesInDir = FindFiles(dirEntry.FullName, mask, recursive);
					foreach (var fileEntry in filesInDir)
					{
						result.Add(fileEntry);
					}
				}
			}

			foreach (var fileName in files)
			{
				var fileEntry = new FtpFile(path, fileName);
				result.Add(fileEntry);
			}

			return result;
		}

		[ContextMethod("Записать")]
		public void Put(string localFilePath, string remoteFilePath)
		{
			var request = GetRequest(remoteFilePath);
			request.Method = WebRequestMethods.Ftp.UploadFile;

			using (var file = new FileStream(localFilePath, FileMode.Open))
			{
				file.CopyTo(request.GetRequestStream());
			}

			request.GetResponse();
		}

		[ContextMethod("Получить")]
		public void Get(string remoteFilePath, string localFilePath)
		{
			var request = GetRequest(remoteFilePath);
			request.Method = WebRequestMethods.Ftp.DownloadFile;

			var response = (FtpWebResponse)request.GetResponse();

			using (var file = new FileStream(localFilePath, FileMode.Create))
			{
				response.GetResponseStream().CopyTo(file);
			}

		}

		[ContextMethod("Удалить")]
		public void Delete(string path, string mask = null)
		{
			var request = GetRequest(path);
			request.Method = WebRequestMethods.Ftp.DeleteFile;

			request.GetResponse();
		}

		[ContextMethod("Переместить")]
		public void Move(string currentPath, string newPath)
		{
			var request = GetRequest(currentPath);
			request.Method = WebRequestMethods.Ftp.Rename;
			request.RenameTo = newPath;

			request.GetResponse();
		}

		[ContextMethod("ТекущийКаталог")]
		public string GetCurrentDirectory()
		{
			var request = GetRequest("");
			request.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;

			var response = (FtpWebResponse)request.GetResponse();
			var reader = new StreamReader(response.GetResponseStream());
			return reader.ReadToEnd();
		}

		public static IRuntimeContextInstance Constructor(
			string server,
			int port = 0,
			string userName = null,
			string password = null,
			InternetProxyContext proxy = null,
			bool? passiveConnection = null,
			int timeout = 0,
			IValue secureConnection = null
		)
		{
			var conn = new FtpConnection(server, port,
			                             userName, password, 
			                             proxy, passiveConnection ?? false,
			                             timeout, secureConnection);
			return conn;
		}
	}
}
