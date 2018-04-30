using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;

/* Clase correspondiente al Form que controla la interfaz gráfica del servidor y la usabilidad y funcionalidad del mismo,
 * contiene un label que indica la cantidad de jugadores activos. Se requieren dos jugadores para poder iniciar el juego. 
 * Además, en caso de que todos los jugadores abandonen el juego se cierra el servidor y se debe reiniciar para jugar de nuevo.
 * 
 * Programado por:
 * Stephanie Zúñiga Villalobos
 *
 * Fecha de última modificación:
 * 29/Abril/2018
 */

namespace Proyecto_Stephanie
{
    public partial class Servidor : Form
    {
        //Método constructor del Servidor, recibe como parámetro un form de tipo principal
        public Servidor(Principal prin)
        {
            InitializeComponent();
            principal = prin; //Se asigna el parámetro recibido a la variables local principal
            changeLabel2(); //Se asigna el label que indica la cantidad de jugadores activos
        }

        private Jugador[] jugadores; //Arreglo de jugadores
        private Thread[] subprocesosJugadores; //Subprocesos para la interaccion con los clientes
        private Principal principal; //Variable del tipo Principal
        private TcpListener oyente; //Escucha en espera de la conexion del cliente
        private int jugadorActual; //Jugador que adivina
        private int jugadorEspera; //Jugador que ingresa la palabra y espera
        private Thread obtenerJugadores; //Subproceso para adquirir las conexiones de los clientes

        private string palabra; //Palabra por adivinar
        public string Palabra //Método get y set de palabra
        {
            get { return palabra; }
            set { palabra = value; }
        }

        private string espacios; //Espacios que corresponden a la palabra a adivinar
        public string Espacios //Método get y set de espacios
        {
            get { return espacios; }
            set
            {
                espacios = value;
            }
        }

        private string letrasIngresadas = ""; //Lista de letras que el jugador actual ha ingresado
        public string LetrasIngresadas //Método get y set de letrasIngresadas
        {
            get { return letrasIngresadas; }
            set { letrasIngresadas = value; }
        }

        private string letrasFallidas = ""; //Lista de letras fallidas que el jugador actual ha ingresado
        public string LetrasFallidas //Método get y set de letrasFallidas
        {
            get { return letrasFallidas; }
            set { letrasFallidas = value; }
        }

        private int jugador1Puntos = 0; //Puntos del jugador 1, se inicializa en cero
        public int Jugador1Puntos //Método get y set de jugador1Puntos
        {
            get { return jugador1Puntos; }
            set
            {
                jugador1Puntos = value;
            }
        }

        private int jugador2Puntos = 0; //Puntos del jugador 2, se inicializa en cero
        public int Jugador2Puntos //Método get y set de jugador2Puntos
        {
            get { return jugador2Puntos; }
            set
            {
                jugador2Puntos = value;
            }
        }

        private int jugador1Ganados = 0; //Juegos ganados de jugador 1, se inicializa en cero
        public int Jugador1Ganados //Método get y set de jugador1Ganados
        {
            get { return jugador1Ganados; }
            set
            {
                jugador1Ganados = value;
            }
        }

        private int jugador2Ganados = 0; //Juegos ganados de jugador 2, se inicializa en cero
        public int Jugador2Ganados //Método get y set de jugador2Ganados
        {
            get { return jugador2Ganados; }
            set
            {
                jugador2Ganados = value;
            }
        }

        private bool reiniciado = false; //Variable lógica que determina si se ha reiniciado o no el juego para un nuevo turno
        public bool Reiniciado //Método get y set de Reiniciado
        { 
            get { return reiniciado; } 
            set { reiniciado = value; } 
        }

        public int JugadorActual //Método get y set de JugadorActual
        {
            get { return jugadorActual; }
            set { jugadorActual = value; }
        }

        public int JugadorEspera //Método get y set de JugadorEspera
        {
            get { return jugadorEspera; }
            set { jugadorEspera = value; }
        }

        internal bool desconectado = false; //variable booleana verdadero si el servidor se cierra

        public void changeLabel2() //Método para actualizar el label que muestra los jugadores activos
        {
            label2.Text = principal.Jugadores + "";
        }

