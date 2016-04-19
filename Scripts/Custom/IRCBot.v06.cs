#region Lista de Cambios
/* [20080303] Ender - v0.6 Adaptado a la 2.0
 */
#endregion

using System;
using Server;
using System.IO;
using System.Net;
using System.Text;
using Server.Gumps;
using Server.Mobiles;
using Server.Network;
using System.Threading;
using Server.Accounting;
using System.Net.Sockets;
using System.Collections;
using Server.Items;
using Server.Guilds;
using Server.Commands;

namespace Server.Misc
{
    public class IRCBot
    {
        #region Configuración
        /* ShardBOT Version 1.0 By RunUOScripts.com
         *
         * You can configure the bot using the variables
         * below, I tried my best to make it extremely
         * simple to configure. If you have any problems
         * you can get support at the RunUOScripts.com
         * website. We have dedicated support forums for
         * all scripts that are released by us.
         *
         * We hope that you have a lot of fun with this
         * bot as that is what it was designed for. We
         * hope that you get a lot of use out of it as
         * we tried to program as many features into it
         * as we thought was necessary. If there is
         * something that you would like to see added
         * please let us know in the forums!
         *
         * Visit us at http://www.runuoscripts.com */

        // You can turn the bot on and off using this variable
        private static bool m_IRCScriptEnabled = true;

        // Would you like the bot to announce in IRC people when they login?
        private static bool m_AnnounceLogins = false;

        // Would you like the bot to announce in IRC people when they logout?
        private static bool m_AnnounceLogouts = false;

        // Would you like the bot to announce new players in IRC?
        private static bool m_AnnounceNewPlayers = false;

        // Would you like the bot to announce deaths in IRC?
        private static bool m_AnnounceDeaths = true;

        // Would you like the bot to filter profane language?
        private static bool m_FilterProfanity = false;

        // Would you like the bot to send staff text to IRC?
        private static bool m_IRCOutput = true;

        //Nivel necesario para enviar mensajes al IRC
        private static readonly AccessLevel m_MinAccessLevel = AccessLevel.GameMaster;

        // Would you like the bot to send text players in-game?
        private static bool m_GlobalOutput = false;

        // Would you like to allow dead people to use the public chat?
        private static bool m_DeadPeopleSpeak = true;

        // What IRC server would you like the bot to connect to?
        private static string m_Server = "irc.irc-hispano.org";

        // What is the port of that IRC server?
        private static int m_Port = 6667;

        // What channel would you like the bot to reside in?
        public static string m_Channel = "#tierras_baldias";

        // Canal Staff
        public static string m_Channel_Staff = "#tierras_staff";

        // What would you like the bot to call itself?
        private static string m_BotName = "Tierras_Baldias";

        // Contraseña del Bot
        private static string m_Pass = "m3mvjr";

        // What would you like the players to type before their message?
        private static string m_ChatSyntax = "irc:"; // Players like the period.
        // Example Syntax Usage: . Hello world!

        // What would you like your players to type to open the chat window?
        private static string m_ChatWindowCmd = "ircg";

        // What would you like your players to type to open the chat window config?
        private static string m_ChatConfigWindowCmd = "ircc";

        // What would you like the in-game chat channel to be called?
        private static string m_ChatChannel = "Tierras Baldias";

        // What is your shard's name? Used for misc notices.
        private static string m_ShardName = "Tierras Baldias";

        // We do not want to pass on profanity to IRC, now do we? Let's bleep it out. Add some more words if you care to.
        private static string[] m_ProfanityCheck = { "jigaboo", "chigaboo", "wop", "kyke", "kike", "tit", "spic", "prick", "piss", "rofls",
               "lesbo", "felatio", "dyke", "dildo", "chinc", "chink", "cunnilingus", "cum", "lezbo",
               "cocksucker", "cock", "clitoris", "clit", "gay", "hitler", "penis", "nigga", "nigger",
               "klit", "kunt", "jiz", "jism", "jerkoff", "jackoff", "goddamn", "fag", "blowjob", "woot",
               "bitch", "asshole", "dick", "pussy", "snatch", "cunt", "twat", "shit", "fuck", "w00t",
               "shit", "lol", "l0l", "1o1", "l.ol", "lo.l", ". lol", ".lol", "lol.", "rofl", "lmao",
               "omg", "lmfao", "roflmfao", "brb", "stfu", "pwnd", "omg", "afk", "1337", "fuck", "n00b",
               "tyvm", "cya", "dude", "d00d", "kewl", "kool", "k00l", "coolies", "c00lies", "atm", "gtg",
               "l o l", "l 0 l", "1 o 1", "l.o l", "l o.l", ". l o l", ".l o l", "l o l.", "brb" }; // I hate lol

