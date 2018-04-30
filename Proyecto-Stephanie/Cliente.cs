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

/* Clase correspondiente al Form que controla la interfaz gráfica del cliente y la usabilidad y funcionalidad del mismo,
 * contiene toda la interfaz gráfica del juego mediante la cuál interactúa el programa con el cliente.
 * 
 * Programado por:
 * Stephanie Zúñiga Villalobos
 *
 * Fecha de última modificación:
 * 29/Abril/2018
 */

namespace Proyecto_Stephanie
{
    public partial class Cliente : Form
    {
        //Constructor de la clase cliente que recibe como parámetro un objeto de tipo Principal
        public Cliente(Principal prin)
        {
            InitializeComponent();
            principal = prin; //Se asigna el parámetro recibido a la variable local principal
        }//Fin del constructor

        private Thread subprocesoSalida; //subproceso para recibir datos del servidor 
        private TcpClient conexion; //Cliente para establecer la conexion
        private NetworkStream flujo; //flujo de datos de red
        private BinaryReader lector; //facilita la lectura en el flujo
        private BinaryWriter escritor; //facilita la escritura en el flujo
        private string palabra; //Palabra por adivinar
        private string espacios; //Espacios que corresponen a la palabra por adivinar
        private int numero; //Número de jugador 
        private bool turno = false; //Es true si corresponde al turno del jugador
        private bool listo = false; //Verdadero cuando termina el juego
        private int jugadorActualpuntos = 0; //Puntos jugador actual
        private int jugadorActualganados = 0; //Ganados jugador actual
        private string letrasIngresadas =""; //Letras que el jugador ha ingresado
        private string letrasfallidas = ""; //Letras fallidas que el jugador ha ingresado
        private int jugador1puntos = 0; //Puntos jugador 1
        private int jugador2puntos = 0; //Puntos jugador 2
        private int jugador1ganados = 0; //Juegos ganados por el jugador 1
        private int jugador2ganados = 0; //Juegos ganados por el jugador 2
        private int jugadorActual; //Jugador actual 
        private Principal principal; //Objeto de tipo Principal

        //Método que controla el evento de cierre del formulario Cliente
        private void Jugador_FormClosing(object sender, FormClosingEventArgs e)
        {
            listo = true; //El juego finaliza
            principal.Jugadores--; //Se quita un jugador porque se cierra
            Exit(); //Cierra conexiones
            principal.notificarCierre(numero); //Notifica el cierre del cliente
        }//Fin del método Jugador_FormClosing

        //Método que notifica el cierre del servidor e inhabilita el textbox y el botón de la GUI para evitar que continúe el juego
        public void cierreServidor()
        {
            mostrarMensaje("Se ha cerrado el servidor.");
            textBox1.Enabled = false;
            button1.Enabled = false;
        }//Fin del método cierreServidor

        //Método que recibe como parámetro un mensaje y lo muestra al usuario
        public void ShowMessage(string Mensaje)
        {
            MessageBox.Show(Mensaje);
        }//Fin del método ShowMessage

        //Inicializa variables y subprocesos para conectar al servidor
        private void Cliente_Load(object sender, EventArgs e)
        {
            textBox1.Enabled = false;
            button1.Enabled = false;
            mostrarTextBox.ReadOnly = true;
            try
            {
                //Hace conexion con el servidor y obtiene el flujo de red asociado
                conexion = new TcpClient("127.0.0.1", 6001);
                flujo = conexion.GetStream();
                escritor = new BinaryWriter(flujo);
                lector = new BinaryReader(flujo);

                //inicia un nuevo subproceso para enviar y recibir mensajes
                subprocesoSalida = new Thread(new ThreadStart(Ejecutar));
                subprocesoSalida.Start();
            }
            catch (SocketException)
            {
                //Se notifica que ocurrió un error con la conexión a los sockets
                MessageBox.Show("Ha ocurrido un error de conexion.");
            }

            //Muestra la imagen que corresponde al inicio del juego
            DisplayImage(30);
        }//Fin del método Cliente_Load

        /*Delegado que permite llamar al método mostrarMensaje 
        en el subproceso que crea y mantiene la GUI*/
        private delegate void DisplayDelegate(string mensaje);

