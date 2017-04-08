using System;
using System.Linq;
using System.Collections.Generic;

namespace oscriptFtp
{
	public static class FtpCrutch
	{

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