        /*Se inicializan las variables y el subproceso para recibir 
        clientes una vez que se abre el formulario de servidor*/
        private void Servidor_Load(object sender, EventArgs e)
        {
            jugadores = new Jugador[2]; // Se inicializa el arreglo de jugadores con un máximo de dos
            subprocesosJugadores = new Thread[2]; //Se inicia un arreglo de hilos para controlar los subprocesos de los dos jugadores
            jugadorActual = 0; //Se inicializa la variable jugador actual en cero

            //Acepta conexiones en un subproceso distinto
            obtenerJugadores = new Thread(new ThreadStart(Establecer));
            obtenerJugadores.Start(); //Se inician los hilos

        }//Fin del metodo Servidor_Load

        //Notifica a los jugadores para que detengan la ejecucion
        private void Servidor_FormClosing(object sender, FormClosingEventArgs e)
        {
            desconectado = true; //Se activa el estado desconectado
            oyente.Stop(); //Se cierra el listener
            principal.servidor = false; //Se desactiva el servidor en principal
            principal.cierreServidor(); //Se llama el método de cierreServidor de la clase Principal
        }//Fin del metodo Servidor_FormClosing

        //Método de desbloqueo que recibe como parámetro el número del jugador al que se debe liberar su hilo de ejecución
        public void Desbloqueo(int numero)
        {
            //verifica si se encuentra suspendido y permite que el hilo continúe su ejecución
            if (jugadores[numero].subprocesoSuspendido)
            {
                //Desbloqueo del proceso suspendido
                lock (jugadores[numero])
                {
                    jugadores[numero].subprocesoSuspendido = false;
                    Monitor.Pulse(jugadores[numero]);
                }//Fin del lock
            }
        }//Fin del método Desbloqueo

        //Método reiniciar que se encarga de incializar las variables y desbloquear los hilos para comenzar un nuevo turno
        public void reiniciar()
        {
            Espacios = "";
            Palabra = "";
            LetrasIngresadas = "";
            LetrasFallidas = "";
            cambiarTurno();
            Desbloqueo(JugadorActual);
            Desbloqueo(JugadorEspera);
            Reiniciado = true;
        }//Fin del método reiniciar

        //Método de bloqueo que recibe como parámetro el número del jugador al que se debe suspender su hilo de ejecución
        public void SuspenderProceso(int numero)
        {
            jugadores[numero].subprocesoSuspendido = true;

            //Espera la notificación de desbloqueo
            lock (jugadores[numero])
            {
                while (jugadores[numero].subprocesoSuspendido)
                {
                    Monitor.Wait(jugadores[numero]);
                }
            }//fin del lock
        }//Fin del método SuspenderProceso

        //Se notifica que todos los jugadores han abandonado el juego
        public void NoJugadores()
        {
            MessageBox.Show("Todos los jugadores han abandonado el juego.");
        }//Fin del método NoJugadores

        //Método que recibe como parámetro una letra y verifica si la misma está contenida en la palabra por adivinar
        public bool contieneLetra(char letra)
        {
            bool ans = false;

            //En caso de que la letra esté contenida en la palabra se actualizan los espacios correspondientes y se muestra la letra
            if (palabra.Contains(letra))
            {
                ans = true;
                string temporal = Espacios;
                for (int i = 0; i < palabra.Length; i++)
                {
                    if (palabra[i] == letra)
                    {
                        StringBuilder SB = new StringBuilder(temporal);
                        SB[i * 2] = letra;
                        temporal = SB.ToString();
                    }
                }
                Espacios = temporal;
            }
            else //Si no contiene la letra se bajan 5 puntos al jugador
            {
                if (jugadorActual == 0)
                {
                    Jugador1Puntos -= 5;
                }
                else
                {
                    Jugador2Puntos -= 5;
                }
            }
            return ans; //Devuelve true si la palabra contiene la letra
        }//Fin del método contieneLetra

        //Se calcula la cantidad de espacios a mostrar según la palabra definida para adivinar y se almacena en la variable espacios
        public void mostrarEspacios()
        {
            string temporal = "";
            foreach (char letter in palabra) //Asigna un espacio para cada letra de la palabra
            {
                temporal += "_ ";
            }
            Espacios = temporal;
        }//Fin del método mostrarEspacios

