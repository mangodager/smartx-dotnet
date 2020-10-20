using System;
using System.Collections.Generic;

namespace ETModel
{
	[Flags]
	public enum AppType
	{
		None  = 0,
        Core  = 1,
        Gate  = 1 << 1,
        Out   = 1 << 10,

        // 
        All = Core | Gate,
    }

	public static class AppTypeHelper
	{
		public static List<AppType> GetTypes()
		{
			List<AppType> appTypes = new List<AppType> { AppType.Core, AppType.Gate, AppType.All };
			return appTypes;
		}

		public static bool Is(this int a, AppType b)
		{
			if ((a & (int)b) != 0)
			{
				return true;
			}
			return false;
		}
	}
}