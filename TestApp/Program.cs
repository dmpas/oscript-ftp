/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using ScriptEngine.Machine;
using ScriptEngine.HostedScript;
using ScriptEngine.HostedScript.Library;
using System.Configuration;
using System.Net;
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

		private static void ListFiles(FtpConnection conn)
		{
			var files = conn.FindFiles("", "*.zip", true);
			foreach (var el in files)
			{
				var file = el as FtpFile;
				Console.WriteLine("file: {0}, Size={1}, Time={2}", el, file.Size(), file.GetModificationTime());
			}

		}

		public static void Main(string[] args)
		{
			var server = ConfigurationManager.AppSettings["server"];
			var userName = ConfigurationManager.AppSettings["userName"];
			var password = ConfigurationManager.AppSettings["password"];
			var port = int.Parse(ConfigurationManager.AppSettings["port"]);
			
			var engine = StartEngine();
			var script = engine.Loader.FromString(SCRIPT);
			var process = engine.CreateProcess(new MainClass(), script);

			var conn = FtpConnection.Constructor(ValueFactory.Create(server), ValueFactory.Create(port),
			                                     ValueFactory.Create(userName), ValueFactory.Create(password),
			                                     null, ValueFactory.Create(true)) as FtpConnection;
			conn.SetCurrentDirectory("/123");
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());

			conn.SetCurrentDirectory("456");
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());

			conn.Delete(@"../", @"some.zip");
			
			conn.Put(@"D:\temp\some.zip", "some.zip");
			
			ListFiles(conn);
			
			conn.Move("some.zip", "../some.zip");
			
			ListFiles(conn);
			
			conn.Get(@"../some.zip", @"C:\temp\some.zip");
			
			Console.WriteLine("Done.");
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
