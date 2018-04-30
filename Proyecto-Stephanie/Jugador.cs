using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

/* La clase Jugador representa a un cliente que accede a la aplicación como un jugador de ahorcado.
 * 
 * Programado por:
 * Stephanie Zúñiga Villalobos
 *
 * Fecha de última modificación:
 * 29/Abril/2018
 */

namespace Proyecto_Stephanie
{
    class Jugador
    {
        internal Socket conexion; //Socket para aceptar una conexion
        private NetworkStream socketStream; //Flujo de datos de red
        private Servidor servidor; //Variable tipo Servidor
        private BinaryWriter escritor; //Facilita la escritura en el flujo
        private BinaryReader lector; //Facilita la lectura del flujo
        private bool turno; //Determina si es el turno del jugador de adivinar la palabra
        public bool Turno { get { return turno; } set { turno = value; } } //Método get set para la variable turno
        internal bool subprocesoSuspendido = true; //Verdadero si esta esperando al otro jugador
        private int numero; //Número de jugador
        private bool listo; //indica cuando se inicia o termina una partida
        private bool jugar = true; //indica cuando se juega o se detiene el juego

        //Método constructor que recibe como parámetros un objeto Socket, un servidor y un entero.
        public Jugador(Socket socket, Servidor valorServidor, int numeroJugador)
        {
            //se asignan los parámetros recibidos a las variables locales de la clase
            conexion = socket;
            servidor = valorServidor;
            numero = numeroJugador;

            //Crea objeto newStream para el Socket
            socketStream = new NetworkStream(conexion);

            //Crea flujos para escribir y leer bytes
            escritor = new BinaryWriter(socketStream);
            lector = new BinaryReader(socketStream);

            //Se envía al cliente el núero de jugador
            escritor.Write(numero);
        }//Fin del constructor

        //Método que se encarga de actualizar los puntos del jugador
        public void actualizarPuntos()
        {
            //Verifica el jugador actual y le asigna los puntos correspondientes para mostrarlos en la interfaz del cliente
            if (servidor.JugadorActual == 0)
            {
                escritor.Write("Puntos Jugador Actual.");
                escritor.Write(servidor.Jugador1Puntos);
            }
            else if (servidor.JugadorActual == 1)
            {
                escritor.Write("Puntos Jugador Actual.");
                escritor.Write(servidor.Jugador2Puntos);
            }
        }//Fin del método actualizarPuntos

        //Método que se encarga de actualizar los juegos ganados por el jugador
        public void actualizarGanados()
        {
            //Verifica el jugador actual y le asigna los juegos ganados para ser mostrados en la interfaz del cliente
            if (servidor.JugadorActual == 0)
            {
                escritor.Write("Ganados Jugador Actual.");
                escritor.Write(servidor.Jugador1Ganados);
            }
            else if (servidor.JugadorActual == 1)
            {
                escritor.Write("Ganados Jugador Actual.");
                escritor.Write(servidor.Jugador2Ganados);
            }
        }//Fin del método actualizarGanados

        //Método encargado de inicializar el turno
        public void Inicio()
        {
            //Se encarga de definir el jugador actual
            escritor.Write("Asignar jugador actual.");
            escritor.Write(servidor.JugadorActual);

            //Asigna el turno a cada jugador
            escritor.Write("Asignar turno");
            escritor.Write(Turno);
            escritor.Write(numero);

            //Si el jugador tiene el turno debe esperar a que el otro jugador ingrese una palabra para empezar a adivinar
            if (Turno)
            {
                //Se indica al jugador que espere mientras el otro ingresa una palabra
                escritor.Write("Espere a que el otro jugador ingrese una palabra.");

                //Se suspende el hilo mientras el otro jugador ingresa la palabra para adivinar
                servidor.SuspenderProceso(servidor.JugadorActual);

                //Cuando se desbloquea el hilo se indica al jugador que puede empezar a adivinar la palabra
                escritor.Write("Empieza adivinar.");
            }
            else
            {
                //Si no corresponde al turno el jugador debe ingresar una palabra para que jugador actual la adivine
                servidor.Palabra = lector.ReadString();
                //Se asignan los espacios que corresponden a la palabra mediante el metodo del servidor
                //y se desbloquea el jugador actual para que inicie su turno de adivinar
                servidor.mostrarEspacios();
                servidor.Desbloqueo(servidor.JugadorActual);
            }
            //Se muestran los espacios en la interfaz
            escritor.Write("Mostrar Espacios.");
            escritor.Write(servidor.Espacios);
        }//Fin del método Inicio

        //Permite a los jugadores jugar su turno y recibir los datos del otro jugador para actualizar la interfaz del cliente
        public void Ejecutar()
        {
            //Se maneja la excepción de tipo IOException y en caso de que se genere se finaliza el juego y se indica al usuario
            //Esta excepción se presenta cuando se tiene un error tratando de accesar información mediante el uso de streams
            try
            {
                //Se indica al primer jugador que debe esperar a que ingrese el segundo jugador
                if (numero == 0)
                {
                    escritor.Write("Esperando al otro jugador.");

                    //Espera la notificacion del servidor de que se conecto otro jugador
                    servidor.SuspenderProceso(0);

                    //Indica que se conectó el otro jugador
                    escritor.Write("Se conectó el otro jugador.");

                    //Se asigna el turno de los jugadores
                    servidor.asignarTurno();
                }//fin del if  

                //Se inicia un nuevo ciclo de juego mientras la variable jugar sea true
                while (jugar)
                {
                    JugarTurno(); //Se juega un nuevo turno

                    if (!Turno)//En caso de que no sea el jugador actual se llama al método reiniciar de servidor para preparar las variables para un nuevo turno
                    {
                        servidor.reiniciar();
                        servidor.Desbloqueo(servidor.JugadorEspera); //Se desbloquea el jugador en espera
                    }
                    else if(!servidor.Reiniciado && Turno) //Verifica si se ha reiniciado y si corresponde a jugador actual
                    {
                        servidor.SuspenderProceso(servidor.JugadorActual); //Se suspende el hilo del jugador actual mientras se reinicia para un nuevo turno
                    }
                } //Fin del ciclo while        
            }
            catch (IOException){
                MessageBox.Show("Ha finalizado el juego.");
                Cerrar(); //Se llama al método cerrar para finalizar la partida
            }
        }//Fin del método ejecutar