        private static string[] m_InChannelIRC = { " <ShardBOT> version 1.0 has entered the room!",
             " In-game commands are as follows (type them without the quotes):",
             " \"" + m_ChatConfigWindowCmd + "\"" + " Opens the chat window configuration in-game. USAGE: " + "\"" + m_ChatConfigWindowCmd + "\"" + "",
             " \"" + m_ChatWindowCmd + "\"" + " Enables and disables the in-game chat window. USAGE: " + "\"" + m_ChatWindowCmd + "\"" + "" };

        /* CAUTION: DO NOT MODIFY BELOW THIS LINE UNLESS YOU KNOW WHAT YOU ARE DOING
         * FAILURE TO COMPLY WILL RESULT IN A LACK OF AVAILABLE SUPPORT FOR US TO
         * ASSIST YOU WITH ANY PROBLEMS YOU MAY HAVE WITH OUR SCRIPT. */
        #endregion

        private static TcpClient irc;
        private static NetworkStream stream;
        private static string inputLine;
        private static StreamReader irc_reader;
        private static StreamWriter irc_writer;

        private static Thread m_ReadThread;
        private static Thread m_ConnectThread;
        private static bool m_IsAlive = false;
        private static string m_ChatHistory = "¡Bienvenido a " + m_ShardName + "!";
        private static int m_ChatHistoryReset = 0;
        private static ArrayList m_OnlineUsers = new ArrayList();
        private static int m_SeenLogin = 0;
        private static int m_SeenDie = 0;
        private static int m_SeenNew = 0;
        private static bool m_Command = false;

        public class RefrescarIRC
        {
            public static void Initialize()
            {
                CommandSystem.Register("re", AccessLevel.GameMaster, new CommandEventHandler(RefrescarIRC_OnCommand));
            }

            [Usage("re")]
            [Description("Refresca la pantalla del IRC en caso de tenerla abierta.")]
            public static void RefrescarIRC_OnCommand(CommandEventArgs e)
            {
                Account acct = (Account)e.Mobile.Account;
                bool ircg = Convert.ToBoolean(acct.GetTag("ircg"));
                if (!ircg)
                {
                    e.Mobile.CloseGump(typeof(IRCGump));
                    e.Mobile.SendGump(new IRCGump(e.Mobile));
                }
                else
                {
                    e.Mobile.SendMessage("No tienes ninguna ventana abierta del IRC");
                }
            }
        }

        public static bool AnnounceDeaths()
        {
            m_AnnounceDeaths = !m_AnnounceDeaths;
            return m_AnnounceDeaths;
        }
        public static void Initialize()
        {
            if (m_IRCScriptEnabled)
            {
                EventSink.Speech += new SpeechEventHandler(IRCBotProcess);
                EventSink.Login += new LoginEventHandler(EventSink_Login);
                EventSink.Disconnected += new DisconnectedEventHandler(EventSink_Disconnected);
                EventSink.WorldSave += new WorldSaveEventHandler(EventSink_OnSave);

                if (m_AnnounceNewPlayers)
                    EventSink.CharacterCreated += new CharacterCreatedEventHandler(EventSink_CharacterCreated);

                if (m_AnnounceDeaths)
                    EventSink.PlayerDeath += new PlayerDeathEventHandler(EventSink_PlayerDeath);

                m_ConnectThread = new Thread(new ThreadStart(ConnectIRC));
                m_ConnectThread.Start();
            }
        }

