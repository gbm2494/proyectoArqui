using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

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

        //cache del procesador, 4 palabras + el bloque, y 4x4 bloques
        public const int filasCache = 5;
        public const int columnasCache = 16;
	    int[,] cache = new int[filasCache,columnasCache];

        //contiene los 32 registros del procesador
        public const int cantidadRegistros = 32;
	    int[] registros = new int[cantidadRegistros];

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
        public const int cantidadMemoria = 256;
        int[] memoria = new int[cantidadMemoria];

        //Se almacena el número de fila del contexto ejecutándose actualmente
        int filaContextoActual = 0;

        /* Barrera para controlar cuando todos los hilos han ejecutado una instrucción, son 4 participantes porque el hilo principal
        también debe interactuar con éstos*/
        public static Barrier barreraFinInstr = new Barrier(participantCount: 4);

        /* Barrera para controlar que todos los hilos esperen mientras el hilo principal les aumenta el reloj, son 4 participantes porque el hilo principal
        también debe interactuar con éstos*/
        public static Barrier barreraCambioReloj = new Barrier(participantCount: 4);
     
        /*Variable que se utiliza para saber si un procesador ya terminó todas las ejecuciones de sus hilillos */
        bool terminarEjecucion = false;

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
                
                    for (int contadorColumnas = 0; contadorColumnas < columnasCache; ++contadorColumnas)
                    {
                        if (contadorFilas != filasCache - 1)
                        {
                            cache[contadorFilas, contadorColumnas] = 0;
                        }

                        else
                        {
                            cache[contadorFilas, contadorColumnas] = -1;
                        }
                    }
                
                
            }

            

            //Se inicializa con ceros la memoria
            for (int i = 0; i < cantidadMemoria; ++i )
            {
                memoria[i] = 0;
            }

            //se inicializa con ceros los registros
            for (int i = 0; i < cantidadRegistros; i++)
            {
                registros[i] = 0;
            }

            //se inicializa con ceros el contexto
            for (int i = 0; i < filasContexto; i++)
            {
                for (int j = 0; j < columnasContexto; j++)
                {
                    contexto[i, j] = 0;
                }
            }
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

            //Se ejecuta la instrucción porque estaba en cache	
            if (cache[5, indice * 4] == bloque)
            {
                ejecutarInstruccion();
                barreraFinInstr.SignalAndWait();
                barreraCambioReloj.SignalAndWait();

            }
            else
            {
                //Llama el metodo de fallo de cache
                ejecutarFalloCache();

                //For de 16 ciclos para simular lo que se tarda en subir un bloque de memoria principal a caché
                for (int i = 0; i < 16; ++i)
                {
                    barreraFinInstr.SignalAndWait();
                    barreraCambioReloj.SignalAndWait();
                }

            }
        }

        /*Método para ejecutar únicamente una instrucción */
        public void ejecutarInstruccion()
        {
            string operando;
            int contadorFilas = 0;


            /* Se busca la fila en donde se encuentra la palabra que se debe ejecutar, ubicacion[2] posee la palabra */
            while(contadorFilas < 4 && cache[contadorFilas, ubicacion[3]*4 ] != ubicacion[2])
            {
                ++contadorFilas;
            }

            int codigoOperacion = cache[contadorFilas,ubicacion[3]*4];

            if(operaciones.TryGetValue(codigoOperacion, out operando))
            {
                    switch (operando)
                    {
                        case "DADDI":
                            registros[cache[contadorFilas, ubicacion[3] * 4 + 2]] = registros[0] + cache[contadorFilas, ubicacion[3] * 4 + 3];
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
            for (int i = 0; i < 5; ++i)
            {
                for (int c = 0; c < 4; c++)
                {
                    if(i == 4)
                    {
                        /* Se ubica en la última fila de la caché el # de bloque al que están asociadas las palabras que se están cargando 
                         desde la memoria principal. Ubicacion[0] contiene el # de bloque. */
                        cache[i, ubicacion[2] * 4 + c] = ubicacion[0];

                    }
                    else
                    {
                        /* Se carga la palabra de la memoria principal a la caché, en ubicacin[2] se tiene almacenado el índice (# de columna)
                        de la caché donde se deben cargar las palabras */
                        cache[i, ubicacion[2] * 4 + c] = memoria[direccionFisica + c];
                    }


                }

                /*La dirección fisica aumenta de 4 en 4 bytes*/
                direccionFisica = direccionFisica + 4;
            }
        }

        /* Método para incrementar el valor del reloj */
        public void aumentarReloj()
        {
            ++reloj;
        }

        /* Método para obtener el valor de la variable terminarEjecución que indica si el procesador se encuentra aún con hilos pendientes
         o no */
        public bool getEjecucion()
        {
            return terminarEjecucion;
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
                //hilillo a ejecutar es el ubicado en la primer fila del contexto, sino se ejecuta el que se encuentra en la siguiente fila.
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

            terminarEjecucion = true;
            barreraFinInstr.RemoveParticipant();
            barreraCambioReloj.RemoveParticipant();
        }
    }
}