        /* El método mostrarMensaje establece la propiedad Text de mostrarTextBox
         * de una manera segura para el subproceso */
        //Recibe como parámetro el mensaje a mostrar en la GUI
        private void mostrarMensaje(string mensaje)
        {
            //Si la modificación de mostrarTextBox no es segura para el subproceso
            if (mostrarTextBox.InvokeRequired)
            {
                //usa el método heredado Invoke para ejecutar mostrarMensaje a traves de un delegado
                Invoke(new DisplayDelegate(mostrarMensaje), new object[] {mensaje});
            } else { //Si se puede modificar mostrarTextBox en el subproceso actual
                mostrarTextBox.Text += mensaje;
            }
        }//Fin del método mostrarMensaje

        /*Delegado que permite llamar al método DisplayImage 
        en el subproceso que crea y mantiene la GUI*/
        private delegate void DisplayImageDelegate(int puntos);

        /* El método DisplayImage establece la propiedad Image de pictureBox1
         * de una manera segura para el subproceso */
        private void DisplayImage(int puntos)
        {
            //Si la modificacion de pictureBox1 no es segura para el subproceso
            if (pictureBox1.InvokeRequired)
            {
                //usa el método heredado Invoke para ejecutar DisplayImage a traves de un delegado
                Invoke(new DisplayImageDelegate(DisplayImage), new object[] { puntos });
            }
            else
            { //Si se puede modificar pictureBox1 en el subproceso actual
                switch (puntos) //Muestra la imagen que corresponde a los puntos del jugador actual
                {
                    case 30:
                        pictureBox1.Image = Proyecto_Stephanie.Properties.Resources.start;
                        break;
                    case 25:
                        pictureBox1.Image = Proyecto_Stephanie.Properties.Resources.cabeza;
                        break;
                    case 20:
                        pictureBox1.Image = Proyecto_Stephanie.Properties.Resources.tronco;
                        break;
                    case 15:
                        pictureBox1.Image = Proyecto_Stephanie.Properties.Resources.brazo1;
                        break;
                    case 10:
                        pictureBox1.Image = Proyecto_Stephanie.Properties.Resources.brazo2;
                        break;
                    case 5:
                        pictureBox1.Image = Proyecto_Stephanie.Properties.Resources.pierna2;
                        break;
                    case 0:
                        pictureBox1.Image = Proyecto_Stephanie.Properties.Resources.pierna1;
                        break;
                }
            }
        }//Fin del método DisplayImage

        /* Delegado que permite llamar al método cambiarPalabraAdivinar 
         * en el subproceso que crea y mantiene la GUI*/
        private delegate void cambiarLabelDelegate(string etiqueta, Label label);

        /*El método cambiarLabel establece la propiedad Text de cambiarLabel
        de una manera segura para el subproceso*/
        private void cambiarLabel(string etiqueta, Label label)
        {
            //Si la modificacion de la etiqueta no es segura para el subproceso
            if (label.InvokeRequired)
            {
                //usa el método heredado Invoke para ejecutar cambiarLabel a traves de un delegado
                Invoke(new cambiarLabelDelegate(cambiarLabel), new object[] {etiqueta,label});
            }
            else
            { //Si se puede modificar la etiqueta en el subproceso actual
                label.Text = etiqueta;
            }
        }//Fin del método cambiarPalabraAdivinar

        /* Delegado que permite llamar al método activarIngreso 
         * en el subproceso que crea y mantiene la GUI*/
        private delegate void activarIngresoDelegate(bool estado);

        /* El método activarIngreso permite activar o desactivar textBox1 y button1
         * de una manera segura para el subproceso*/
        private void activarIngreso(bool estado)
        {
            //Si la modificacion de estado de textBox1 o button1 no es segura para el subproceso
            if (textBox1.InvokeRequired || button1.InvokeRequired)
            {
                //usa el método heredado Invoke para ejecutar activarIngreso a traves de un delegado
                Invoke(new activarIngresoDelegate(activarIngreso), new object[] { estado });
            }
            else
            { //Si se puede modificar el estado de textBox1 y button1
                textBox1.Enabled = estado;
                button1.Enabled = estado;
            }
        }//Fin del método activarIngreso

        //Subproceso de control que permite la actualizacion continua en pantalla
        public void Ejecutar()
        {
            //Procesa los mensajes entrantes
            try
            {
                while (!listo)
                {
                    //Recibe los mensajes que se envian al cliente
                        ProcesarMensaje(lector.ReadString());
                }
            } catch (IOException)
            {
                MessageBox.Show("No se puede continuar con el juego.","Error",MessageBoxButtons.OK,MessageBoxIcon.Error);
            } //Fin del catch
        }//Fin del método ejecutar

