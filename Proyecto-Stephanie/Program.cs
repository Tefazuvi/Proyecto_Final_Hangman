using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

/* Programa con interfaz gráfica de tipo windows forms que utiliza conexión cliente servidor de tipo TCP para 
 * jugar ahorcado entre dos jugadores o clientes. Inicialmente, se debe abrir el servidor y posteriormente se inician los dos jugadores
 * desde la pantalla principal de la aplicación. Se comienza con un jugador aleatorio y luego se intercambian los turnos entre 
 * ambos jugadores.
 * 
 * Programado por:
 * Stephanie Zúñiga Villalobos
 *
 * Fecha de última modificación:
 * 29/Abril/2018
 */

namespace Proyecto_Stephanie
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Principal());
        }
    }
}
