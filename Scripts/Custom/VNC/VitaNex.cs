﻿#region Header
//   Vorspire    _,-'/-'/  VitaNex.cs
//   .      __,-; ,'( '/
//    \.    `-.__`-._`:_,-._       _ , . ``
//     `:-._,------' ` _,`--` -: `_ , ` ,' :
//        `---..__,,--'  (C) 2016  ` -'. -'
//        #  Vita-Nex [http://core.vita-nex.com]  #
//  {o)xxx|===============-   #   -===============|xxx(o}
//        #        The MIT License (MIT)          #
#endregion

#region References
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using Server;
using Server.Commands;
using Server.Gumps;

using VitaNex.IO;
using VitaNex.Network;
using VitaNex.SuperGumps;
using VitaNex.SuperGumps.UI;
#endregion

namespace VitaNex
{
	public static class TaskPriority
	{
		public const int Highest = 0, High = 250000, Medium = 500000, Low = 750000, Lowest = 1000000;
	}

	/// <summary>
	///     Exposes an interface for managing VitaNexCore and its' sub-systems.
	/// </summary>
	public static partial class VitaNexCore
	{
		public const AccessLevel Access = AccessLevel.Administrator;

		private static VersionInfo _Version;

		public static VersionInfo Version
		{
			get { return _Version; }
			private set
			{
				_Version = value;

				_Version.Name = "VitaNexCore";
				_Version.Description = "Represents the local version value of Vita-Nex: Core";
			}
		}

		public static readonly object ConsoleLock = new object();
		public static readonly object IOLock = new object();

		private static readonly DateTime _Started = DateTime.UtcNow;

		public static long Ticks
		{
			get
			{
				if (Stopwatch.IsHighResolution && !Core.Unix)
				{
					return (long)(Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency));
				}

				return (long)(DateTime.UtcNow.Ticks * (1000.0 / TimeSpan.TicksPerSecond));
			}
		}

		private static readonly List<ICorePluginInfo> _Plugins = new List<ICorePluginInfo>(0x20);

		public static IEnumerable<ICorePluginInfo> Plugins
		{
			get
			{
				var idx = _Plugins.Count;

				while (--idx >= 0)
				{
					if (_Plugins.InBounds(idx))
					{
						yield return _Plugins[idx];
					}
				}
			}
		}

		public static int PluginCount { get { return _Plugins.Count; } }

		/// <summary>
		///     Gets the amount of time that has passed since VitaNexCore was first initialized.
		/// </summary>
		public static TimeSpan UpTime { get { return DateTime.UtcNow - _Started; } }

		/// <summary>
		///     Gets the root directory for VitaNexCore.
		///     This is the directory where the core scripts reside.
		/// </summary>
		public static DirectoryInfo RootDirectory { get; private set; }

		/// <summary>
		///     Gets the working directory for VitaNexCore.
		/// </summary>
		public static DirectoryInfo BaseDirectory { get; private set; }

		/// <summary>
		///     Gets the build directory for VitaNexCore
		/// </summary>
		public static DirectoryInfo BuildDirectory { get { return IOUtility.EnsureDirectory(BaseDirectory + "/Build/"); } }

		/// <summary>
		///     Gets the data directory for VitaNexCore.
		/// </summary>
		public static DirectoryInfo DataDirectory { get { return IOUtility.EnsureDirectory(BaseDirectory + "/Data/"); } }

		/// <summary>
		///     Gets the cache directory for VitaNexCore
		/// </summary>
		public static DirectoryInfo CacheDirectory { get { return IOUtility.EnsureDirectory(BaseDirectory + "/Cache/"); } }

		/// <summary>
		///     Gets the services directory for VitaNexCore
		/// </summary>
		public static DirectoryInfo ServicesDirectory
		{
			get { return IOUtility.EnsureDirectory(BaseDirectory + "/Services/"); }
		}

		/// <summary>
		///     Gets the modules directory for VitaNexCore
		/// </summary>
		public static DirectoryInfo ModulesDirectory
		{
			get { return IOUtility.EnsureDirectory(BaseDirectory + "/Modules/"); }
		}

		/// <summary>
		///     Gets the saves backup directory for VitaNexCore
		/// </summary>
		public static DirectoryInfo BackupDirectory { get { return IOUtility.EnsureDirectory(BaseDirectory + "/Backups/"); } }

		/// <summary>
		///     Gets the saves directory for VitaNexCore
		/// </summary>
		public static DirectoryInfo SavesDirectory { get { return IOUtility.EnsureDirectory(BaseDirectory + "/Saves/"); } }

		/// <summary>
		///     Gets the logs directory for VitaNexCore
		/// </summary>
		public static DirectoryInfo LogsDirectory { get { return IOUtility.EnsureDirectory(BaseDirectory + "/Logs/"); } }

