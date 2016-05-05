using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace proyectoArqui
{
    class Procesador
    {
        //variable para almacenar el quantum
        int quantum;

        //variable para almacenar cuantos hilos tiene activos ese procesador
        int hilosActivos = 0;

        //program counter del procesador
        int PC;

        //cache del procesador, 4 palabras mas el bloque, y 4x4 bloques
	    int[,] cache = new int[5,16];

        //contiene los 32 registros del procesador
	    int[] registros = new int[32];

        //Contiene el PC y los registros de cada hilo, primero los 32 registros y por último el PC
	    int[,] contexto = new int[4,33];

        //Diccionario que asocia el operando con su correspondiente operacion
	    Dictionary<int,string> operaciones = new Dictionary<int, string>(); 

        //vector para bloque, palabra e indice
	    int[] ubicacion = new int[3];

        //Memoria principal del procesador, comienza en 128
        int[] memoria = new int[256];

        /*Constructor de la clase procesador*/
        public Procesador()
        {
            /*Operaciones de los hilos asociados en el diccionario*/
            operaciones.Add(5, "DADDI");
            inicializarEstructuras();
        }

        public void inicializarEstructuras()
        {
 
        }

        /*Método para leer una instrucción en la cache*/
        public void leerInstruccion()
        {
            /*Calcula el bloque en memoria*/
            int bloque = PC / 16;

            /*Calcula la palabra en memoria*/
            int palabra = bloque % 16;
            palabra = palabra / 4;

            /*Calcula el indice en la caché*/
            int indice = bloque % 4;

            /*Vector que guarda los datos de la instrucción que se esté ejecutando*/
            ubicacion[0] = bloque;
            ubicacion[1] = palabra;
            ubicacion[2] = indice;

            //se ejecuta la instruccion porque estaba en cache	
            if (cache[5, indice * 4] == bloque)
            {
                //Lee el diccionario y ejecuta la instruccion con un switch
            }
            else
            {
                //Llama el metodo de fallo de cache
                falloCache();
            }
        }

        /*Método para arreglar un fallo de caché, cargando en caché*/
        public void falloCache()
        {
            /*Calcula la dirección fisica en memoria*/
            int direccionFisica = PC - 128;

            /*Carga en caché lo que está apuntando la dirección fisica */
            for (int i = 0; i < 4; ++i)
            {
                for (int c = 0; c < 4; c++)
                {
                    /*Carga de la caché*/
                    cache[i , ubicacion[2] * 4 + c] = memoria[direccionFisica + c];
                }

                /*La dirección fisica aumenta de 4 en 4 bytes*/
                direccionFisica = direccionFisica + 4;
            }
        }

        /*Método para ejecutar instrucciones por parte del procesador
         * 
         */
        public void ejecucionInstrucciones() 
        { 
            
            int i = 0;
            int j = 0;

            while (j < hilosActivos)
            {
                while (i < quantum)
                {
                    i++;
                    leerInstruccion();
                }

                j++;
            }

            
        }
    }
}