        //Método que se encarga de manejar la lógica de juego de cada turno
        public void JugarTurno()
        {
            //Se establece listo = false y se cambia hasta que termina el turno
            listo = false;
            if (Turno) //Se desbloquea al jugador en espera desde el hilo de jugador actual
            {
                servidor.Desbloqueo(servidor.JugadorEspera);
            }

            Inicio(); //Se inicializa el juego
            
            //Iniciar el juego en un cliclo que finaliza hasta que se infica que el juego ha terminado mediante la variable lógica listo
            while (!listo)
            {
                if (Turno) //jugador actual
                {
                    //Espera a que haya datos disponibles
                    while (conexion.Available == 0)
                    {
                        Thread.Sleep(1000); //1 segundo
                        if (servidor.desconectado)
                            return;
                    }//fin del while

                    //Recibe la letra ingresada por el jugador
                    char letra = lector.ReadChar();

                    //se inserta la letra en la lista de letras ingresadas en el servidor
                    servidor.LetrasIngresadas += letra;
                    escritor.Write("Letras Ingresadas.");
                    escritor.Write(servidor.LetrasIngresadas);

                    //Se verifica si la palabra contiene la letra que el jugador ingresó y se actualizan los espacios a mostrar en la interfaz del cliente
                    if (servidor.contieneLetra(letra))
                    {
                        escritor.Write("La palabra contiene la letra: " + letra + ".");
                        escritor.Write("Mostrar Espacios.");
                        escritor.Write(servidor.Espacios);
                    }
                    else
                    {
                        //Si no contiene la letra se ingresa en la lista de letras fallidas y se muestra al usuario en la interfaz del cliente
                        servidor.LetrasFallidas +=letra +" ";
                        escritor.Write("Letras Fallidas.");
                        escritor.Write(servidor.LetrasFallidas);
                        escritor.Write("La palabra no contiene la letra: " + letra + ".");
                        actualizarPuntos(); //Se actualizan los puntos del jugador
                    }

                    //Si el juego termino, establece listo = true para salir del juego 
                    if (servidor.JuegoTerminado(numero))
                    {
                        //Se actualizan los juegos ganados por el jugador, las letras ingresads, las letras fallidas y se desactivan los objetos en la interfaz del cliente hasta que inicia un nuevo turno.
                        actualizarGanados();
                        escritor.Write("Letras Ingresadas.");
                        escritor.Write("");
                        escritor.Write("Letras Fallidas.");
                        escritor.Write("");
                        escritor.Write("Desactivar.");
                        listo = true;
                        servidor.Reiniciado = false;
                        servidor.Desbloqueo(servidor.JugadorEspera); //Se bloquea al jugador en espera
                        break;//Abandona el método
                    }//Finaliza el if que verifica si ha terminado el juego

                    servidor.Desbloqueo(servidor.JugadorEspera); //Se desbloquea el jugador en espera
                    servidor.SuspenderProceso(servidor.JugadorActual); //Se suspende el hilo del Jugador actual mientras se actualiza jugador en espera
                }
                else
                {
                    //Se actualizan los espacios a mostrar, los puntos y las letras fallidas
                    escritor.Write("Mostrar Espacios.");
                    escritor.Write(servidor.Espacios);
                    actualizarPuntos();
                    escritor.Write("Letras Fallidas.");
                    escritor.Write(servidor.LetrasFallidas);

                    //Si el juego termino, establece listo = true para salir del juego 
                    if (servidor.JuegoTerminado(numero))
                    {
                        //Se suspende el hilo del Jugador actual mientras se actualiza jugador en espera
                        actualizarGanados();
                        escritor.Write("Letras Ingresadas.");
                        escritor.Write("");
                        escritor.Write("Letras Fallidas.");
                        escritor.Write("");
                        escritor.Write("Desactivar.");
                        listo = true;
                        break; //Abandona el método
                    }//Finaliza el if que verifica si ha terminado el juego

                    servidor.Desbloqueo(servidor.JugadorActual);//Se desbloquea el jugador actual
                    servidor.SuspenderProceso(servidor.JugadorEspera); //Se suspende el hilo del Jugador espera
                }
            }//fin de ciclo while 
        }//Fin del método JugarTurno


        //Método Cerrar se encarga de finalizar el juego y cerrar los sockets, la conexion, el writer y el reader.
        private void Cerrar()
        {
            jugar = false; //Se detiene el juego
            servidor.cerrar(numero); //Método cerrar del servidor que permite finalizar los procesos asociados al jugador en el servidor. 

            //Cierra la conexion de los sockets
            escritor.Close();
            lector.Close();
            socketStream.Close();
            conexion.Close();

        }//Fin del método cerrar

    }//Fin de la clase Jugador
}