		/// <summary>
		///     Gets a file used for unhandled and generec exception logging.
		/// </summary>
		public static FileInfo LogFile
		{
			get { return IOUtility.EnsureFile(LogsDirectory + "/Logs (" + DateTime.Now.ToSimpleString("D d M y") + ").log"); }
		}

		/// <summary>
		///     Gets a value representing whether VitaNexCore is busy performing a save or load action
		/// </summary>
		public static bool Busy { get; private set; }

		/// <summary>
		///     Gets a value representing the compile state of VitaNexCore
		/// </summary>
		public static bool Compiled { get; private set; }

		/// <summary>
		///     Gets a value representing the configure state of VitaNexCore
		/// </summary>
		public static bool Configured { get; private set; }

		/// <summary>
		///     Gets a value representing the initialize state of VitaNexCore
		/// </summary>
		public static bool Initialized { get; private set; }

		/// <summary>
		///     Gets a value representing whether this run is the first boot of VitaNexCore
		/// </summary>
		public static bool FirstBoot { get; private set; }

		public static bool Disposing { get; private set; }
		public static bool Disposed { get; private set; }

		public static TimeSpan BackupExpireAge { get; set; }

		public static event Action OnCompiled;
		public static event Action OnConfigured;
		public static event Action OnInitialized;
		public static event Action OnBackup;
		public static event Action OnSaved;
		public static event Action OnLoaded;
		public static event Action OnDispose;
		public static event Action OnDisposed;
		public static event Action<Exception> OnExceptionThrown;

		/// <summary>
		///     Configure method entry point, called by ScriptCompiler during compile of 'Scripts' directory.
		///     Performs a global invoke action, processing all Services and Modules that support CSConfig() and CMConfig()
		/// </summary>
		[CallPriority(Int32.MaxValue)]
		public static void Configure()
		{
			if (Configured)
			{
				return;
			}

			DisplayRetroBoot();

			CommandUtility.Register("VNC", AccessLevel.Administrator, OnCoreCommand);

			OutgoingPacketOverrides.Init();
			ExtendedOPL.Init();

			var now = DateTime.UtcNow;

			ToConsole(String.Empty);
			ToConsole("Compile action started...");

			TryCatch(CompileServices, ToConsole);
			TryCatch(CompileModules, ToConsole);

			Compiled = true;

			if (OnCompiled != null)
			{
				TryCatch(OnCompiled, ToConsole);
			}

			var time = (DateTime.UtcNow - now).TotalSeconds;

			ToConsole("Compile action completed in {0:F2} second{1}", time, (time != 1) ? "s" : String.Empty);

			now = DateTime.UtcNow;

			ToConsole(String.Empty);
			ToConsole("Configure action started...");

			TryCatch(ConfigureServices, ToConsole);
			TryCatch(ConfigureModules, ToConsole);

			Configured = true;

			if (OnConfigured != null)
			{
				TryCatch(OnConfigured, ToConsole);
			}

			time = (DateTime.UtcNow - now).TotalSeconds;

			ToConsole("Configure action completed in {0:F2} second{1}", time, (time != 1) ? "s" : String.Empty);

			ProcessINIT();

			EventSink.ServerStarted += () =>
			{
				EventSink.WorldSave += e =>
				{
					TryCatch(Backup, ToConsole);
					TryCatch(Save, ToConsole);
				};
				EventSink.Shutdown += e => TryCatch(Dispose, ToConsole);
				EventSink.Crashed += e => TryCatch(Dispose, ToConsole);
			};
		}

		/// <summary>
		///     Initialize method entry point, called by ScriptCompiler after compile of 'Scripts' directory.
		///     Performs a global invoke action, processing all Services and Modules that support CSInvoke() and CMInvoke()
		/// </summary>
		[CallPriority(Int32.MaxValue)]
		public static void Initialize()
		{
			if (Initialized)
			{
				return;
			}

			TryCatch(Load, ToConsole);

			var now = DateTime.UtcNow;

			ToConsole(String.Empty);
			ToConsole("Invoke action started...");

			TryCatch(InvokeServices, ToConsole);
			TryCatch(InvokeModules, ToConsole);

			Initialized = true;

			if (OnInitialized != null)
			{
				TryCatch(OnInitialized, ToConsole);
			}

			var time = (DateTime.UtcNow - now).TotalSeconds;

			ToConsole("Invoke action completed in {0:F2} second{1}", time, (time != 1) ? "s" : String.Empty);
		}

