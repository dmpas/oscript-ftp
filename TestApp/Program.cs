using System;
using ScriptEngine.Machine;
using ScriptEngine.HostedScript;
using ScriptEngine.HostedScript.Library;
using oscriptFtp;

namespace TestApp
{
	class MainClass : IHostApplication
	{

		static readonly string SCRIPT = @""
			;

		public static HostedScriptEngine StartEngine()
		{
			var engine = new ScriptEngine.HostedScript.HostedScriptEngine();
			engine.Initialize();

			engine.AttachAssembly(System.Reflection.Assembly.GetAssembly(typeof(oscriptFtp.FtpConnection)));

			return engine;
		}

		public static void Main(string[] args)
		{
			var engine = StartEngine();
			var script = engine.Loader.FromString(SCRIPT);
			var process = engine.CreateProcess(new MainClass(), script);

			var conn = FtpConnection.Constructor(ValueFactory.Create("10.2.150.7"), ValueFactory.Create(21),
			                                     ValueFactory.Create("update"), ValueFactory.Create("Black34")) as FtpConnection;
			conn.SetCurrentDirectory("Storage1C");
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());
			conn.SetCurrentDirectory("Obmen");
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());
			conn.SetCurrentDirectory("/Storage1C/Obmen");
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());

			var files = conn.FindFiles("", "", true);
			var first = true;
			foreach (var el in files)
			{
				var file = el as FtpFile;
				Console.WriteLine("file: {0}", el);
				if (first)
				{
					conn.Get(file.FullName, @"C:\temp\some.zip");
					conn.Delete(file.FullName);
					conn.Put(@"C:\temp\some.zip", file.FullName);
					first = false;
				}

			}

			Console.ReadKey();
		}

		public void Echo(string str, MessageStatusEnum status = MessageStatusEnum.Ordinary)
		{
			Console.WriteLine(str);
		}

		public void ShowExceptionInfo(Exception exc)
		{
			Console.WriteLine(exc.ToString());
		}

		public bool InputString(out string result, int maxLen)
		{
			throw new NotImplementedException();
		}

		public string[] GetCommandLineArguments()
		{
			return new string[] { "1", "2", "3" }; // Здесь можно зашить список аргументов командной строки
		}
	}
}
