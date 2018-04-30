using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

/* Clase correspondiente al Form que controla la interfaz gráfica principal y la usabilidad y funcionalidad de la misma,
 * contiene tres botones uno para abrir el servidor, otro para abrir un jugador y otro para salir por completo del programa.
 * 
 * Programado por:
 * Stephanie Zúñiga Villalobos
 *
 * Fecha de última modificación:
 * 29/Abril/2018
 */

namespace Proyecto_Stephanie
{
    public partial class Principal : Form
    {
        public bool servidor = false; //Variable lógica que indica si el servidor se encuentra activo
        public Servidor server; //Objeto de tipo Servidor
        public Cliente player1; //Objeto de tipo Cliente
        public Cliente player2; //Objeto de tipo Cliente

        private int jugadores = 0; //Variable que lleva el conteo de los jugadores
        public int Jugadores //Método get y set de jugadores
        {
            get { return jugadores; }
            set
            {
                jugadores = value;
                server.changeLabel2();
            }
        }


        public Principal() //Método constructor de Principal
        {
            InitializeComponent();
        }

        //Método que se encarga de manejar el evento del botón para activar un nuevo servidor
        private void button1_Click(object sender, EventArgs e)
        {
            //Verifica si el servidor se encuentra activo
            if (servidor == false) //No hay servidor activo
            {
                //Abre una nueva ventana de servidor
                server = new Servidor(this);
                server.Show();
                servidor = true;
            }
            else {//Si ya existe un servidor activo se notifica
                MessageBox.Show("Ya se ha iniciado el servidor.","Error");
            }
        }//Fin del método button1_Click

        //Método que recibe como parámetro el número del jugador actual y un string de condición que notifica a los clientes si el jugador actual ha ganado o perdido
        public void notifyClient(int num, string condicion)
        {
            if (num == 0) //Jugador actual es el jugador 1
            {
                player1.ShowMessage("Usted ha " + condicion);
                player2.ShowMessage("El jugador 1 ha " + condicion);
            }
            else { //Jugador actual es el jugador 2
                player2.ShowMessage("Usted ha " + condicion);
                player1.ShowMessage("El jugador 2 ha " + condicion);
            }
        }//Fin del método notifyClient

        //Método de cierre de servidor que notifica a los clientes
        public void cierreServidor()
        {
            //Notifica que se ha cerrado el servidor
            MessageBox.Show("Se ha cerrado el servidor.", "Error");
            try {
                //Notifica cierre de servidor en cada cliente
                player1.cierreServidor();
                player2.cierreServidor();
            } catch (NullReferenceException)
            { 
                //No debe realizar nada, se presenta cuando no se encuentra uno de los clientes.
            }
        }//Fin del método cierreServidor

        //Método que se encarga de manejar el evento del botón para activar un nuevo jugador
        private void button2_Click(object sender, EventArgs e)
        {
            //Verifica que se encuentre activo un servidor
            if (servidor == true)
            {
                //Se abre el cliente para el jugador correspondiente
                switch (Jugadores)
                {
                    case 0:
                        player1 = new Cliente(this);
                        player1.Text = "Jugador 1";
                        player1.Show();
                        Jugadores++;
                        break;
                    case 1:
                        player2 = new Cliente(this);
                        player2.Text = "Jugador 2";
                        player2.Show();
                        Jugadores++;
                        break;
                    default: //Se define un máximo de dos jugadores
                        MessageBox.Show("Ha alcanzado el maximo de jugadores (2).", "Error");
                        break;
                }
            }
            else {
                MessageBox.Show("Debe iniciar el servidor.", "Error"); //Se notifica al usuario que debe abrir primero un servidor
            }
        }//Fin del método button2_Click

        //Método que recibe como parámetro el número del jugador que ha abandonado el juego
        public void notificarCierre(int num)
        {
            //Llama al método para cerrar el jugador en el servidor
            server.cerrar(num);

            //Notifica que el jugador ha abandonado el juego
            MessageBox.Show("El jugador " + (num + 1) + " ha abandonado el juego.");
            try
            {
                if (num == 0)
                {
                    player2.jugadorSalio();
                }
                else
                {
                    player1.jugadorSalio();
                }
            }
            catch (NullReferenceException)
            { //No se requiere tomar ninguna accion, se da si no se encuentra alguno de los clientes
            }
            //Si todos los jugadores salen del juego se notifica en servidor y se cierra el mismo
            //De modo que para iniciar un nuevo juego se debe de reiniciar el servidor
            if (Jugadores == 0)
            {
                server.NoJugadores();
                server.Close();
            }
        }//Fin del método notificarCierre

        //Método que se encarga de manejar el evento del botón para salir y cierra el programa
        private void button3_Click(object sender, EventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }//Fin del método button3_Click

        //Método que se encarga de manejar el evento de cierre de la ventana Principal y cierra el programa 
        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }//Fin del método Principal_FormClosing

    }//Fin de la clase Principal
}