		/// <summary>
		///     Performs a global save action, processing all Services and Modules that support CSSave() and CMSave()
		/// </summary>
		public static void Save()
		{
			if (Busy)
			{
				ToConsole("Could not perform save action, the service is busy.");
				return;
			}

			Busy = true;

			var now = DateTime.UtcNow;

			ToConsole(String.Empty);
			ToConsole("Save action started...");

			TryCatch(SaveServices, ToConsole);
			TryCatch(SaveModules, ToConsole);

			if (OnSaved != null)
			{
				TryCatch(OnSaved, ToConsole);
			}

			Busy = false;

			var time = (DateTime.UtcNow - now).TotalSeconds;

			ToConsole("Save action completed in {0:F2} second{1}", time, (time != 1) ? "s" : String.Empty);
		}

		/// <summary>
		///     Performs a global load action, processing all Services and Modules that support CSLoad() and CMLoad()
		/// </summary>
		public static void Load()
		{
			if (Busy)
			{
				ToConsole("Could not perform load action, the service is busy.");
				return;
			}

			Busy = true;

			var now = DateTime.UtcNow;

			ToConsole(String.Empty);
			ToConsole("Load action started...");

			TryCatch(LoadServices, ToConsole);
			TryCatch(LoadModules, ToConsole);

			if (OnLoaded != null)
			{
				TryCatch(OnLoaded, ToConsole);
			}

			Busy = false;

			var time = (DateTime.UtcNow - now).TotalSeconds;

			ToConsole("Load action completed in {0:F2} second{1}", time, (time != 1) ? "s" : String.Empty);
		}

		/// <summary>
		///     Performs a global dispose action, processing all Services and Modules that support CSDispose() and CMDispose()
		/// </summary>
		public static void Dispose()
		{
			if (Busy || Disposing || Disposed)
			{
				ToConsole("Could not perform dispose action, the service is busy.");
				return;
			}

			Busy = Disposing = Disposed = true;

			var now = DateTime.UtcNow;

			ToConsole(String.Empty);
			ToConsole("Dispose action started...");

			if (OnDispose != null)
			{
				TryCatch(OnDispose, ToConsole);
			}

			TryCatch(DisposeServices, ToConsole);
			TryCatch(DisposeModules, ToConsole);

			if (OnDisposed != null)
			{
				TryCatch(OnDisposed, ToConsole);
			}

			Busy = Disposing = false;

			var time = (DateTime.UtcNow - now).TotalSeconds;

			ToConsole("Dispose action completed in {0:F2} second{1}", time, (time != 1) ? "s" : String.Empty);
		}

		/// <summary>
		///     Performs a global backup action, copying all files in the SavesDirectory to the BackupDirectory.
		/// </summary>
		public static void Backup()
		{
			if (Busy)
			{
				ToConsole("Could not perform backup action, the service is busy.");
				return;
			}

			Busy = true;

			var now = DateTime.UtcNow;

			ToConsole(String.Empty);
			ToConsole("Backup action started...");

			if (BackupExpireAge > TimeSpan.Zero)
			{
				ToConsole("Backup Expire Age: {0}", BackupExpireAge);

				lock (IOLock)
				{
						BackupDirectory.EmptyDirectory(BackupExpireAge);
				}
			}

			lock (IOLock)
			{
				SavesDirectory.CopyDirectory(
					IOUtility.EnsureDirectory(BackupDirectory + "/" + DateTime.Now.ToSimpleString("D d M y"), true));
			}

			if (OnBackup != null)
			{
				TryCatch(OnBackup, ToConsole);
			}

			Busy = false;

			var time = (DateTime.UtcNow - now).TotalSeconds;

			ToConsole("Backup action completed in {0:F2} second{1}", time, (time != 1) ? "s" : String.Empty);
		}

		private static void OnCoreCommand(CommandEventArgs e)
		{
			if (e == null || e.Mobile == null || e.Mobile.Deleted)
			{
				return;
			}

			var cmd = e.GetString(0);
			var search = e.GetString(1);

			switch (cmd.ToLower())
			{
				case "srv":
				{
					var cs = !String.IsNullOrWhiteSpace(search)
						? Services.FirstOrDefault(o => Insensitive.Contains(o.Name, search))
						: null;

					VitaNexCoreUI.DisplayTo(e.Mobile, cs);
				}
					break;
				case "mod":
				{
					var cm = !String.IsNullOrWhiteSpace(search)
						? Modules.FirstOrDefault(o => Insensitive.Contains(o.Name, search))
						: null;

					VitaNexCoreUI.DisplayTo(e.Mobile, cm);
				}
					break;
				case "plg":
				{
					var cp = !String.IsNullOrWhiteSpace(search)
						? Plugins.FirstOrDefault(o => Insensitive.Contains(o.Name, search))
						: null;

					VitaNexCoreUI.DisplayTo(e.Mobile, cp);
				}
					break;
				default:
					VitaNexCoreUI.DisplayTo(e.Mobile);
					break;
			}
		}

