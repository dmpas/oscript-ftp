/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using Microsoft.Extensions.Configuration;
using OneScript.StandardLibrary;
using oscriptFtp;
using ScriptEngine.HostedScript;
using ScriptEngine.Hosting;
using ScriptEngine.Machine;
using System;

namespace TestApp
{
	class MainClass : IHostApplication
	{

		static readonly string SCRIPT = @""
			;

        public static HostedScriptEngine StartEngine()
        {
            var mainEngine = DefaultEngineBuilder.Create()
                .SetDefaultOptions()
                .SetupEnvironment(envSetup => {
                    envSetup
                        .AddStandardLibrary()
                        .AddAssembly(typeof(FtpFile).Assembly);
                })
                .Build();
            var engine = new HostedScriptEngine(mainEngine);
            engine.Initialize();

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
            /*
             * Необходимо создать файл с именем appsettings.json и содержимым такого вида:
				{
				  "AppSettings": {
					  "server": "ftp.dlptest.com",
					  "userName": "dlpuser",
					  "password": "***"
				  }
				}
			*/
            var cfg = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
				.Build();
			var server = cfg.GetSection("AppSettings:server").Value;
            var userName = cfg.GetSection("AppSettings:userName").Value;
            var password = cfg.GetSection("AppSettings:password").Value;
            var port = int.Parse(cfg.GetSection("AppSettings:port").Value ?? "21");

            var engine = StartEngine();
			var bslProcess = engine.Engine.NewProcess();
			var script = engine.Loader.FromString(SCRIPT);
			var process = engine.CreateProcess(new MainClass(), script);

			var conn = FtpConnection.Constructor(server, port, userName, password) as FtpConnection;
			conn.SetCurrentDirectory("/123");
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());

			conn.SetCurrentDirectory("456");
			Console.WriteLine("PWD: {0}", conn.GetCurrentDirectory());

            conn.Delete(@"../", @"some.zip");

			conn.Put(@"C:\temp\some.zip", "/some.zip");
			
			ListFiles(conn);
			
			conn.Move("/some.zip", "/some2.zip");
			
			ListFiles(conn);
			
			conn.Get(@"/some2.zip", @"C:\temp\some2.zip");
			
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

		public bool InputString(out string result, string prompt, int maxLen, bool multiline)
        {
			throw new NotImplementedException();
		}

		public string[] GetCommandLineArguments()
		{
			return new string[] { "1", "2", "3" }; // Здесь можно зашить список аргументов командной строки
		}

    }
}
