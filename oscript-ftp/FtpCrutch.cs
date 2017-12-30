/*----------------------------------------------------------
This Source Code Form is subject to the terms of the 
Mozilla Public License, v.2.0. If a copy of the MPL 
was not distributed with this file, You can obtain one 
at http://mozilla.org/MPL/2.0/.
----------------------------------------------------------*/
using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace oscriptFtp
{
	static class FtpCrutch
	{

		public static Regex GetRegexForFileMask(string FileMask)
		{
			var regexBuilder = new StringBuilder();
			foreach (var c in FileMask)
			{
				if (c == '.')
				{
					regexBuilder.Append("[.]");
				}
				else
				if (c == '*')
				{
					regexBuilder.Append(".*");
				}
				else
				if (c == '?')
				{
					regexBuilder.Append(".");
				}
				else
				if (("+()^$.{}[]|\\".IndexOf(c) != -1))
				{
					regexBuilder.Append("\\").Append(c);
				}
				else
				{
					regexBuilder.Append(c);
				}
			}

			return new Regex(regexBuilder.ToString(), RegexOptions.Compiled);
		}

		public static IDictionary<string, string> MatchLists(
			IReadOnlyList<string> fullData,
			IReadOnlyList<string> names)
		{

			var mNames = new List<string>(names);
			var mData = new List<string>(fullData);

			var result = new Dictionary<string, string>();

			var reiterate = true;

			while (reiterate)
			{
				reiterate = false;
				var nameToDelete = "";
				foreach (var name in mNames)
				{
					var allMatching = mData.FindAll((obj) => obj.EndsWith(name, StringComparison.Ordinal));
					if (allMatching.Count == 1)
					{
						result.Add(name, allMatching[0]);
						mData.Remove(allMatching[0]);
						reiterate = true;
						nameToDelete = name;
						break;
					}
				}
				if (!string.IsNullOrEmpty(nameToDelete))
				{
					mNames.Remove(nameToDelete);
				}
			}

			return result;
		}

	}
}
