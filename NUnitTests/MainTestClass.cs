/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Collections.Generic;
using NUnit.Framework;
using oscriptFtp;

// Используется NUnit 3.6

namespace NUnitTests
{
	[TestFixture]
	public class MainTestClass
	{

		private EngineHelpWrapper host;

		private string[] ls_response = new []{
				"123",
				"abc",
				"some.txt",
				"zyv",
				"абв",
				"файл.txt",
				"яязю"
			};

		private Dictionary<string, bool> isDirDictionary = new Dictionary<string, bool>()
		{
			{"123", true},
			{"abc", true},
			{"some.txt", false},
			{"zyv", true},
			{"абв", true},
			{"файл.txt", false},
			{"яязю", true}
		};

		[OneTimeSetUp]
		public void Initialize()
		{
			host = new EngineHelpWrapper();
			host.StartEngine();
		}

		[Test]
		public void TestAsInternalObjects()
		{

		}

		private void TestFileAndDirectoryListProcessing(string[] dir_response)
		{
			var dict = FtpCrutch.MatchLists(dir_response, ls_response);

			for (var i = 0; i < ls_response.Length; i++)
			{
				var key = ls_response[i];
				Assert.That<string>(dict[key], Is.EqualTo(dir_response[i]));
			}
			
			IList<string> files = new List<string>();
			IList<string> directories = new List<string>();
			
			FtpCrutch.SortData(dict, ref files, ref directories);
			foreach (var filename in files)
			{
				Assert.That<bool>(isDirDictionary[filename], Is.EqualTo(false));
			}
			foreach (var filename in directories)
			{
				Assert.That<bool>(isDirDictionary[filename], Is.EqualTo(true));
			}
		}

		[Test]
		public void TestIisMsDosModeParser()
		{
			var dir_response = new[]
			{
				"07-01-19  09:02AM       <DIR>          123",
				"07-01-19  09:02AM       <DIR>          abc",
				"02-19-19  02:11PM               921016 some.txt",
				"07-01-19  09:02AM       <DIR>          zyv",
				"07-01-19  09:03AM       <DIR>          абв",
				"02-19-19  02:11PM               921016 файл.txt",
				"07-01-19  09:04AM       <DIR>          яязю"
			};

			TestFileAndDirectoryListProcessing(dir_response);
		}

		[Test]
		public void TestIisUnixModeParser()
		{
			var dir_response = new[]
			{
				"drwxrwxrwx   1 owner    group               0 Jul  1  9:02 123",
				"drwxrwxrwx   1 owner    group               0 Jul  1  9:02 abc",
				"-rwxrwxrwx   1 owner    group          921016 Feb 19 14:11 some.txt",
				"drwxrwxrwx   1 owner    group               0 Jul  1  9:02 zyv",
				"drwxrwxrwx   1 owner    group               0 Jul  1  9:03 абв",
				"-rwxrwxrwx   1 owner    group          921016 Feb 19 14:11 файл.txt",
				"drwxrwxrwx   1 owner    group               0 Jul  1  9:04 яязю"
			};

			TestFileAndDirectoryListProcessing(dir_response);
		}
		
		[Test]
		public void TestAsExternalObjects()
		{
			host.RunTestScript("NUnitTests.Tests.external.os");
		}
	}
}