        //Acepta las conexiones de dos jugadores
        public void Establecer()
        {
            //El socket Exception se presenta si se tiene un error en la conexión TCP
            try
            {
                //Establece Socket
                oyente = new TcpListener(IPAddress.Parse("127.0.0.1"), 6001);
                oyente.Start();

                //Acepta el primer jugador e inicia un subproceso jugador
                jugadores[0] = new Jugador(oyente.AcceptSocket(), this, 0);
                subprocesosJugadores[0] = new Thread(new ThreadStart(jugadores[0].Ejecutar));
                subprocesosJugadores[0].Start();

                //Acepta el segundo jugador e inicia otro subproceso jugador
                jugadores[1] = new Jugador(oyente.AcceptSocket(), this, 1);
                subprocesosJugadores[1] = new Thread(new ThreadStart(jugadores[1].Ejecutar));
                subprocesosJugadores[1].Start();

                //Hace saber al primer jugador que otro jugador se conecto
                lock (jugadores[0])
                {
                    jugadores[0].subprocesoSuspendido = false;
                    Monitor.Pulse(jugadores[0]);
                }//Fin del lock
            }
            catch (SocketException)
            { //No requiere tomar ninguna acción pero evita la caída del programa
            }

        }//Fin del metodo Establecer

        //Se asigna aleatoriamente el jugador actual y se establecen las otras variables para iniciar el primer turno de juego
        public void asignarTurno()
        {
            //Establecer aleatoriamente el jugador que empieza el turno de adivinar
            Random random = new Random();
            JugadorActual = random.Next(0, 2);

            if (JugadorActual == 0)
            {
                JugadorEspera = 1;
                Jugador1Puntos = 30;
                Jugador2Puntos = 0;
            }
            else
            {
                JugadorEspera = 0;
                Jugador2Puntos = 30;
                Jugador1Puntos = 0;
            }

            //Se establece el turno = true para el jugador actual designado.
            jugadores[JugadorActual].Turno = true;
        }//Fin del método asignarTurno

        //Método que se encarga de alternar los turnos y establecer las otras variables para iniciar otro turno
        public void cambiarTurno()
        {
            if (JugadorActual == 0)
            {
                JugadorActual = 1;
                JugadorEspera = 0;
                Jugador2Puntos = 30;
                Jugador1Puntos = 0;
            }
            else
            {
                JugadorActual = 0;
                JugadorEspera = 1;
                Jugador1Puntos = 30;
                Jugador2Puntos = 0;
            }

            //Se establece el turno = true para el jugador actual designado y turno = false para el jugador en espera.
            jugadores[JugadorActual].Turno = true;
            jugadores[JugadorEspera].Turno = false;
        }//Fin del método cambiarTurno

        //Método que recibe un entero que indica el número del jugador que se desea cerrar de modo que se finalice el hilo y se asigne un null en el arreglo de jugadores
        public void cerrar(int numero)
        {
            try
            {
                subprocesosJugadores[numero].Abort();
                jugadores[numero] = null;
            }
            catch (NullReferenceException) {
                MessageBox.Show("Ha ocurrido un error.", "Error");
            }//Se obtiene un NullReferenceException en caso de que no se encuentre dicho jugador
        }//Fin del método cerrar

        //Método que recibe un entero como parámetro y verifica las condiciones para finalizar el juego
        public bool JuegoTerminado(int num)
        {
            bool ans = false;

            if (!Espacios.Contains("_"))//Adivinó la palabra y ganó el juego
            {
                ans = true;
                if (num == jugadorActual)
                {
                    principal.notifyClient(jugadorActual, "ganado."); //Se notifica que el jugador actual ha ganado

                    //Se actualizan los juegos ganados para el jugador actual
                    if (jugadorActual == 0)
                    {
                        Jugador1Ganados += 1;
                    }
                    else
                    {
                        Jugador2Ganados += 1;
                    }
                }

            } else if (JugadorActual == 0 && Jugador1Puntos<=0)//Se le acabaron los intentos y perdió
            {
                //Se notifica que el jugador actual ha perdido
                if (num == jugadorActual)
                {
                    principal.notifyClient(jugadorActual, "perdido.");
                }
                    ans = true;
            }
            else if (JugadorActual == 1 && Jugador2Puntos <= 0)//Se le acabaron los intentos y perdió
            {
                //Se notifica que el jugador actual ha perdido
                if (num == jugadorActual)
                {
                    principal.notifyClient(jugadorActual, "perdido.");
                }
                ans = true;
            }

            return ans;//Devuelve true si ha terminado el turno

        }//Fin del método JuegoTerminado

    }//Fin de la clase Servidor
}
