﻿#region Header
//   Vorspire    _,-'/-'/  Updates.cs
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
using System.Net;
using System.Text;

using Server;
using Server.Mobiles;

using VitaNex.Schedules;
using VitaNex.Web;
#endregion

namespace VitaNex.Updates
{
	public static partial class UpdateService
	{
		public const AccessLevel Access = AccessLevel.Administrator;

		public const string DefaultURL = "http://core.vita-nex.com/svn/VERSION";

		private static readonly object _Lock = new object();

		private static Timer _Timeout;
		private static VersionInfo _RemoteVersion;

		public static VersionInfo LocalVersion { get { return VitaNexCore.Version; } }

		public static VersionInfo RemoteVersion
		{
			get { return _RemoteVersion; }
			private set
			{
				_RemoteVersion = value;
				RemoteVersion.Name = "VitaNexCore";
				RemoteVersion.Description = "Represents the remote version value of Vita-Nex: Core";
			}
		}

		public static UpdateServiceOptions CSOptions { get; private set; }

		public static List<PlayerMobile> Staff { get; private set; }

		public static Uri URL { get; set; }

		public static event Action<VersionInfo, VersionInfo> OnVersionResolved;

		private static void OnScheduleTick(Schedule s)
		{
			RequestVersion();
		}

		public static void RequestVersion()
		{
			if (_Timeout != null && _Timeout.Running)
			{
				CSOptions.ToConsole("Previous request has not been handled yet.");
				return;
			}

			CSOptions.ToConsole("Requesting remote version...");

			_Timeout = Timer.DelayCall(
				TimeSpan.FromMilliseconds(CSOptions.Timeout.TotalMilliseconds + 1000),
				() =>
				{
					CSOptions.ToConsole("Request timed-out.");

					NotifyStaff("Update request failed, the connection timed-out.", true, 1.0, 10.0);
				});

			WebAPI.BeginRequest(URL != null ? URL.ToString() : DefaultURL, null, OnSend, OnReceive);
		}

		private static void OnSend(HttpWebRequest req, object state)
		{
			req.Timeout = (int)CSOptions.Timeout.TotalMilliseconds;

			if (URL == null)
			{
				URL = req.RequestUri;
			}
		}

		private static void OnReceive(HttpWebRequest req, object state, HttpWebResponse res)
		{
			OnDataReceived(res.GetContent());

			if (_Timeout == null)
			{
				return;
			}

			_Timeout.Stop();
			_Timeout = null;
		}

		private static void OnDataReceived(string data)
		{
			CSOptions.ToConsole("{0} bytes of data received, parsing...", Encoding.Default.GetByteCount(data));

			VersionInfo version;

			if (!VersionInfo.TryParse(data, out version))
			{
				CSOptions.ToConsole("The remote version could not be resolved.");
				NotifyStaff("Update request failed, the remote version could not be resolved.", true, 1.0, 10.0);
				return;
			}

			RemoteVersion = version;

			CSOptions.ToConsole("Remote version resolved as {0}", RemoteVersion);

			if (LocalVersion >= RemoteVersion)
			{
				NotifyStaff(
					String.Format("No updates are available, your version [b]{0}[/b] is up-to-date.", LocalVersion),
					true,
					1.0,
					10.0);
			}
			else
			{
				NotifyStaff(
					String.Format(
						"Updates are available, your version [b]{0}[/b] is out-of-date, the remote version is [b]{1}[/b].",
						LocalVersion,
						RemoteVersion),
					true,
					1.0,
					10.0);
			}

			if (OnVersionResolved != null)
			{
				OnVersionResolved(LocalVersion, RemoteVersion);
			}
		}

		private static void NotifyStaff(string message, bool autoClose = true, double delay = 1.0, double pause = 3.0)
		{
			if (!CSOptions.NotifyStaff)
			{
				return;
			}

			lock (_Lock)
			{
				Staff.RemoveAll(m => !m.IsOnline() || m.Account.AccessLevel < CSOptions.NotifyAccess);
				Staff.Free(false);
			}

			message = String.Format("[url=http://core.vita-nex.com]Vita-Nex: Core[/url][br]{0}", message);

			lock (_Lock)
			{
				Staff.ForEach(m => m.SendNotification<UpdatesNotifyGump>(message, autoClose, delay, pause));
			}
		}
	}
}