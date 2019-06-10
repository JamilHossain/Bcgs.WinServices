
//namespace Bcgs.WinService.Scheduler
//{
//	using System;
//	using System.Collections.Generic;
//	using System.Linq;
//	using System.Text;

//	internal enum LogLevel : byte
//	{
//		All = 0,
//		Trace = 1,
//		Debug = 2,
//		Info = 3,
//		Warn = 4,
//		Error = 5,
//		Fatal = 6
//	}

//	internal static class Logger
//	{
//		private static Object consoleLockObj = new Object();
//		private static Object fileLockObj = new Object();
//		private static LogLevel CurrentLevel = LogLevel.Info;
//		private static String logFile = null;

//		static Logger()
//		{
//			String appLogLevel = System.Configuration.ConfigurationManager.AppSettings["SelfLogLevel"];
//			CurrentLevel = (LogLevel)Enum.Parse(typeof(LogLevel), appLogLevel);
//			logFile = System.Configuration.ConfigurationManager.AppSettings["SelfLogFile"];
//			if (!String.IsNullOrEmpty(logFile))
//			{
//				logFile = Environment.ExpandEnvironmentVariables(logFile);
//			}
//		}


//		private static Boolean CanLog(LogLevel level)
//		{
//			return level >= CurrentLevel;
//		}
//		private static ConsoleColor getColor(LogLevel level)
//		{
//			ConsoleColor ans = Console.ForegroundColor;
//			switch (level)
//			{
//				case LogLevel.All:
//					ans = ConsoleColor.White;
//					break;
//				case LogLevel.Trace:
//					ans = ConsoleColor.Green;
//					break;
//				case LogLevel.Debug:
//					ans = ConsoleColor.Yellow;
//					break;
//				case LogLevel.Info:
//					ans = ConsoleColor.Gray;
//					break;
//				case LogLevel.Warn:
//					ans = ConsoleColor.Cyan;
//					break;
//				case LogLevel.Error:
//					ans = ConsoleColor.Red;
//					break;
//				case LogLevel.Fatal:
//					ans = ConsoleColor.Magenta;

//					break;
//				default:
//					break;
//			}
//			return ans;
//		}

//		internal static void WriteMessage(LogLevel level, String msg)
//		{
//			if (CanLog(level))
//			{
//				if (Environment.UserInteractive)
//				{
//					lock (consoleLockObj)
//					{
//						ConsoleColor currentColor = Console.ForegroundColor;
//						Console.ForegroundColor = getColor(level);
//						Console.WriteLine("{0} - {1}", DateTime.Now, msg);
//						Console.ForegroundColor = currentColor;
//					}
//				}
//				if (!String.IsNullOrEmpty(logFile))
//				{
//					lock (fileLockObj)
//					{
//						//TODO: Roll log (max size / daily)
//						System.IO.File.AppendAllText(logFile, String.Format("{0} - {1}{2}", DateTime.Now, msg, Environment.NewLine), Encoding.UTF8);
//					}
//				}
//			}
//		}
//		internal static void WriteMessage(String msg)
//		{
//			WriteMessage(LogLevel.Info, msg);
//		}
//		internal static void WriteMessage(LogLevel level, String format, params String[] args)
//		{
//			WriteMessage(level, String.Format(format, args));
//		}
//		internal static void WriteMessage(String format, params String[] args)
//		{
//			WriteMessage(String.Format(format, args));
//		}

//		internal static void WriteException(Exception e, String msg)
//		{
//			WriteMessage(LogLevel.Error, "{0} - {1} - {2}", msg, e.Message, e.StackTrace);
//		}
//	}
//}
