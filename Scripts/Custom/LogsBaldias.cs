using System; 
using System.IO; 
using Server.Spells; 
using Server.Accounting; 
using Server.Network; 
using System.Collections;

namespace Server
{ 

	public class DatosDelUsuario
	{
		public string IP;
		public string Nombre;
		public string Username;


		public DatosDelUsuario ( Mobile from )
		{
			IP = String.Format ("{0}", from.NetState );
			Nombre = from.Name;

			Account cuenta = from.Account as Account;

			Username = cuenta.Username;
		}
	}

	public class LogBaldias 
	{ 


		public static ArrayList InformacionCuentas ;
		
		public static void Initialize() 
		{ 
		    EventSink.AccountLogin += new AccountLoginEventHandler( EventSink_AccountLogin ); 
			EventSink.Connected += new ConnectedEventHandler( EventSink_Connected ); 
			EventSink.Disconnected += new DisconnectedEventHandler( EventSink_Disconnected ); 
			//EventSink.FastWalk += new FastWalkEventHandler( EventSink_FastWalk ); 
			InformacionCuentas = new ArrayList();
		} 

		
		private static void ComprobarUsosCuentas ( Mobile from ) {


			if ( InformacionCuentas == null ) return;

			for (int i=0; i < InformacionCuentas.Count; i++){
				
				DatosDelUsuario quien = InformacionCuentas [i] as DatosDelUsuario;
		
				if ( quien.IP == String.Format ("{0}", from.NetState) ){

					Account cuenta = from.Account as Account;

					if ( quien.Username != cuenta.Username ){

			       			WriteToLog ( String.Format( "La CUENTA '{0}' (Player = {1}) con ip {2} , ha entrado ahora con la CUENTA '{3}' (Player = {4}) con ip {5}" , quien.Username, quien.Nombre , quien.IP, cuenta.Username, from.Name, from.NetState ) );
					}
				}
			}

		}

      		private static void EventSink_AccountLogin( AccountLoginEventArgs args )
      		{ 
       			WriteToLog( String.Format( "Conectando cuenta: ip {0}, cuenta {1}" , args.State, args.Username ) );

      		} 

      		private static void EventSink_Connected( ConnectedEventArgs args ) 
      		{ 
        		Mobile mobile = args.Mobile;
			Account cuenta = mobile.Account as Account;

			DatosDelUsuario datos = new DatosDelUsuario ( mobile );

			InformacionCuentas.Add ( datos ) ;

			ComprobarUsosCuentas ( mobile );

        		WriteToLog( String.Format( "Conectado: ip {0}, cuenta {1}, player {2}" , args.Mobile.NetState, cuenta.Username, mobile.Name ) ); 

     		} 

     		private static void EventSink_Disconnected( DisconnectedEventArgs args ) 
      		{ 
        		Mobile mobile = args.Mobile;
			Account cuenta = mobile.Account as Account;

        		WriteToLog( String.Format( "Desconectado: ip {0}, cuenta {1}, player {2}" , args.Mobile.NetState, cuenta.Username, mobile.Name ) ); 
      		} 

      		private static void EventSink_FastWalk( FastWalkEventArgs args )
      		{ 
                	//NetState netstate = args.NetState; 
                	//WriteToLog( String.Format( FastWalkMsg, args.NetState ) ); 
      		} 
   

     	 	private static void WriteToLog( String text ) 
      		{ 
      			String LogFile = Path.Combine( "Logs", String.Format( "{0}.Players.log", DateTime.Now.ToLongDateString() ) ); 
      			using( StreamWriter sw = new StreamWriter( LogFile, true ) ) 
      			{ 
      				DateTime datetime = DateTime.Now; 
      				sw.WriteLine(datetime.ToString() + ": " + text ); 
      			}

    		} 


     	 	private static void WriteToLogCuentas( String text ) 
      		{ 
      			String LogCuentas = Path.Combine( "Logs", String.Format( "{0}.Cuentas.log", DateTime.Now.ToLongDateString() ) ); 
      			using( StreamWriter sw = new StreamWriter( LogCuentas, true ) ) 
      			{ 
      				DateTime datetime = DateTime.Now; 
      				sw.WriteLine(datetime.ToString() + ": " + text ); 
      			} 
		}
   	} 
} 
