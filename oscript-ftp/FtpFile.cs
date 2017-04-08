using System;
using ScriptEngine.Machine.Contexts;
using ScriptEngine.Machine;

namespace oscriptFtp
{
	[ContextClass("FTPФайл")]
	public sealed class FtpFile : AutoContext<FtpFile>
	{
		private readonly bool _isDirectory;

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

		[ContextProperty("Имя")]
		public string Name { get; }

		[ContextProperty("ИмяБезРасширения")]
		public string BaseName { get; }

		[ContextProperty("ПолноеИмя")]
		public string FullName { get; }

		[ContextProperty("Путь")]
		public string Path { get; }

		[ContextProperty("Расширение")]
		public string Extension { get; }

		[ContextMethod("ЭтоФайл")]
		public bool IsFile()
		{
			return !_isDirectory;
		}

		[ContextMethod("ЭтоКаталог")]
		public bool IsDirectory()
		{
			return _isDirectory;
		}

		public override string ToString()
		{
			return string.Format("[{0}]", FullName);
		}
	}
}