		public static T TryCatchGet<T>(Func<T> func)
		{
			return TryCatchGet(func, ToConsole);
		}

		public static T TryCatchGet<T>(Func<T> func, Action<Exception> handler)
		{
			if (func == null)
			{
				return default(T);
			}

			try
			{
				return func();
			}
			catch (Exception e)
			{
				if (handler == null)
				{
					ToConsole("{0} at {1}:", e.GetType(), Trace(func));
				}
				else
				{
					handler(e);
				}
			}

			return default(T);
		}

		public static T TryCatchGet<T, TState>(Func<TState, T> func, TState state)
		{
			return TryCatchGet(func, state, ToConsole);
		}

		public static T TryCatchGet<T, TState>(Func<TState, T> func, TState state, Action<Exception> handler)
		{
			if (func == null)
			{
				return default(T);
			}

			try
			{
				return func(state);
			}
			catch (Exception e)
			{
				if (handler == null)
				{
					ToConsole("{0} at {1}:", e.GetType(), Trace(func));
				}
				else
				{
					handler(e);
				}
			}

			return default(T);
		}

		public static void TryCatch(Action action)
		{
			TryCatch(action, ToConsole);
		}

		public static void TryCatch(Action action, Action<Exception> handler)
		{
			if (action == null)
			{
				return;
			}

			try
			{
				action();
			}
			catch (Exception e)
			{
				if (handler == null)
				{
					ToConsole("{0} at {1}:", e.GetType(), Trace(action));
				}
				else
				{
					handler(e);
				}
			}
		}

		public static void TryCatch<T>(Action<T> action, T state)
		{
			TryCatch(action, state, ToConsole);
		}

		public static void TryCatch<T>(Action<T> action, T state, Action<Exception> handler)
		{
			if (action == null)
			{
				return;
			}

			try
			{
				action(state);
			}
			catch (Exception e)
			{
				if (handler == null)
				{
					ToConsole("{0} at {1}:", e.GetType(), Trace(action));
				}
				else
				{
					handler(e);
				}
			}
		}

		public static void Catch(Exception e)
		{
			ToConsole("{0} at {1}:", e.GetType(), new StackTrace());
		}

		public static void WaitWhile(Func<bool> func)
		{
			WaitWhile(func, TimeSpan.MaxValue);
		}

		public static void WaitWhile(Func<bool> func, TimeSpan timeOut)
		{
			if (func == null)
			{
				return;
			}

			var expire = DateTime.UtcNow.Add(timeOut);

			var exit = false;

			while (!exit)
			{
				exit = !func();

				if (DateTime.UtcNow >= expire)
				{
					break;
				}

				Thread.Sleep(1);
			}
		}

		public static void ToConsole(string format, params object[] args)
		{
			lock (ConsoleLock)
			{
				Console.Write('[');
				Utility.PushColor(ConsoleColor.Yellow);
				Console.Write("VitaNexCore");
				Utility.PopColor();
				Console.Write("]: ");
				Utility.PushColor(ConsoleColor.DarkYellow);

				if (args.Length > 0)
				{
					Console.WriteLine(format, args);
				}
				else
				{
					Console.WriteLine(format);
				}

				Utility.PopColor();
			}
		}

		public static void ToConsole(Exception e)
		{
			lock (ConsoleLock)
			{
				Console.Write('[');
				Utility.PushColor(ConsoleColor.Yellow);
				Console.Write("VitaNexCore");
				Utility.PopColor();
				Console.Write("]: ");
				Utility.PushColor(ConsoleColor.DarkRed);
				Console.WriteLine(e);
				Utility.PopColor();
			}

			e.Log(LogFile);

			if (OnExceptionThrown != null)
			{
				OnExceptionThrown(e);
			}
		}

		public static string Trace(this Delegate d)
		{
			return Trace(d, true);
		}

		public static string Trace(this Delegate d, bool output)
		{
			if (d == null)
			{
				return "NULL";
			}

			var value = "";

			var dt = d.Method.DeclaringType;

			while (dt != null)
			{
				value = dt.Name + '.' + value;

				dt = dt.DeclaringType;
			}

			value += d.Method.Name;

			if (output)
			{
				ToConsole("Trace: {0}", value);
			}

			return value;
		}

		private const ConsoleColor _BackgroundColor = ConsoleColor.Black;
		private const ConsoleColor _BorderColor = ConsoleColor.Green;
		private const ConsoleColor _TextColor = ConsoleColor.White;

