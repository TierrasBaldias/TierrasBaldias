/*
*	A Brand New Ultra Cool Stats Anouncer by Flexo V 0.01a (I.S.F.W.I.C.M.E) Edition.
*/
using System;
using System.Collections;
using System.IO;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Misc;
using System.Net;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Server.Items;
using Server.Prompts;
using Server.Network;
using Server.Accounting;
using Server.Commands;

namespace Server.Misc
{
	public class SrvInfo : Timer
	{
		private static TimeSpan m_Delay = TimeSpan.FromMinutes( 20 );


		public static void Initialize()
		{
			new SrvInfo().Start();
			//CommandSystem.Register( "botinfo", AccessLevel.Administrator, new CommandEventHandler( SrvInfo_OnCommand ) );
		}

		private static bool m_SrvInfoEnabled = true;

		public static bool SrvInfoEnabled
		{
			get{ return m_SrvInfoEnabled; }
			set{ m_SrvInfoEnabled = value; }
		}



		public SrvInfo() : base( m_Delay, m_Delay )
		{
			Priority = TimerPriority.OneMinute;
		}

		protected override void OnTick()
		{
		
			string s0 = "3[4TB3] 3[4Estadisticas del Servidor3]";
			string s1 = "3[4TB3]  Clientes: " + NetState.Instances.Count.ToString() + "  Items: " + World.Items.Count.ToString() + "  Npcs: " + World.Mobiles.Count.ToString() + "  Online: " + FormatTimeSpan( DateTime.Now - Clock.ServerStart );
			string s2 = "3[4TB3]  Ram: " + FormatByteAmount( GC.GetTotalMemory( false ) ) + "  Uso de CPU: " + Core.CyclesPerSecond.ToString( "N2" ) + " (" + Core.AverageCPS.ToString( "N2" ) + ") media" ;
			/*
			Console.WriteLine( s0 );
			Console.WriteLine( s1 );
			Console.WriteLine( s2 );
			*/
			Server.Misc.IRCBot.SendToIRC( s0 );
			Server.Misc.IRCBot.SendToIRC( s1 );
			Server.Misc.IRCBot.SendToIRC( s2 );
			//SendToIRC(s2, false, tierras_staff)
		}
		
		public static string FormatTimeSpan( TimeSpan ts )
		{
			return String.Format( "{0:D2}:{1:D2}:{2:D2}:{3:D2}", ts.Days, ts.Hours % 24, ts.Minutes % 60, ts.Seconds % 60 );
		}

		public static string FormatByteAmount( long totalBytes )
		{
			if ( totalBytes > 1000000000 )
				return String.Format( "{0:F1} GB", (double)totalBytes / 1073741824 );

			if ( totalBytes > 1000000 )
				return String.Format( "{0:F1} MB", (double)totalBytes / 1048576 );

			if ( totalBytes > 1000 )
				return String.Format( "{0:F1} KB", (double)totalBytes / 1024 );

			return String.Format( "{0} Bytes", totalBytes );
		}
	}
}