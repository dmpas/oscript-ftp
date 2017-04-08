using System;
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

			var conn = FtpConnection.Constructor("localhost", 21, "dmpas", "") as FtpConnection;
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());

			var files = conn.FindFiles("workspace", "*.deb", true);

			foreach (var el in files)
			{
				Console.WriteLine("file: {0}", el);
			}

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