        //Método que se encarga de actualizar los puntos de los jugadores y cambiar el label que los muestra en la interfaz
        //Recibe como parámetros el número de jugador actual y los puntos correspondientes
        private void ActualizarPuntos(int num, int ptos)
        {
            if (num == 0)
            {
                jugador1puntos = ptos;
                jugador2puntos = 0;
                cambiarLabel("" + jugador1puntos, Jugador1Puntos);
                cambiarLabel("" + jugador2puntos, Jugador2Puntos);
            }
            else
            {
                jugador2puntos = ptos;
                jugador1puntos = 0;
                cambiarLabel("" + jugador1puntos, Jugador1Puntos);
                cambiarLabel("" + jugador2puntos, Jugador2Puntos);
            }
        }//Fin del método ActualizarPuntos

        //Método que se encarga de actualizar los juegos ganados y mostrarlo en la interfaz al usuario
        //Recibe como parámetros el número de jugador actual y los juegos ganados correspondientes
        private void ActualizarGanados(int num, int ganados)
        {
            if (num == 0)
            {
                jugador1ganados = ganados;
                cambiarLabel("" + jugador1ganados, Jugador1Ganados);
            }
            else
            {
                jugador2ganados = ganados;
                cambiarLabel("" + jugador2ganados, Jugador2Ganados);
            }
        }//Fin del método ActualizarGanados

        //Procesa los mensajes enviados al cliente
        private void ProcesarMensaje(string mensaje)
        {
            if (mensaje == "Asignar turno") //Se encarga de asignar el turno y mostrar el mensaje correspondiente al usuario en la interfaz
            {
                //Obtiene el turno del jugador
                turno = lector.ReadBoolean();
                numero = lector.ReadInt16();

                mostrarMensaje(turno ? "Es su turno de adivinar.\n" : "Es su turno de ingresar una palabra.\n");
                activarIngreso(!turno);

                //Se inicializan los campos correspondientes a los puntos de cada jugador y los juegos ganados a mostrar en la interfaz
                if (jugadorActual == 0)
                {
                    jugador1puntos = 30;
                    jugador2puntos = 0;
                }
                else
                {
                    jugador2puntos = 30;
                    jugador1puntos = 0;
                }
                cambiarLabel("" + jugador1puntos, Jugador1Puntos);
                cambiarLabel("" + jugador2puntos, Jugador2Puntos);
                cambiarLabel("" + jugador1ganados, Jugador1Ganados);
                cambiarLabel("" + jugador2ganados, Jugador2Ganados);
            }
            else if (mensaje == "Asignar jugador actual.") //Se obtiene el jugador actual
            {
                jugadorActual = lector.ReadInt16();
            }
            else if (mensaje == "Empieza adivinar.") //Si le corresponde adivinar se activa el ingreso de letras y se notifica al usuario en la GUI
            {
                activarIngreso(true);
                mostrarMensaje("Ingresa una letra para comenzar el juego. \n");
            }
            else if (mensaje == "Desactivar.") //Desactiva el ingreso de letras y se reinicia la imagen a mostrar del ahorcado
            {
                activarIngreso(false);
                DisplayImage(30);
            }
            else if (mensaje == "Activar.") //Se activa el campo para ingreso de letras o palabra según corresponda
            {
                activarIngreso(true);
            }
            else if (mensaje == "Letras Ingresadas.") //Obtiene las letras ya ingresadas
            {
                letrasIngresadas = lector.ReadString();
            }
            else if (mensaje == "Letras Fallidas.") //Obtiene las letras fallidas y las muestra en la interfaz
            {
                letrasfallidas = lector.ReadString();
                cambiarLabel(letrasfallidas, letrasFallidas);
            }
            else if (mensaje == "Mostrar Espacios.") //Se actualizan los espacios a mostrar de la palabra por advinar
            {
                espacios = lector.ReadString();
                cambiarLabel(espacios, palabra_Adivinar);
            } else if (mensaje == "Puntos Jugador Actual.")//Actualiza los puntos del jugador actual
            {
                jugadorActualpuntos = lector.ReadInt16();
                ActualizarPuntos(jugadorActual,jugadorActualpuntos);
                DisplayImage(jugadorActualpuntos);
            }
            else if (mensaje == "Ganados Jugador Actual.")//Actualiza los juegos ganados por el jugador actual
            {
                jugadorActualganados = lector.ReadInt16();
                ActualizarGanados(jugadorActual, jugadorActualganados);
                DisplayImage(jugadorActualpuntos);
            }
            else if (mensaje == "") //Si se envía un mjs vacío por el streamer no sucede nada
            {
            }
            else
            {
                mostrarMensaje(mensaje + "\n");//Muestra el mensaje recibido
            }
            //verifica la letra que envio el jugador al servidor y si la contiene la muestra 
        }//Fin del método ProcesarMensaje

