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
        public const int filasCache = 4;
        public const int columnasCache = 16;
	    int[,] cache = new int[filasCache,columnasCache];

        //contiene los 32 registros del procesador
	    int[] registros = new int[32];

        //Contiene el PC y los registros de cada hilo, primero los 32 registros y por último el PC
        public const int filasContexto = 4;
        public const int columnasContexto = 33;
        int[,] contexto = new int[filasContexto, columnasContexto];

        //Variable para manejar el reloj del procesador
        int reloj = 0;

        //Diccionario que asocia el operando con su correspondiente operacion
	    Dictionary<int,string> operaciones = new Dictionary<int, string>(); 

        //vector para bloque, palabra e indice
	    int[] ubicacion = new int[3];

        //Memoria principal del procesador, comienza en 128
        int[] memoria = new int[256];

        //Se almacena el numero de fila del contexto ejecutándose actualmente
        int filaContextoActual = 0;

        /*Constructor de la clase procesador*/
        public Procesador()
        {
            /*Operaciones de los hilos asociados en el diccionario*/
            operaciones.Add(8, "DADDI");
            operaciones.Add(32, "DADD");
            operaciones.Add(34, "DSUB");
            operaciones.Add(12, "DMUL");
            operaciones.Add(14, "DDIV");
            operaciones.Add(4, "BEQZ");
            operaciones.Add(5, "BNEZ");
            operaciones.Add(3, "JAL");
            operaciones.Add(2, "JR");
            operaciones.Add(63, "FIN");
            inicializarEstructuras();
        }

        public void inicializarEstructuras()
        {

            //Se inicializa con ceros la cache
            for (int contadorFilas = 0; contadorFilas < filasCache; ++contadorFilas)
            {
                for (int contadorColumnas = 0; contadorColumnas < columnasCache; ++contadorColumnas )
                {
                    cache[contadorFilas, contadorColumnas ] = 0;
                }
            }

            //Se inicializa con ceros la memoria
            for (int i = 0; i < memoria.Length; ++i )
            {
                memoria[i] = 0;
            }

            //
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
                ejecutarInstruccion();

            }
            else
            {
                //Llama el metodo de fallo de cache
                ejecutarFalloCache();

                //Hacer for de 16 ciclos
                for (int i = 0; i < 16; ++i)
                {
                    //Creo que se debe poner una barrera
                }

            }
        }

        /*Método para ejecutar la instrucción */
        public void ejecutarInstruccion()
        {
            string operando;

            //CAMBIAR EL CODIGO DE OPERACION
            int codigoOperacion =1;

            if(operaciones.TryGetValue(codigoOperacion, out operando))
            {
                    switch (operando)
                    {
                        case "DADDI":
                            break;
                        case "DADD":
                            break;
                        case "DSUB":
                            break;
                        case "DMUL":
                            break;
                        case "DDIV":
                            break;
                        case "BEQZ":
                            break;
                        case "BNEZ":
                            break;
                        case "JAL":
                            break;
                        case "JR":
                            break;
                        case "FIN":
                            --hilosActivos;
                            break;
                    }
            }

            else
            {

            }
        }

        /*Método para arreglar un fallo de caché, cargando en caché*/
        public void ejecutarFalloCache()
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

        /*Método para ejecutar instrucciones por parte del procesador */
        public void ejecutarInstrucciones() 
        { 
            
            int contadorInstrucciones = 0;
            int contadorContexto = 0;

            while (hilosActivos > 0)
            {
                while (contadorInstrucciones < quantum)
                {
                    contadorInstrucciones++;
                    leerInstruccion();
                }

                //Se copia en el contexto los registros porque se acabó el quantum
                for (contadorContexto = 0; contadorContexto < columnasContexto-1; ++contadorContexto)
                {
                    contexto[filaContextoActual, contadorContexto] = registros[contadorContexto];
                }

                //Se copia en la última columna del contexto el PC a ejecutar posteriormente
                contexto[filaContextoActual, contadorContexto] = PC + 4;

                //Se inicializa en 0 nuevamente el contador de instrucciones
                contadorInstrucciones = 0;

                //Se verifica si la fila actual del contexto es la última, pues en caso de serlo, el siguiente
                //hilillo a ejecutar es el ubicado en la primer fila del contexto, sino se ejecuta la siguiente fila.
                if (filaContextoActual == filasContexto)
                {
                    PC = contexto[0, columnasContexto - 1];
                    filaContextoActual = 0;
                }
                else
                {
                    ++filaContextoActual;
                    PC = contexto[filaContextoActual, columnasContexto - 1];
                    
                }

            }           
        }

        /*Método para iniciar el proceso de simulación */
        public void ejecutarSimulacion()
        {
 
        }


    }
}