		private static void DrawLine(string text = "", int align = 0)
		{
			text = text ?? "";
			align = Math.Max(0, Math.Min(2, align));

			var defBG = Console.BackgroundColor;
			const int borderWidth = 2;
			const int indentWidth = 1;
			var maxWidth = Console.WindowWidth - ((borderWidth + indentWidth) * 2);
			var lines = new List<string>();

			if (text.Length > maxWidth)
			{
				var words = text.Split(' ');

				if (words.Length == 0)
				{
					for (int i = 0, offset = 0, count; i < (text.Length / maxWidth); i++)
					{
						lines.Add(text.Substring(offset, (count = Math.Min(text.Length - offset, maxWidth))));
						offset += count;
					}
				}
				else
				{
					var rebuild = String.Empty;

					for (var wi = 0; wi < words.Length; wi++)
					{
						if (rebuild.Length + (words[wi].Length + 1) <= maxWidth)
						{
							rebuild += words[wi] + ' ';
						}
						else
						{
							lines.Add(rebuild);
							rebuild = words[wi] + ' ';
						}

						if (wi + 1 >= words.Length)
						{
							lines.Add(rebuild);
						}
					}
				}
			}
			else
			{
				lines.Add(text);
			}

			lock (ConsoleLock)
			{
				Utility.PushColor(_TextColor);

				foreach (var line in lines)
				{
					Console.BackgroundColor = _BorderColor;
					Console.Write(new String(' ', borderWidth));
					Console.BackgroundColor = _BackgroundColor;
					Console.Write(new String(' ', indentWidth));

					var len = maxWidth - line.Length;
					var str = line;

					switch (align)
					{
						//Center
						case 1:
							str = new String(' ', len / 2) + str + new String(' ', len / 2);
							break;
						//Right
						case 2:
							str = new String(' ', len) + str;
							break;
					}

					if (str.Length < maxWidth)
					{
						str += new String(' ', maxWidth - str.Length);
					}

					Console.Write(str);
					Console.Write(new String(' ', indentWidth));
					Console.BackgroundColor = _BorderColor;
					Console.Write(new String(' ', borderWidth));
				}

				lines.Free(true);

				Console.BackgroundColor = defBG;
				Utility.PopColor();
			}
		}

		private static void DisplayRetroBoot()
		{
			ConsoleColor defBG;

			lock (ConsoleLock)
			{
				defBG = Console.BackgroundColor;
				Console.WriteLine();

				Console.BackgroundColor = _BorderColor;
				Console.Write(new String(' ', Console.WindowWidth));
			}

			DrawLine();
			DrawLine("**** VITA-NEX: CORE " + Version + " ****", 1);
			DrawLine();

			DrawLine("Root Directory:     " + RootDirectory);
			DrawLine("Working Directory:  " + BaseDirectory);
			DrawLine("Build Directory:    " + BuildDirectory);
			DrawLine("Data Directory:     " + DataDirectory);
			DrawLine("Cache Directory:    " + CacheDirectory);
			DrawLine("Services Directory: " + ServicesDirectory);
			DrawLine("Modules Directory:  " + ModulesDirectory);
			DrawLine("Backup Directory:   " + BackupDirectory);
			DrawLine("Saves Directory:    " + SavesDirectory);
			DrawLine("Logs Directory:     " + LogsDirectory);
			DrawLine();

			DrawLine("http://core.vita-nex.com", 1);
			DrawLine();

			if (FirstBoot)
			{
				File.ReadAllLines(IOUtility.GetSafeFilePath(RootDirectory + "/LICENSE", true)).ForEach(line => DrawLine(line));
				DrawLine();
			}

			if (Core.Debug)
			{
				DrawLine("Server is running in DEBUG mode.");
				DrawLine();
			}

			lock (ConsoleLock)
			{
				Console.BackgroundColor = _BorderColor;
				Console.Write(new String(' ', Console.WindowWidth));

				Console.BackgroundColor = defBG;
				Utility.PopColor();
				Console.WriteLine();
			}
		}

