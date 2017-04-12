using System;
using ScriptEngine.Machine;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.HostedScript.Library.Http;
using ScriptEngine.HostedScript.Library;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace oscriptFtp
{
	/// <summary>
	/// Предназначен для работы с файлами посредством FTP.
	/// </summary>
	[ContextClass("FTPСоединение")]
	public sealed class FtpConnection : AutoContext<FtpConnection>
	{
		private string _currentDirectory = "/";

		/// <summary>
		/// Создаёт новый объект <see cref="T:oscriptFtp.FtpConnection">FTPСоединение</see>.
		/// </summary>
		/// <param name="server">Сервер.</param>
		/// <param name="port">Порт. Необязательный</param>
		/// <param name="userName">Имя пользователя. Необязательный</param>
		/// <param name="password">Пароль. Необязательный</param>
		/// <param name="proxy">Прокси. Необязательный</param>
		/// <param name="passiveConnection">Пассивный режим.</param>
		/// <param name="timeout">Таймаут.</param>
		/// <param name="secureConnection">Защищённое соединение.</param>
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

		/// <summary>
		/// Защищённое соединение.
		/// </summary>
		/// <value>Содержит объект защищённого соединения.</value>
		[ContextProperty("ЗащищенноеСоединение")]
		public IValue SecureConnection { get; }

		/// <summary>
		/// Пароль.
		/// </summary>
		/// <value>Пароль.</value>
		[ContextProperty("Пароль")]
		public string Password { get; }

		/// <summary>
		/// Пассивный режим.
		/// </summary>
		/// <value>Пассивный режим.</value>
		[ContextProperty("ПассивныйРежим")]
		public bool PassiveMode { get; }

		/// <summary>
		/// Имя пользователя.
		/// </summary>
		/// <value>Имя пользователя.</value>
		[ContextProperty("Пользователь")]
		public string User { get; }

		/// <summary>
		/// Порт.
		/// </summary>
		/// <value>Порт.</value>
		[ContextProperty("Порт")]
		public int Port { get; }

		/// <summary>
		/// Прокси.
		/// </summary>
		/// <value>Прокси.</value>
		[ContextProperty("Прокси")]
		public InternetProxyContext Proxy { get; }

		/// <summary>
		/// Сервер.
		/// </summary>
		/// <value>Сервер, с которым устанавливается соединение.</value>
		[ContextProperty("Сервер")]
		public string Server { get; }

		/// <summary>
		/// Таймаут.
		/// </summary>
		/// <value>Таймаут в секундах. 0 - без таймаута.</value>
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
			var uri = GetUri(path);
			var request = (FtpWebRequest)WebRequest.Create(uri);
			if (!string.IsNullOrEmpty(User))
			{
				try
				{
					request.UseDefaultCredentials = false;
				}
				catch (NotImplementedException)
				{
				}
				catch (NotSupportedException)
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

		/// <summary>
		/// Ищет файлы на удалённом сервере.
		/// </summary>
		/// <returns>Массив.</returns>
		/// <param name="path">Путь.</param>
		/// <param name="mask">Маска. Необязательный</param>
		/// <param name="recursive">Искать во вложенных каталогах.</param>
		[ContextMethod("НайтиФайлы")]
		public ArrayImpl FindFiles(string path, string mask = null, bool recursive = true)
		{
			var result = ArrayImpl.Constructor() as ArrayImpl;

			Regex maskChecker = null;
			if (!string.IsNullOrEmpty(mask))
			{
				maskChecker = FtpCrutch.GetRegexForFileMask(mask);
			}

			if (!string.IsNullOrEmpty(path) && !path.EndsWith("/", StringComparison.Ordinal))
			{
				path += "/";
			}
			path = UniteFtpPath(_currentDirectory, path);

			IList<string> files, directories;

			try
			{
				ListFiles(path, out directories, out files);
			}
			catch (System.Net.ProtocolViolationException)
			{
				return result;
			}
			catch (WebException ex)
			{
				var error = (FtpWebResponse)ex.Response;
				if (error.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
				{
					return result;
				}
				throw ex;
			}
			catch
			{
				throw;
			}

			foreach (var dirName in directories)
			{
				var dirEntry = new FtpFile(path, dirName, isDir: true);
				if (maskChecker?.IsMatch(dirName) ?? true)
				{
					result.Add(dirEntry);
				}
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
				if (maskChecker?.IsMatch(fileName) ?? true)
				{
					var fileEntry = new FtpFile(path, fileName);
					result.Add(fileEntry);
				}
			}

			return result;
		}

		/// <summary>
		/// Записывает локальный файл на удалённый сервер.
		/// </summary>
		/// <param name="localFilePath">Путь к файлу на локальном компьютере.</param>
		/// <param name="remoteFilePath">Путь к файлу на удалённом сервере.</param>
		[ContextMethod("Записать")]
		public void Put(string localFilePath, string remoteFilePath)
		{
			var request = GetRequest(remoteFilePath);
			request.Method = WebRequestMethods.Ftp.UploadFile;

			var requestStream = request.GetRequestStream();

			using (var file = new FileStream(localFilePath, FileMode.Open))
			{
				file.CopyTo(requestStream);
			}
			requestStream.Close();

			request.GetResponse();
		}

		/// <summary>
		/// Скачивает файл с удалённого сервера.
		/// </summary>
		/// <param name="remoteFilePath">Путь к файлу ан удалённом сервере.</param>
		/// <param name="localFilePath">Путь к файлу, в который будет записан удалённый файл.</param>
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

		/// <summary>
		/// Удаляет файлы на сервере.
		/// </summary>
		/// <param name="path">Путь.</param>
		/// <param name="mask">Маска. Необязательный</param>
		[ContextMethod("Удалить")]
		public void Delete(string path, string mask = null)
		{
			if (!string.IsNullOrEmpty(mask))
			{
				var filesToDelete = FindFiles(path, mask, recursive: false);
				foreach (var file in filesToDelete)
				{
					Delete((file as FtpFile).FullName);
				}
				return;
			}

			var request = GetRequest(path);
			request.Method = WebRequestMethods.Ftp.DeleteFile;

			request.GetResponse();
		}

		/// <summary>
		/// Перемещает файл на удалённом сервере.
		/// </summary>
		/// <param name="currentPath">Путь к перемещаемому файлу.</param>
		/// <param name="newPath">Новый Путь файла.</param>
		[ContextMethod("Переместить")]
		public void Move(string currentPath, string newPath)
		{
			var request = GetRequest(currentPath);
			request.Method = WebRequestMethods.Ftp.Rename;
			request.RenameTo = newPath;

			request.GetResponse();
		}

		/// <summary>
		/// Получает рабочий каталог на удалённом сервере.
		/// </summary>
		/// <returns>Рабочий каталог.</returns>
		public string GetWorkingDirectory()
		{
			var request = GetRequest("");
			request.Method = WebRequestMethods.Ftp.PrintWorkingDirectory;

			var response = (FtpWebResponse)request.GetResponse();
			var reader = new StreamReader(response.GetResponseStream());
			return reader.ReadToEnd();
		}

		private string UniteFtpPath(string a, string b)
		{
			if (b.StartsWith("/", StringComparison.Ordinal))
			{
				return b;
			}
			var uri = GetUri(string.Format("{0}{1}", a, b));
			return uri.LocalPath;
		}

		/// <summary>
		/// Возвращает текущий каталог соединения.
		/// </summary>
		/// <returns>Текущий каталог.</returns>
		[ContextMethod("ТекущийКаталог")]
		public string GetCurrentDirectory()
		{
			return _currentDirectory;
		}

		/// <summary>
		/// Устанавливает текущий каталог соединения.
		/// </summary>
		/// <param name="directory">Каталог.</param>
		[ContextMethod("УстановитьТекущийКаталог")]
		public void SetCurrentDirectory(string directory)
		{
			_currentDirectory = UniteFtpPath(_currentDirectory, directory);
			if (!_currentDirectory.EndsWith("/", StringComparison.Ordinal))
			{
				_currentDirectory += "/";
			}
		}

		/// <summary>
		/// Создаёт каталог на удалённом сервере.
		/// </summary>
		/// <param name="dirName">Имя каталога.</param>
		[ContextMethod("СоздатьКаталог")]
		public void CreateDirectory(string dirName)
		{
			var request = GetRequest(dirName);
			request.Method = WebRequestMethods.Ftp.MakeDirectory;
			var response = (FtpWebResponse)request.GetResponse();

		}

		/// <summary>
		/// Создаёт объект FTPСоединение.
		/// </summary>
		/// <returns>FTPСоединение.</returns>
		/// <param name="server">Подключаемый сервер.</param>
		/// <param name="port">Порт. Необязательный</param>
		/// <param name="userName">Имя пользователя. Необязательный</param>
		/// <param name="password">Пароль. Необязательный</param>
		/// <param name="proxy">Прокси. Необязательный</param>
		/// <param name="passiveConnection">Пассивный режим. Необязательный</param>
		/// <param name="timeout">Таймаут. Необязательный</param>
		/// <param name="secureConnection">Защищённое соединение. Необязательный</param>
		[ScriptConstructor]
		public static IRuntimeContextInstance Constructor(
			IValue server,
			IValue port = null,
			IValue userName = null,
			IValue password = null,
			InternetProxyContext proxy = null,
			IValue passiveConnection = null,
			IValue timeout = null,
			IValue secureConnection = null
		)
		{
			var conn = new FtpConnection(server.AsString(),
			                             (int)(port?.AsNumber() ?? 21),
			                             userName?.AsString(), password?.AsString(), 
			                             proxy, passiveConnection?.AsBoolean() ?? false,
			                             (int)(timeout?.AsNumber() ?? 0), secureConnection);
			return conn;
		}
	}
}
