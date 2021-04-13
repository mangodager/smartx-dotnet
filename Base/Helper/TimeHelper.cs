using System;

namespace ETModel
{
	public static class TimeHelper
	{
		private static readonly long epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
		/// <summary>
		/// 客户端时间
		/// </summary>
		/// <returns></returns>
		public static long ClientNow()
		{
			return (DateTime.UtcNow.Ticks - epoch) / 10000;
		}

		public static long NowSeconds()
		{
			return (DateTime.UtcNow.Ticks - epoch) / 10000000;
		}

		public static long Now()
		{
            return (DateTime.UtcNow.Ticks - epoch) / 10000;
        }

		public static long NowTicks()
		{
            return (DateTime.UtcNow.Ticks - epoch);
        }

        static long startTime = 0;
        public static float time 
        {
            get
            {
                if(startTime==0)
                    startTime = Now();
                return (float)(Now() - startTime)/1000f;
            }
        }

        public static float deltaTime;

    }
}