		public static void RegisterPlugin(ICorePluginInfo cp)
		{
			if (cp == null)
			{
				return;
			}

			_Plugins.AddOrReplace(cp);

			TryCatch(cp.OnRegistered, cp.ToConsole);
		}
	}

	public sealed class VitaNexCoreUI : TreeGump
	{
		public static void DisplayTo(Mobile user, CoreServiceInfo cs)
		{
			var node = "Plugins|Services";

			if (cs != null)
			{
				node += "|" + cs.FullName;
			}

			DisplayTo(user, false, node);
		}

		public static void DisplayTo(Mobile user, CoreModuleInfo cm)
		{
			var node = "Plugins|Modules";

			if (cm != null)
			{
				node += "|" + cm.FullName;
			}

			DisplayTo(user, false, node);
		}

		public static void DisplayTo(Mobile user, ICorePluginInfo cp)
		{
			if (cp is CoreServiceInfo)
			{
				DisplayTo(user, (CoreServiceInfo)cp);
				return;
			}

			if (cp is CoreModuleInfo)
			{
				DisplayTo(user, (CoreModuleInfo)cp);
				return;
			}

			var node = "Plugins";

			if (cp != null)
			{
				node += "|" + cp.FullName;
			}

			DisplayTo(user, false, node);
		}

		public static void DisplayTo(Mobile user)
		{
			DisplayTo(user, String.Empty);
		}

		public static void DisplayTo(Mobile user, string node)
		{
			DisplayTo(user, false, node);
		}

		public static void DisplayTo(Mobile user, bool refreshOnly)
		{
			DisplayTo(user, refreshOnly, String.Empty);
		}

		public static void DisplayTo(Mobile user, bool refreshOnly, string node)
		{
			if (user == null)
			{
				return;
			}

			if (user.AccessLevel < VitaNexCore.Access)
			{
				user.SendMessage(0x22, "You do not have access to this feature.");
				return;
			}

			var info = EnumerateInstances<VitaNexCoreUI>(user).FirstOrDefault(g => g != null && !g.IsDisposed && g.IsOpen);

			if (info == null)
			{
				if (refreshOnly)
				{
					return;
				}

				info = new VitaNexCoreUI(user);
			}

			if (!String.IsNullOrWhiteSpace(node))
			{
				info.SelectedNode = node;
			}

			info.Refresh(true);
		}

		private List<ICorePluginInfo> _Buffer;
		private int[,] _Indicies;

		private VitaNexCoreUI(Mobile user)
			: base(user)
		{
			_Buffer = new List<ICorePluginInfo>();
			_Indicies = new int[3, 2];

			Title = "Vita-Nex: Core Control Panel";

			LoadAsset("http://core.vita-nex.com/images/icon32b.png");
		}

		protected override void OnDispose()
		{
			_Buffer.Free(true);
			_Buffer = null;

			_Indicies = null;

			base.OnDispose();
		}

		protected override bool OnBeforeSend()
		{
			if (base.OnBeforeSend() && User.AccessLevel >= VitaNexCore.Access)
			{
				return true;
			}

			User.SendMessage(0x22, "You do not have access to this feature.");
			return false;
		}

		protected override void CompileNodes(Dictionary<TreeGumpNode, Action<Rectangle2D, int, TreeGumpNode>> list)
		{
			list.Clear();

			list["Core"] = CompileOverview;

			list["Plugins"] = CompilePlugins;
			list["Plugins|Services"] = CompileServices;
			list["Plugins|Modules"] = CompileModules;

			foreach (var p in VitaNexCore.Plugins)
			{
				var name = p.FullName.Replace('|', '/');

				if (String.IsNullOrWhiteSpace(name))
				{
					name = String.Format("{0}/{1}", p.TypeOf.Name, p.Version);
				}

				if (p is CoreServiceInfo)
				{
					list["Plugins|Services|" + name] = CompileService;
				}
				else if (p is CoreModuleInfo)
				{
					list["Plugins|Modules|" + name] = CompileModule;
				}
				else
				{
					list["Plugins|" + name] = CompilePlugin;
				}
			}

			base.CompileNodes(list);
		}

		protected override void CompileLayout(SuperGumpLayout layout)
		{
			base.CompileLayout(layout);

			layout.Replace(
				"body/mainbutton",
				() =>
				{
					AddButton(101, 9, 5545, 5546, MainButtonHandler);
					AddImage(101, 9, 5545, 2999);
					AddAsset(116, 24, "http://core.vita-nex.com/images/icon32b.png");
				});
		}

		protected override void CompileEmptyNodeLayout(
			SuperGumpLayout layout,
			int x,
			int y,
			int w,
			int h,
			int index,
			TreeGumpNode node)
		{
			base.CompileEmptyNodeLayout(layout, x, y, w, h, index, node);

			layout.Add("node/page/" + index, () => CompileOverview(x, y, w, h));
		}

		private void CompileOverview(Rectangle2D b, int i, TreeGumpNode n)
		{
			CompileOverview(b.X, b.Y, b.Width, b.Height);
		}

		private void CompileOverview(int x, int y, int w, int h)
		{
			var html = new StringBuilder();

			html.AppendLine();
			html.AppendLine("Vita-Nex: Core " + "{0}".WrapUOHtmlColor(Color.LawnGreen, HtmlColor), VitaNexCore.Version);
			html.AppendLine("A dynamic extension library for RunUO");
			html.AppendLine("<a href=\"http://core.vita-nex.com\">http://core.vita-nex.com</a>");
			html.AppendLine();
			html.AppendLine("Services: " + "{0:#,0}".WrapUOHtmlColor(Color.LawnGreen, HtmlColor), VitaNexCore.ServiceCount);
			html.AppendLine("Modules:  " + "{0:#,0}".WrapUOHtmlColor(Color.LawnGreen, HtmlColor), VitaNexCore.ModuleCount);
			html.AppendLine();

			AddHtml(x + 5, y, w - 10, h, html.ToString().WrapUOHtmlColor(HtmlColor, false), false, true);
		}

		private void CompileService(Rectangle2D b, int i, TreeGumpNode n)
		{
			var cs = VitaNexCore.Services.FirstOrDefault(o => Insensitive.EndsWith(n.Name, o.FullName));

			if (cs != null)
			{
				CompileService(b.X, b.Y, b.Width, b.Height, cs);
			}
		}

		private void CompileService(int x, int y, int w, int h, CoreServiceInfo cs)
		{
			CompileBufferEntry(x, y, w, h, 0, cs);

			y += 75;
			h -= 75;

			cs.CompileControlPanel(this, x, y, w, h);
		}

		private void CompileModule(Rectangle2D b, int i, TreeGumpNode n)
		{
			var cm = VitaNexCore.Modules.FirstOrDefault(o => Insensitive.EndsWith(n.Name, o.FullName));

			if (cm != null)
			{
				CompileModule(b.X, b.Y, b.Width, b.Height, cm);
			}
		}

		private void CompileModule(int x, int y, int w, int h, CoreModuleInfo cm)
		{
			CompileBufferEntry(x, y, w, h, 0, cm);

			y += 75;
			h -= 75;

			cm.CompileControlPanel(this, x, y, w, h);
		}

		private void CompilePlugin(Rectangle2D b, int i, TreeGumpNode n)
		{
			var cp = VitaNexCore.Plugins.FirstOrDefault(o => Insensitive.EndsWith(n.Name, o.FullName));

			if (cp != null)
			{
				CompilePlugin(b.X, b.Y, b.Width, b.Height, cp);
			}
		}

		private void CompilePlugin(int x, int y, int w, int h, ICorePluginInfo cp)
		{
			CompileBufferEntry(x, y, w, h, 0, cp);

			y += 75;
			h -= 75;

			cp.CompileControlPanel(this, x, y, w, h);
		}

		private void CompileServices(Rectangle2D b, int i, TreeGumpNode n)
		{
			CompileBuffer(0, b.X, b.Y, b.Width, b.Height, VitaNexCore.Services);
		}

		private void CompileModules(Rectangle2D b, int i, TreeGumpNode n)
		{
			CompileBuffer(1, b.X, b.Y, b.Width, b.Height, VitaNexCore.Modules);
		}

		private void CompilePlugins(Rectangle2D b, int i, TreeGumpNode n)
		{
			CompileBuffer(
				2,
				b.X,
				b.Y,
				b.Width,
				b.Height,
				VitaNexCore.Plugins.Not(o => o is CoreServiceInfo || o is CoreModuleInfo));
		}

		private void CompileBuffer(int i, int x, int y, int w, int h, IEnumerable<ICorePluginInfo> plugins)
		{
			_Buffer.Clear();
			_Buffer.AddRange(plugins);
			_Buffer.Sort();

			_Indicies[i, 1] = _Buffer.Count;
			_Indicies[i, 0] = Math.Max(0, Math.Min(_Indicies[i, 1] - 1, _Indicies[i, 0]));

			var idx = 0;

			foreach (var cp in _Buffer.Skip(_Indicies[i, 0]).Take(5))
			{
				CompileBufferEntry(x, y, w - 25, h, idx++, cp);
			}

			_Buffer.Clear();

			AddBackground(x + (w - 25), y, 28, 351, 2620);

			AddScrollbarV(
				x + (w - 24),
				y,
				_Indicies[i, 1],
				_Indicies[i, 0],
				b =>
				{
					--_Indicies[i, 0];
					Refresh(true);
				},
				b =>
				{
					++_Indicies[i, 0];
					Refresh(true);
				},
				new Rectangle2D(6, 42, 13, 267),
				new Rectangle2D(6, 10, 13, 28),
				new Rectangle2D(6, 313, 13, 28),
				Tuple.Create(10740, 10742),
				Tuple.Create(10701, 10702, 10700),
				Tuple.Create(10721, 10722, 10720));
		}

		private void CompileBufferEntry(int x, int y, int w, int h, int i, ICorePluginInfo cp)
		{
			if (w * h <= 0 || cp == null)
			{
				return;
			}

			var xx = x;
			var yy = y + (i * 70);

			AddRectangle(xx, yy, w, 65, Color.Black, cp.Active ? Color.PaleGoldenrod : Color.Silver, 2);

			xx += 5;
			yy += 5;

			var label = cp.Name.WrapUOHtmlColor(HtmlColor, false);

			AddHtml(xx, yy, w - 70, 40, label, false, false);

			xx = x + (w - 60);

			label = cp.Version.ToString().WrapUOHtmlColor(HtmlColor, false);

			AddHtml(xx, yy, 55, 40, label, false, false);

			xx = x + 5;
			yy += 25;

			Color color, border, fill;

			if (cp.Disposed)
			{
				label = "DISPOSED".WrapUOHtmlBold().WrapUOHtmlCenter();
				color = border = Color.OrangeRed;
				fill = Color.Black;

				AddRectangle(xx, yy, w - 10, 30, fill, border, 1);
				AddHtml(xx, yy, w, 30, label.WrapUOHtmlColor(color, false), false, false);

				return;
			}

			var bw = (w - 10) / 4;

			label = "CONFIG".WrapUOHtmlBold().WrapUOHtmlCenter();
			color = border = Color.PaleGoldenrod;
			fill = Color.Black;

			AddHtmlButton(xx, yy, bw, 30, o => HandleConfig(cp), label, color, fill, border, 1);

			xx += bw;

			label = "DEBUG".WrapUOHtmlBold().WrapUOHtmlCenter();
			color = border = cp.Debug ? Color.LawnGreen : Color.OrangeRed;

			AddHtmlButton(xx, yy, bw, 30, o => HandleDebug(cp), label, color, fill, border, 1);

			xx += bw;

			label = "QUIET".WrapUOHtmlBold().WrapUOHtmlCenter();
			color = border = cp.Quiet ? Color.LawnGreen : Color.OrangeRed;

			AddHtmlButton(xx, yy, bw, 30, o => HandleQuiet(cp), label, color, fill, border, 1);

			xx += bw;

			label = "ACTIVE".WrapUOHtmlBold().WrapUOHtmlCenter();
			color = border = cp.Active ? Color.LawnGreen : Color.OrangeRed;

			AddHtmlButton(xx, yy, bw, 30, o => HandleActive(cp), label, color, fill, border, 1);
		}

		private void HandleConfig(ICorePluginInfo cp)
		{
			Refresh(true);

			if (cp != null && !cp.Disposed)
			{
				User.SendGump(new PropertiesGump(User, cp));
			}
		}

		private void HandleDebug(ICorePluginInfo cp)
		{
			if (cp != null && !cp.Disposed)
			{
				var old = cp.Debug;

				cp.Debug = !cp.Debug;

				if (cp.Debug != old)
				{
					User.SendMessage(85, "[{0}]: Debugging {1}", cp.Name, cp.Debug ? "enabled" : "disabled");
				}
			}

			Refresh(true);
		}

		private void HandleQuiet(ICorePluginInfo cp)
		{
			if (cp != null && !cp.Disposed)
			{
				var old = cp.Quiet;

				cp.Quiet = !cp.Quiet;

				if (cp.Quiet != old)
				{
					User.SendMessage(85, "[{0}]: Using {1} output", cp.Name, cp.Quiet ? "simple" : "extended");
				}
			}

			Refresh(true);
		}

		private void HandleActive(ICorePluginInfo cp)
		{
			if (cp != null && !cp.Disposed)
			{
				var old = cp.Active;

				cp.Active = !cp.Active;

				if (cp.Active != old)
				{
					User.SendMessage(85, "[{0}]: Now {1}", cp.Name, cp.Active ? "active" : "inactive");
				}
			}

			Refresh(true);
		}
	}

	public interface ICorePluginInfo : IEquatable<ICorePluginInfo>, IComparable<ICorePluginInfo>
	{
		bool Active { get; set; }

		bool Configured { get; }
		bool Invoked { get; }
		bool Disposed { get; }

		Assembly DynamicAssembly { get; }
		FileInfo DynamicAssemblyFile { get; }

		bool Dynamic { get; }
		int Priority { get; }
		Type TypeOf { get; }
		VersionInfo Version { get; }

		string Name { get; }
		string FullName { get; }

		bool Debug { get; set; }
		bool Quiet { get; set; }

		void OnRegistered();

		void SaveState();
		void LoadState();

		void SaveOptions();
		void LoadOptions();

		void ToConsole(string[] lines);
		void ToConsole(string format, params object[] args);
		void ToConsole(Exception[] errors);
		void ToConsole(Exception e);

		void CompileControlPanel(SuperGump g, int x, int y, int w, int h);
	}
}