        //Método que recibe como parámetro una palabra y verifica que sea válida según las reglas de juego establecidas
        private bool verificarPalabra(string pal)
        {
            bool ans = true;
            if (pal.Length < 5 || pal.Length > 8) //La palabra de tener minimo 5 caracteres y maximo 8.
            {
                ans = false;
                MessageBox.Show("La palabra de tener mínimo 5 caracteres y máximo 8.","Error");
            }
            else if (pal.All(Char.IsWhiteSpace) || !pal.All(Char.IsLetter)) //Verifica que no se ingresen espacios en blanco o caracteres no válidos
            {
                ans = false;
                MessageBox.Show("La palabra únicamente debe contener letras", "Error");
            }
            return ans;
        }//Fin del método verificarPalabra

        //Método que recibe como parámetro una letra y verifica que sea válida según las reglas del juego
        private bool verificarletra(string let)
        {
            bool ans = true;
            if (let.Length != 1) //Solo puede ingresar una letra a la vez
            {
                ans = false;
                MessageBox.Show("Debe ingresar solo una letra.", "Error");
            }
            else
            {
                char letra = let[0];

                if (char.IsWhiteSpace(letra) || !char.IsLetter(letra)) //El caracter ingresado debe de ser exclusivamente una letra
                {
                    ans = false;
                    MessageBox.Show("La palabra únicamente contiene letras.", "Error");
                }
                else if (letrasIngresadas.Contains(letra))  //Verifica si ya se ha ingresado esta letra antes y se notifica al usuario
                {
                    ans = false;
                    MessageBox.Show("Ya ha ingresado la letra: " + letra + ". Ingrese otra letra.", "Error");
                }
            }
            return ans;
        }//Fin del método verificarletra

        //Método que controla el evento cuando se presiona el botón para ingresar una letra o palabra para adivinar
        private void button1_Click(object sender, EventArgs e)
        {
            if (turno)//Si es el jugador en turno le corresponde adivinar por lo tanto se ingresan letras
            {
                if (verificarletra(textBox1.Text))//Verifica que se haya ingresado una letra
                {
                    char letra = textBox1.Text.ToLower()[0];
                    mostrarMensaje("Ha ingresado la letra: " + letra +".\n");
                    escritor.Write(letra);
                    textBox1.Text = "";
                }             
            }
            else
            { //Si no es el jugador en turno le corresponde ingresar una palabra para adivinar
                palabra = textBox1.Text.ToLower();
                if (verificarPalabra(palabra)) //Verifica la validez de la palabra ingresada
                {
                    mostrarMensaje("Ha ingresado la palabra. \n");
                    escritor.Write(palabra);
                    textBox1.Text = "";
                    activarIngreso(false);
                }
            }
        }//Fin del método button1_Click

        //Notifica que salió un jugador e inhabilita el botón y el textbox para evitar que continúe el juego
        public void jugadorSalio()
        {
            if (numero == 0)
            {
                mostrarMensaje("El jugador 2 ha abandonado el juego. \n");
            } else {
                mostrarMensaje("El jugador 1 ha abandonado el juego. \n");
            }
            textBox1.Enabled = false;
            button1.Enabled = false;
        }//Fin del método jugadorSalio

        //Método que se encarga de cerrar todas las conexiones y finalizar los subprocesos
        private void Exit()
        {
            try
            {
                //Cierra la conexion de los sockets
                escritor.Close();
                lector.Close();
                flujo.Close();
                conexion.Close();
                subprocesoSalida.Abort();
            }
            catch (NullReferenceException) { 
                //Se presenta cuando no se encuentra uno de los objetos que se intentan cerrar o finalizar
                //Evita que se caiga el programa pero no se debe ejecutar ninguna acción adicional
            }
        }//Fin del método Exit()

        //Método que maneja el evento de salida de la interfaz mediante el botón que presenta al usuario la opción
        private void button2_Click(object sender, EventArgs e)
        {
            //Se consulta mediante una ventana de diálogo si realmente desea salir del juego
            DialogResult dialog = MessageBox.Show("Esta seguro que desea salir del juego?", "Salir",MessageBoxButtons.YesNo,MessageBoxIcon.Question);
           
            //Si la respuesta es positiva entonces se llama al método exit y se cierra la ventana
            if (dialog == DialogResult.Yes)
            {
                Exit();
                this.Close();
            }
        }//Fin del método button2_Click

    }//Fin de la clase Cliente
}