        private static void IRCBotProcess(SpeechEventArgs args)
        {
            try
            {
                if (args.Mobile == null)
                {
                    return;
                }
                string m_Message = args.Speech;
                Mobile m_Mobile = args.Mobile;
                bool m_IsAlive = m_Mobile.Alive;
                Account acct = (Account)m_Mobile.Account;
                bool ircg = Convert.ToBoolean(acct.GetTag("ircg"));

                if (m_Mobile.AccessLevel >= m_MinAccessLevel)
                {
                    if (m_DeadPeopleSpeak)
                        m_IsAlive = true;

                    if (m_IsAlive)
                    {
                        if (m_FilterProfanity)
                        {
                            m_Message = FilterProfanity(m_Message);
                        }

                        try
                        {
                            if (m_Message.StartsWith(m_ChatSyntax))
                            {
                                if (m_Message.StartsWith(m_ChatSyntax + " ban"))
                                {
                                    m_Command = true;
                                    m_Message = String.Format("mode {0} +b {1}", m_Channel, m_Message.Substring(m_ChatSyntax.Length + 5));
                                    m_ChatHistory = "[" + m_ChatChannel + "] " + m_Message + "\r\n" + m_ChatHistory;
                                }
                                else if (m_Message.StartsWith(m_ChatSyntax + " unban"))
                                {
                                    m_Command = true;
                                    m_Message = String.Format("mode {0} -b {1}", m_Channel, m_Message.Substring(m_ChatSyntax.Length + 7));
                                    m_ChatHistory = "[" + m_ChatChannel + "] " + m_Message + "\r\n" + m_ChatHistory;
                                }
                                else
                                {
                                    m_Command = false;
                                    m_Message = String.Format("4{0}:1{1}", m_Mobile.Name, m_Message.Substring(m_ChatSyntax.Length));
                                    m_ChatHistory = "[" + m_ChatChannel + "] " + m_Message + "\r\n" + m_ChatHistory;
                                }

                                if (m_GlobalOutput)
                                {
                                    SendPublicMsg(m_Message);
                                }

                                if (m_IRCOutput)
                                {
                                    SendToIRC(m_Message, m_Command, m_Channel);
                                    if (!ircg)
                                    {
                                        m_Mobile.CloseGump(typeof(IRCGump));
                                        m_Mobile.SendGump(new IRCGump(m_Mobile));
                                    }
                                }
                            }
                        }
                        catch (Exception error)
                        {
                            ReportException(Convert.ToString(error));
                        }
                    }
                }
            }
            catch (Exception error)
            {
                ReportException(Convert.ToString(error));
            }
        }

