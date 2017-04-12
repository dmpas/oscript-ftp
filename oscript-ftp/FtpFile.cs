using System;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.Machine;

namespace oscriptFtp
{
	/// <summary>
	/// Описывает файл на удалённом сервере.
	/// </summary>
	[ContextClass("FTPФайл")]
	public sealed class FtpFile : AutoContext<FtpFile>
	{
		private readonly bool _isDirectory;

		/// <summary>
		/// Создаёт описание файла.
		/// </summary>
		/// <param name="path">Путь к файлу.</param>
		/// <param name="filename">Имя файла.</param>
		/// <param name="isDir"><c>true</c> если это каталог.</param>
		public FtpFile(string path, string filename, bool isDir = false)
		{
			Path = path;
			if (!Path.EndsWith("/", StringComparison.Ordinal))
			{
				Path += "/";
			}
			Name = filename;
			FullName = string.Format("{0}{1}", Path, Name);
			Extension = System.IO.Path.GetExtension(Name);
			BaseName = System.IO.Path.GetFileNameWithoutExtension(Name);
			_isDirectory = isDir;
		}

		/// <summary>
		/// Имя файла.
		/// </summary>
		/// <value>Имя файла или каталога.</value>
		[ContextProperty("Имя")]
		public string Name { get; }

		/// <summary>
		/// Имя без расширения.
		/// </summary>
		/// <value>Имя файла или каталога без расширения.</value>
		[ContextProperty("ИмяБезРасширения")]
		public string BaseName { get; }

		/// <summary>
		/// Полное имя.
		/// </summary>
		/// <value>Полный путь к файлу.</value>
		[ContextProperty("ПолноеИмя")]
		public string FullName { get; }

		/// <summary>
		/// Путь.
		/// </summary>
		/// <value>Каталог, в котором находится файл.</value>
		[ContextProperty("Путь")]
		public string Path { get; }

		/// <summary>
		/// Расширение.
		/// </summary>
		/// <value>Расширение файла.</value>
		[ContextProperty("Расширение")]
		public string Extension { get; }

		/// <summary>
		/// Определяет, что путь указывает на файл.
		/// </summary>
		/// <returns><c>true</c>, если это файл, <c>false</c> - это каталог.</returns>
		[ContextMethod("ЭтоФайл")]
		public bool IsFile()
		{
			return !_isDirectory;
		}

		/// <summary>
		/// Определяет, что путь указывает на каталог.
		/// </summary>
		/// <returns><c>true</c>, если это каталог, <c>false</c> - это файл.</returns>
		[ContextMethod("ЭтоКаталог")]
		public bool IsDirectory()
		{
			return _isDirectory;
		}

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:oscriptFtp.FtpFile"/>.
		/// </summary>
		/// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:oscriptFtp.FtpFile"/>.</returns>
		public override string ToString()
		{
			return string.Format("[{0}]", FullName);
		}
	}
}
