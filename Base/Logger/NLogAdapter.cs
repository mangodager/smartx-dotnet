
#if SERVER
using NLog;
#endif

namespace ETModel
{
	public class NLogAdapter: ALogDecorater, ILog
	{
#if SERVER
        private readonly Logger logger = LogManager.GetLogger("Logger");

		public NLogAdapter(ALogDecorater decorater = null): base(decorater)
		{
		}

		public void Trace(string message)
		{
			this.logger.Trace(this.Decorate(message));
		}

		public void Warning(string message)
		{
			this.logger.Warn(this.Decorate(message));
		}

		public void Info(string message)
		{
			this.logger.Info(this.Decorate(message));
		}

		public void Debug(string message)
		{
			this.logger.Debug(this.Decorate(message));
		}

		public void Error(string message)
		{
			this.logger.Error(this.Decorate(message));
		}

        public void Fatal(string message)
        {
            this.logger.Fatal(this.Decorate(message));
        }
#else
        public NLogAdapter(ALogDecorater decorater = null) : base(decorater)
        {
        }

        public void Trace(string message)
        {
            Debug(this.Decorate(message));
        }

        public void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(this.Decorate(message));
        }

        public void Info(string message)
        {
            UnityEngine.Debug.Log(this.Decorate(message));
        }

        public void Debug(string message)
        {
            UnityEngine.Debug.Log(this.Decorate(message));
        }

        public void Error(string message)
        {
            UnityEngine.Debug.LogError(this.Decorate(message));
        }

        public void Fatal(string message)
        {
            UnityEngine.Debug.Log(this.Decorate(message));
        }
#endif

    }
}