        private static void WatchIRC()
        {
            //do
            //{
            //Thread.Sleep(1000);

            try
            {
                if (inputLine != null)
                {
                    char[] m_Separator = { ' ' };
                    string[] m_MessageParts = inputLine.Split(m_Separator);

                    int m_NameLength = m_MessageParts[0].IndexOf("~");
                    m_NameLength--;

                    if (m_NameLength <= 0)
                        m_NameLength = m_MessageParts[0].IndexOf("!");

                    String m_MessageSender = null;

                    if (m_NameLength > 0)
                        m_MessageSender = m_MessageParts[0].Substring(0, m_NameLength);

                    switch (m_MessageParts[1])
                    {
                        case "PRIVMSG":
                            String m_IRCMessage = null;
                            switch (m_MessageParts[3])
                            {
                                case ":ACTION":
                                    for (int i = 4; i < m_MessageParts.Length; i++)
                                        m_IRCMessage += " " + m_MessageParts[i];
                                    if (m_FilterProfanity)
                                        m_IRCMessage = FilterProfanity(m_IRCMessage);

                                    m_IRCMessage = m_IRCMessage.Replace("\r", "");
                                    m_IRCMessage = m_IRCMessage.Replace("\n", "");

                                    m_ChatHistory = "[" + m_ChatChannel + "] * " + m_MessageSender.Substring(1) + m_IRCMessage + "\r\n" + m_ChatHistory;

                                    if (m_GlobalOutput)
                                        SendPublicMsg("* " + m_MessageSender.Substring(1) + m_IRCMessage);
                                    break;
                                default:
                                    for (int i = 3; i < m_MessageParts.Length; i++)
                                        m_IRCMessage += " " + m_MessageParts[i];

                                    if (m_FilterProfanity)
                                        m_IRCMessage = FilterProfanity(m_IRCMessage);

                                    m_IRCMessage = m_IRCMessage.Replace("\r", "");
                                    m_IRCMessage = m_IRCMessage.Replace("\n", "");

                                    m_ChatHistory = "[" + m_ChatChannel + "] " + m_MessageSender.Substring(1) + ": " + m_IRCMessage.Substring(2) + "\r\n" + m_ChatHistory;

                                    if (m_GlobalOutput)
                                        SendPublicMsg(m_MessageSender.Substring(1) + ": " + m_IRCMessage.Substring(2));
                                    break;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception error)
            {
                ReportException(Convert.ToString(error));
            }

            // } while (true);
        }

        private static void ConnectIRC()
        {
            Thread.Sleep(20000);
            Console.WriteLine("Tierras Baldias : Conectando a " + m_Server + " en el puerto " + m_Port);
            try
            {
                irc = new TcpClient(m_Server, m_Port);
                stream = irc.GetStream();
                irc_reader = new StreamReader(stream);
                irc_writer = new StreamWriter(stream);

                Console.WriteLine("Tierras Baldias : Conectado a " + m_Server);

                irc_writer.WriteLine("USER Baldias 0 * : Bot_Baldias");
                irc_writer.Flush();
                irc_writer.WriteLine("NICK " + m_BotName + "!" + m_Pass);
                irc_writer.Flush();
                Console.WriteLine("Tierras Bot: identificacion correcta");

                m_ReadThread = new Thread(new ThreadStart(WatchIRC));
                m_ReadThread.Start();

                while (true)
                {
                    Thread.Sleep(100);
                    while ((inputLine = irc_reader.ReadLine()) != null)
                    {

                        if (inputLine.StartsWith("PING :"))
                        {
                            string Pong = inputLine.Substring(6);
                            irc_writer.WriteLine("PONG :" + Pong);
                            irc_writer.Flush();
                            irc_writer.WriteLine("JOIN " + m_Channel);
                            irc_writer.Flush();
                            irc_writer.WriteLine("JOIN " + m_Channel_Staff);
                            irc_writer.Flush();
                        }
                        WatchIRC();
                    }
                    irc_writer.Close();
                    irc_reader.Close();
                    irc.Close();
                }
            }
            catch (Exception error)
            {
                Console.WriteLine("Tierras Baldias : conexion perdida...");
                Thread.Sleep(5000);
                ConnectIRC();
            }
        }

        private static string FilterProfanity(string str)
        {
            for (int i = 0; i < m_ProfanityCheck.Length; i++)
            {
                string m_ReplaceWith = "*";

                for (int ii = 1; ii < m_ProfanityCheck[i].Length; ii++)
                {
                    m_ReplaceWith += "*";
                }

                str = str.Replace(m_ProfanityCheck[i], m_ReplaceWith);
            }

            return str;
        }

        private static void SendPublicMsg(string str)
        {
            for (int i = 0; i < m_OnlineUsers.Count; i++)
            {
                Mobile m_Mobile = ((Mobile)m_OnlineUsers[i]);
                Account acct = (Account)m_Mobile.Account;
                bool ircg = Convert.ToBoolean(acct.GetTag("ircg"));

                if (ircg)
                {
                    m_Mobile.SendMessage("[" + m_ChatChannel + "] " + str);
                }
                else
                {
                    m_Mobile.CloseGump(typeof(IRCGump));
                    m_Mobile.SendGump(new IRCGump(m_Mobile));
                }
            }

            m_ChatHistoryReset += m_ChatHistory.Length;

            if (m_ChatHistoryReset == 800)
            {
                m_ChatHistory = m_ChatHistory.Substring(800);
                m_ChatHistoryReset = 0;
            }
        }

        private static void EventSink_Login(LoginEventArgs args)
        {
            Account acct = (Account)args.Mobile.Account;
            Mobile m_Mobile = args.Mobile;

            acct.SetTag("ircg", "false");

            /*
            args.Mobile.SendGump( new IRCGump( m_Mobile ) );

            m_Mobile.SendMessage( "To participate in public chat please use the chat command as follows: " + m_ChatSyntax + "Hello world!" );
            m_Mobile.SendMessage( "To configure your chat window please type the following command: " + m_ChatWindowCmd );
            */

            m_OnlineUsers.Add(m_Mobile);

            m_SeenLogin++;

            if (m_AnnounceLogins)
            {
                try
                {
                    string Han = null;
                    string UserUsers = null;
                    string ConCon = null;

                    if (m_OnlineUsers.Count == 1)
                    {
                        Han = "ha";
                        UserUsers = "usuario";
                        ConCon = "conectado";
                    }
                    else
                    {
                        Han = "han";
                        UserUsers = "usuarios";
                        ConCon = "conectados";
                    }

                    SendToIRC(" <" + m_Mobile.Name + "> ha entrado!. Ahora hay " + m_OnlineUsers.Count + " " + UserUsers + " " + ConCon + ".");
                    SendToIRC(" Hasta ahora se " + Han + " conectado " + m_SeenLogin + " " + UserUsers + ".");
                }
                catch (Exception error)
                {
                    ReportException(Convert.ToString(error));
                }
            }
        }

        private static void EventSink_Disconnected(DisconnectedEventArgs args)
        {
            Mobile m_Mobile = args.Mobile;
            m_OnlineUsers.Remove(m_Mobile);
            m_OnlineUsers.TrimToSize();

            if (m_AnnounceLogouts)
            {
                try
                {
                    string Han = null;
                    string UserUsers = null;
                    string ConCon = null;

                    if (m_OnlineUsers.Count == 0)
                    {
                        SendToIRC(" <" + m_Mobile.Name + "> ha salido!. Ahora el Shard esta vacio.");
                    }
                    else
                    {
                        if (m_OnlineUsers.Count == 1)
                        {
                            UserUsers = "usuario";
                            ConCon = "conectado";
                        }
                        else
                        {
                            UserUsers = "usuarios";
                            ConCon = "conectados";
                        }
                        SendToIRC(" <" + m_Mobile.Name + "> ha salido!. Ahora hay " + m_OnlineUsers.Count + " " + UserUsers + " " + ConCon + ".");
                    }

                }
                catch (Exception error)
                {
                    ReportException(Convert.ToString(error));
                }
            }
        }

        private static void EventSink_CharacterCreated(CharacterCreatedEventArgs args)
        {
            Mobile m_Mobile = args.Mobile;

            m_SeenNew++;

            try
            {
                SendToIRC(" Everyone please welcome <" + m_Mobile.Name + "> to " + m_ShardName + "!");
                SendToIRC(" Since I have been running I have seen " + m_SeenNew + " new characters login.");
                m_ChatHistory = "* Everyone please welcome [" + m_Mobile.Name + "] to " + m_ShardName + "! *" + m_ChatHistory;
                SendPublicMsg("* Everyone please welcome [" + m_Mobile.Name + "] to " + m_ShardName + "! *");
            }
            catch (Exception error)
            {
                ReportException(Convert.ToString(error));
            }
        }

        public static void EventSink_PlayerDeath(PlayerDeathEventArgs e)
        {
            try
            {
                Mobile m_Mobile = e.Mobile;
                Mobile m_MobileKiller = m_Mobile.LastKiller;

                if (m_MobileKiller is BaseCreature)
                {
                    return;
                }
                /*TODO modificar para añadir el atake a un equipo contrario en el ctf*/

                /* equipoCTF ft = piedraCTF.FindTeamFor( m_Mobile );
 
                 if ( ft != null )
                  return;
   
                 if (!m_AnnounceDeaths)
                  return;*/
                /*Fin modificacion*/

                m_SeenDie++;

                if (m_Mobile == m_MobileKiller)
                {
                    SendToIRC("12<" + m_Mobile.Name + ">1 se ha matado a si mismo");
                }
                else
                {
                    string Mtitle = "";
                    string Ktitle = "";
                    Guild gM = m_Mobile.Guild as Guild;
                    if (gM != null)
                    {
                        Mtitle = "[" + gM.Abbreviation + "]";
                    }

                    Guild gK = m_MobileKiller.Guild as Guild;
                    if (gK != null)
                    {
                        Ktitle = "[" + gK.Abbreviation + "]";
                    }

                    //     SendToIRC ( " <" + m_Mobile.Name + "> ha muerto" );
                    SendToIRC(String.Format("{0}<" + m_MobileKiller.Name + "3{2}>1 ha matado a {1}<" + m_Mobile.Name + "3{3}>", m_MobileKiller.Kills >= 5 ? 4 : 2, m_Mobile.Kills >= 5 ? 4 : 2, gK != null ? Ktitle : "", gM != null ? Mtitle : ""));
                }

                //    SendToIRC ( " Since I have been running I have seen " + m_SeenDie + " deaths." );
            }
            catch (Exception error)
            {
                ReportException(Convert.ToString(error));
            }
        }

        public static void EventSink_OnSave(WorldSaveEventArgs e)
        {
            SendToIRC("3Guardando ...");
        }

        public static void SendToIRC(string str)
        {
            SendToIRC(str, false, m_Channel);
        }

        public static void SendToIRC(string str, bool com, string chan)
        {
            try
            {
                if (com)
                {
                    irc_writer.WriteLine(str);
                    irc_writer.Flush();
                }
                else
                {
                    irc_writer.WriteLine("PRIVMSG " + chan + " :" + str);
                    irc_writer.Flush();
                }
                Console.WriteLine("Tierras Baldias: Escrito al IRC: " + str);
            }
            catch (Exception error)
            {
                Console.WriteLine("Tierras Baldias: No puedo enviar el mensaje ¡¡¡ ERROR !!! ");
            }
        }

        public static void ReportException(string error)
        {
            // World.Broadcast( 0x124, true, "Ha ocurrido una Excepción IRCBot: {0}", error );
            Console.WriteLine("Ha ocurrido una Excepción IRCBot: {0}", error);
        }

        public class IRCConfigGump : Gump
        {
            public static void Initialize()
            {
                EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
            }

            public static void EventSink_Speech(SpeechEventArgs args)
            {
                try
                {
                    if (args.Speech.ToLower().IndexOf("ircc") >= 0)
                    {
                        Mobile m = args.Mobile;
                        if (m.AccessLevel >= m_MinAccessLevel)
                        {

                            m.CloseGump(typeof(IRCConfigGump));
                            m.SendGump(new IRCConfigGump(m));
                        }
                    }
                }
                catch (Exception error)
                {
                    World.Broadcast(0x124, true, "Ha ocurrido una Excepción IRCBot: {0}", error);
                    Console.WriteLine("Ha ocurrido una Excepción IRCBot: {0}", error);
                }
            }

            public IRCConfigGump(Mobile m)
                : base(0, 0)
            {
                Closable = true;
                Disposable = true;
                Dragable = true;
                Resizable = false;
                AddPage(0);
                AddBackground(72, 80, 201, 244, 9200);
                AddBackground(86, 202, 172, 88, 9200);
                AddBackground(86, 107, 173, 89, 9200);
                AddBackground(175, 152, 75, 20, 9350);
                AddLabel(101, 84, 75, @"Chat Configuration");
                AddBackground(94, 152, 75, 20, 9350);
                AddTextEntry(180, 152, 67, 20, 0, (int)Buttons.TEYLoc, @"");
                AddLabel(97, 129, 35, @"X Position");
                AddTextEntry(99, 152, 67, 20, 0, (int)Buttons.TEXLoc, @"");
                AddBackground(94, 245, 75, 20, 9350);
                AddLabel(178, 129, 55, @"Y Position");
                AddLabel(109, 222, 44, @"Height");
                AddBackground(175, 245, 75, 20, 9350);
                AddTextEntry(99, 245, 67, 20, 0, (int)Buttons.TEHeight, @"");
                AddLabel(190, 222, 62, @"Width");
                AddTextEntry(180, 245, 67, 20, 0, (int)Buttons.TEWidth, @"");
                AddLabel(103, 111, 90, @"Chat Window Position");
                AddLabel(115, 205, 90, @"Chat Window Size");
                AddButton(192, 295, 247, 248, (int)Buttons.Okay, GumpButtonType.Reply, 0);
                AddButton(124, 295, 239, 240, (int)Buttons.Apply, GumpButtonType.Reply, 0);
                AddLabel(102, 172, 35, @"Default 15");
                AddLabel(183, 172, 55, @"Default 30");
                AddLabel(102, 265, 44, @"Default 74");
                AddLabel(179, 265, 62, @"Default 600");
            }

            public enum Buttons
            {
                TEYLoc,
                TEXLoc,
                TEHeight,
                TEWidth,
                Okay,
                Apply,
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                Mobile from = sender.Mobile;
                Account acct = (Account)from.Account;

                switch (info.ButtonID)
                {
                    case (int)Buttons.Okay: //Okay
                        {
                            from.SendMessage("You pressed Okay {Save,Close}");
                            TextRelay teH = info.GetTextEntry((int)Buttons.TEHeight);
                            TextRelay teW = info.GetTextEntry((int)Buttons.TEWidth);
                            TextRelay teX = info.GetTextEntry((int)Buttons.TEXLoc);
                            TextRelay teY = info.GetTextEntry((int)Buttons.TEYLoc);
                            acct.SetTag("irch", teH.Text.Trim());
                            acct.SetTag("ircw", teW.Text.Trim());
                            acct.SetTag("ircx", teX.Text.Trim());
                            acct.SetTag("ircy", teY.Text.Trim());
                            break;
                        }

                    case (int)Buttons.Apply: //Apply
                        {
                            from.SendMessage("You pressed Apply {Save,Closes,ReOpen}");
                            TextRelay teH = info.GetTextEntry((int)Buttons.TEHeight);
                            TextRelay teW = info.GetTextEntry((int)Buttons.TEWidth);
                            TextRelay teX = info.GetTextEntry((int)Buttons.TEXLoc);
                            TextRelay teY = info.GetTextEntry((int)Buttons.TEYLoc);
                            acct.SetTag("irch", teH.Text.Trim());
                            acct.SetTag("ircw", teW.Text.Trim());
                            acct.SetTag("ircx", teX.Text.Trim());
                            acct.SetTag("ircy", teY.Text.Trim());
                            from.SendGump(new IRCConfigGump(from));
                            from.CloseGump(typeof(IRCGump));
                            from.SendGump(new IRCGump(from));
                            break;
                        }

                    default:
                        {

                            break;
                        }
                }
            }

        }

        public class IRCGump : Gump
        {
            public static void Initialize()
            {
                EventSink.Speech += new SpeechEventHandler(EventSink_Speech);
            }

            public static void EventSink_Speech(SpeechEventArgs args)
            {
                try
                {
                    if (args.Speech.ToLower().IndexOf(m_ChatWindowCmd) >= 0)
                    {
                        Mobile m = args.Mobile;
                        Account acct = (Account)m.Account;
                        bool ircg = Convert.ToBoolean(acct.GetTag("ircg"));

                        if (ircg)
                        {
                            acct.SetTag("ircg", "false");
                            m.SendGump(new IRCGump(m));
                        }
                        else
                        {
                            acct.SetTag("ircg", "true");
                            m.CloseGump(typeof(IRCGump));
                        }
                    }
                }
                catch (Exception error)
                {
                    World.Broadcast(0x124, true, "Ha ocurrido una Excepción IRCBot: {0}", error);
                    Console.WriteLine("Ha ocurrido una Excepción IRCBot: {0}", error);
                }

                if (args.Speech.ToLower().StartsWith("ircx"))
                {
                    Mobile m_Mobile = args.Mobile;
                    Account acct = (Account)m_Mobile.Account;

                    try
                    {
                        acct.SetTag("ircx", args.Speech.Substring(5));
                    }
                    catch
                    {
                        acct.SetTag("ircx", "0");
                    }
                }

                if (args.Speech.ToLower().StartsWith("ircy"))
                {
                    Mobile m_Mobile = args.Mobile;
                    Account acct = (Account)m_Mobile.Account;

                    try
                    {
                        acct.SetTag("ircy", args.Speech.Substring(5));
                    }
                    catch
                    {
                        acct.SetTag("ircy", "0");
                    }
                }

                if (args.Speech.ToLower().StartsWith("ircw"))
                {
                    Mobile m_Mobile = args.Mobile;
                    Account acct = (Account)m_Mobile.Account;

                    try
                    {
                        acct.SetTag("ircw", args.Speech.Substring(5));
                    }
                    catch
                    {
                        acct.SetTag("ircw", "0");
                    }
                }

                if (args.Speech.ToLower().StartsWith("irch"))
                {
                    Mobile m_Mobile = args.Mobile;
                    Account acct = (Account)m_Mobile.Account;

                    try
                    {
                        acct.SetTag("irch", args.Speech.Substring(5));
                    }
                    catch
                    {
                        acct.SetTag("irch", "0");
                    }
                }

                if (args.Speech.ToLower().StartsWith("ircv"))
                {
                    Mobile m_Mobile = args.Mobile;
                    Account acct = (Account)m_Mobile.Account;

                    try
                    {
                        acct.SetTag("ircv", args.Speech.Substring(5));
                    }
                    catch
                    {
                        acct.SetTag("ircv", "0");
                    }
                }
            }

            public IRCGump(Mobile m)
                : base(15, 30)
            {
                m.CloseGump(typeof(IRCGump));

                Account acct = (Account)m.Account;

                int ircx = 0;
                int ircy = 0;
                int ircw = 0;
                int irch = 0;
                int ircv = 0;

                try
                {
                    ircx = Convert.ToInt32(acct.GetTag("ircx"));
                }
                catch
                {
                    ircx = 0;
                    acct.SetTag("ircx", "0");
                }

                try
                {
                    ircy = Convert.ToInt32(acct.GetTag("ircy"));
                }
                catch
                {
                    ircy = 0;
                    acct.SetTag("ircy", "0");
                }

                try
                {
                    ircw = Convert.ToInt32(acct.GetTag("ircw"));
                }
                catch
                {
                    ircw = 0;
                    acct.SetTag("ircw", "0");
                }

                try
                {
                    irch = Convert.ToInt32(acct.GetTag("irch"));
                }
                catch
                {
                    irch = 0;
                    acct.SetTag("irch", "0");
                }

                try
                {
                    ircv = Convert.ToInt32(acct.GetTag("ircv"));
                }
                catch
                {
                    irch = 0;
                    acct.SetTag("ircv", "0");
                }

                if (irch <= 0)
                    irch = 74;

                if (ircw <= 0)
                    ircw = 600;

                if (ircy <= 0)
                    ircy = 30;

                if (ircx <= 0)
                    ircx = 15;

                if (ircv <= 0)
                    ircv = 799;

                if (IRCBot.m_ChatHistory.Length <= ircv)
                    ircv = IRCBot.m_ChatHistory.Length;

                bool value = DisplayMessage(m);
                Dragable = false;
                Resizable = false;
                X = ircx;
                Y = ircy;
                AddPage(0);
                AddAlphaRegion(0, 0, ircw, irch);
                AddHtml(0, 0, ircw, irch, IRCBot.m_ChatHistory.Substring(0, ircv), false, true);
            }

            private static bool DisplayMessage(Mobile m)
            {
                Account acct = (Account)m.Account;
                bool ircg = Convert.ToBoolean(acct.GetTag("ircg"));
                if (!ircg)
                    return false;
                return true;
            }
        }
    }
}