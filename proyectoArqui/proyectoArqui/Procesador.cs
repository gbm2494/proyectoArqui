using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace proyectoArqui
{

    class Procesador
    {
        //variable para almacenar el quantum
        public int quantum = 0;

        //variable para almacenar cuantos hilos tiene activos ese procesador
        public int hilosActivos;

        //program counter del procesador
        int PC = 128;

        //cache del procesador, 4 palabras + el bloque, y 4x4 bloques
        public const int filasCache = 5;
        public const int columnasCache = 16;
	    int[,] cache = new int[filasCache,columnasCache];

        //contiene los 32 registros del procesador
        public const int cantidadRegistros = 32;
	    int[] registros = new int[cantidadRegistros];

        //Contiene el PC y los registros de cada hilo, primero los 32 registros y por último el PC
        public readonly int filasContexto;
        public readonly int columnasContexto = 33;
        public int[,] contexto; 

        //Variable para manejar el reloj del procesador
        int reloj = 1;

        //Diccionario que asocia el operando con su correspondiente operacion
	    Dictionary<int,string> operaciones = new Dictionary<int, string>(); 

        //vector para bloque, palabra e indice
	    int[] ubicacion = new int[3];

        //Memoria principal del procesador, comienza en 128
        public const int cantidadMemoria = 256;
        public int[] memoria = new int[cantidadMemoria];

        //Se almacena el número de fila del contexto ejecutándose actualmente
        int filaContextoActual = 0;

        /* Barrera para controlar cuando todos los hilos han ejecutado una instrucción, son 4 participantes porque el hilo principal
        también debe interactuar con éstos*/
        public static Barrier barreraFinInstr = new Barrier(participantCount: 4);

        /* Barrera para controlar que todos los hilos esperen mientras el hilo principal les aumenta el reloj y la cantidad de ciclos, son 4
        participantes porque el hilo principal también debe interactuar con éstos*/
        public static Barrier barreraCambioReloj_Ciclo = new Barrier(participantCount: 4);
     
        /*Variable que se utiliza para saber si un procesador ya terminó todas las ejecuciones de sus hilillos */
        bool terminarEjecucion = false;


        /*Arreglo donde el número de filas indican la cantidad de hilos a ejecutar, la primer columna simboliza el número del hilo, la segunda 
         la cantidad de ciclos realizados, la tercera el valor del reloj al iniciar la ejecución y la cuarta el valor del reloj al finalizar la 
         ejecución y la cuarta el número del procesador donde se ejecutará el hilo. */
        public readonly int filasDatosHilos;
        public const int columnasDatosHilos = 6;
        public int[,] datosHilos;


        /* Arreglo utilizado para conocer el estado del hilo ejecutandose: si es la primera vez y si ya terminó ejecución */
        readonly int filasEjecucionHilos;
        readonly int columnasEjecucionHilos = 2;
        public int[,] ejecucionHilos;

        /*Método para pruebas que imprime en el debugger la memoria del procesador*/
        public void imprimirMemoria() {
            for (int i = 0; i < cantidadMemoria; i++)
                Debug.WriteLine(memoria[i]);
        }

        /*Método para pruebas que imprime en el debugger el contexto del procesador*/
        public void imprimirContexto()
        {
             for(int i = 0; i < filasContexto; i++)
             {
                 Debug.WriteLine(contexto[i,32]);
             }
        }

        /*Método para obtener el nombre de los hilos ejecutados en el procesador*/
        public string getNombreHilos()
        {
            string retorno = "";

            for (int i = 0; i < filasDatosHilos; i++)
            {
                retorno = retorno + " " + datosHilos[i, 0];
            }

            return retorno;
        }

        /*Método para obtener el contexto del hilo especificado por el parámetro del método*/
        public string getContextoHilo(int idHilo)
        {
            string retorno = ""; 

            for (int i = 0; i < columnasContexto - 1; i++)
            {
                retorno = retorno + contexto[idHilo, i] + " ";
            }

            return retorno;
        }

        /*Método para obtener la cantidad de ciclos del hilo especificado por el parámetro del método*/
        public string getCicloHilo(int idHilo)
        {
            string retorno = "";
            
            retorno = "" +  datosHilos[idHilo, 1];
            
            return retorno;
        }

        /*Método para obtener el reloj inicial del hilo especificado por el parámetro del método*/
        public string getInicialHilo(int idHilo)
        {
            string retorno = "";

            retorno = "" + datosHilos[idHilo, 2];

            return retorno;
        }

        /*Método para obtener el reloj final del hilo especificado por el parámetro del método*/
        public string getFinalHilo(int idHilo)
        {
            string retorno = "";

            retorno = "" + datosHilos[idHilo, 3];

            return retorno;
        }

        /*Constructor de la clase procesador*/
        public Procesador(int numHilos)
        {
            /*Operaciones de los hilos agregadas al diccionario*/
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

            filasContexto = numHilos;
            contexto = new int[filasContexto, columnasContexto];

            filasDatosHilos = numHilos;
            datosHilos = new int[filasDatosHilos, columnasDatosHilos];

            filasEjecucionHilos = numHilos;
            ejecucionHilos = new int[filasEjecucionHilos, columnasEjecucionHilos];

            hilosActivos = numHilos;

            inicializarEstructuras();
        }

       
        /*Método para inicializar todas las estructuras utilizadas en valores correctos*/
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

            //Se inicializa con ceros los registros
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

            /* Se inicializa con ceros el arreglo que almacenará datos importantes sobre cada hilo, tales como el número de hilo la cantidad de ciclos
             realizados, el valor del reloj al iniciar la ejecución, el valor del reloj al finalizar la ejecución y el número del procesador donde
             se ejecutó */
            for(int i = 0; i < filasDatosHilos; ++i)
            {
                for(int j = 0; j < columnasDatosHilos; ++j)
                {
                    datosHilos[i, j] = 0;
                }
            }

            /* Se inicializa con ceros el arreglo que mantiene el estado de ejecución de cada hilo */
            for (int i = 0; i < filasEjecucionHilos; ++i)
            {
                for (int j = 0; j < columnasEjecucionHilos; ++j)
                {
                    ejecucionHilos[i, j] = 0;
                }
            }

        }

        /*Método para indicar en el arreglo el número del hilo así como el número del procesador donde correrá el hilo */
        public void setNumHilo_Procesador(int numFila, int numHilo, int numProcesador)
        {
            datosHilos[numFila, 0] = numHilo;
            datosHilos[numFila, 4] = numProcesador;
        }

        /*Método para indicar en el arreglo el valor inicial del reloj al iniciar la ejecucion del hilo */
        public void setValorInicialReloj()
        {
            datosHilos[filaContextoActual, 2] = reloj;
        }

        /*Método para leer una instrucción en la cache*/
        public void leerInstruccion()
        {
            /*Calcula el bloque en memoria*/
            int bloque = PC / 16;

            /*Calcula la palabra en memoria*/
            int palabra = PC % 16;
            palabra = palabra / 4;

            /*Calcula el indice en la caché*/
            int indice = bloque % 4;

            /*Vector que guarda los datos de la instrucción que se esté ejecutando*/
            ubicacion[0] = bloque;
            ubicacion[1] = palabra;
            ubicacion[2] = indice;

            //Se ejecuta la instrucción porque estaba en cache	
            if (cache[4, indice * 4] == bloque)
            {
                /* Se cambia el valor del PC a la dirección de la próxima instrucción */
                PC = PC + 4;

                ejecutarInstruccion();
                barreraFinInstr.SignalAndWait();
                barreraCambioReloj_Ciclo.SignalAndWait();

            }
            else
            {
                //Llama el metodo de fallo de cache
                ejecutarFalloCache();

                //For de 16 ciclos para simular lo que se tarda en subir un bloque de memoria principal a caché
                for (int i = 0; i < 16; ++i)
                {
                    barreraFinInstr.SignalAndWait();
                    barreraCambioReloj_Ciclo.SignalAndWait();
                }

            }
        }

        /*Método para ejecutar únicamente una instrucción */
        public void ejecutarInstruccion()
        {
            string operando;

            /* Variable utilizada para conocer el número de fila donde se encuentra la palabra que se desea ejecutar, Ubicacion[1] 
            posee el número de palabra a ejecutar, pero como se requiere el índice, debo restarle 1  */
            int contadorFilas = ubicacion[1];

            /* Se obtiene el primer operando de la palabra o instrucción*/

            int codigoOperacion = cache[contadorFilas, ubicacion[2] * 4];

            if(operaciones.TryGetValue(codigoOperacion, out operando))
            {
                    switch (operando)
                    {
                        case "DADDI":
                            /* Ubicacion[3] contiene el índice de la cache donde se encuentra el bloque almacenado  */
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 2]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + cache[contadorFilas, ubicacion[2] * 4 + 3];
                            break;
                        case "DADD":
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] + registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "DSUB":
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] - registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "DMUL":
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] * registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "DDIV":
                            registros[cache[contadorFilas, ubicacion[2] * 4 + 3]] = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] / registros[cache[contadorFilas, ubicacion[2] * 4 + 2]];
                            break;
                        case "BEQZ":
                            /* Se verifica la condición del salto */
                            if(registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] == 0)
                            {
                                /* Se multiplica la cantidad de instrucciones que debe retornarse por 4 debido a que una instrucción equivale a
                                 1 palabra, es decir, a 4 bytes, por lo que la dirección de la próxima instrucción a ejecutar estará a 4 bytes 
                                 de distancia. */
                                PC = PC + (cache[contadorFilas, ubicacion[2] * 4 + 3]*4);

                            }
                            break;
                        case "BNEZ":
                            /* Se verifica la condición del salto */
                            if (registros[cache[contadorFilas, ubicacion[2] * 4 + 1]] != 0)
                            {
                                /* Se multiplica la cantidad de instrucciones que debe retornarse por 4 debido a que una instrucción equivale a
                                 1 palabra, es decir, a 4 bytes, por lo que la dirección de la próxima instrucción a ejecutar estará a 4 bytes 
                                 de distancia. */
                                PC = PC + (cache[contadorFilas, ubicacion[2] * 4 + 3] * 4);

                            }
                            break;
                        case "JAL":
                            registros[31] = PC;
                            PC = PC + cache[contadorFilas, ubicacion[2] * 4 + 3];
                            break;
                        case "JR":
                            PC = registros[cache[contadorFilas, ubicacion[2] * 4 + 1]];
                            break;
                        case "FIN":

                            /* Se disminuye la cantidad de hilos activos en el procesador */
                            --hilosActivos;

                            /* Se guarda el valor del reloj porque ya se terminó de ejecutar el hilo. Se guarda el valor del reloj aumentado porque
                            en este punto el hilo principal aún no ha aumentado el valor del reloj. */
                            datosHilos[filaContextoActual, 3] = reloj+1;

                            /* Se indica que un hilo ya no está en ejecucion */
                            ejecucionHilos[filaContextoActual, 1] = 1;
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
            int numBloque = ubicacion[0] - 8;
            int direccionFisica = numBloque * 16;
       


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

        /* Método para incrementar el valor del reloj y la cantidad de ciclos */
        public void aumentarReloj_Ciclos()
        {
            //Se aumenta el reloj
            ++reloj;

            //La columna 3 del arreglo datosHilos[] simboliza la cantidad de ciclos, por lo que se aumenta dicha cantidad. 
            ++datosHilos[filaContextoActual, 1];
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

                while (contadorInstrucciones < quantum  && ejecucionHilos[filaContextoActual, 1] == 0)
                {
                    contadorInstrucciones++;
                    leerInstruccion();               
                }

                    //Se copia en el contexto los registros porque se acabó el quantum
                    for (contadorContexto = 0; contadorContexto < columnasContexto - 1; ++contadorContexto)
                    {
                        contexto[filaContextoActual, contadorContexto] = registros[contadorContexto];
                    }

                    
                    //Se copia en la última columna del contexto el PC a ejecutar posteriormente o -1 si ya el hilo se terminó de ejecutar
                    if (ejecucionHilos[filaContextoActual, 1] == 1)
                    {
                        contexto[filaContextoActual, contadorContexto] = -1;
                    }
                    else
                    {
                        contexto[filaContextoActual, contadorContexto] = PC;
                    }


                    //Se inicializa en 0 nuevamente el contador de instrucciones
                    contadorInstrucciones = 0;

                    //Se verifica si la fila actual del contexto es la última, pues en caso de serlo, el siguiente
                    //hilillo a ejecutar es el ubicado en la primer fila del contexto, sino se ejecuta el que se encuentra en la siguiente fila.
                    ++filaContextoActual;

                    if (filaContextoActual == filasContexto)
                    {
                        filaContextoActual = 0;
                        while (filaContextoActual < filasContexto && contexto[filaContextoActual, columnasContexto - 1] == -1)
                        {
                            ++filaContextoActual;
                        }

                        if (filaContextoActual < filasContexto)
                        {
                            PC = contexto[filaContextoActual, columnasContexto - 1];
                        }
                    }
                    else
                    {
                        int contador = 0;
                        while (contador < filasContexto && contexto[filaContextoActual, columnasContexto - 1] == -1)
                        {
                            if (filaContextoActual == filasContexto - 1)
                            {
                                filaContextoActual = 0;
                            }
                            else
                            {
                                ++filaContextoActual;
                            }

                            ++contador;

                        }


                        PC = contexto[filaContextoActual, columnasContexto - 1];
                    }

                    /* Se realiza solo cuando el procesador posee más de un hilo */
                    if (filaContextoActual < filasContexto)
                    {
                        //Se copia en los registros el contexto del hilo a ejecutar proximamente
                        for (contadorContexto = 0; contadorContexto < columnasContexto - 1; ++contadorContexto)
                        {
                            registros[contadorContexto] = contexto[filaContextoActual, contadorContexto];
                        }

                        /*Se verifica si es la primera vez que se ejecuta el hilo, pues en caso de serlo se debe guardar el valor actual del reloj */
                        if (ejecucionHilos[filaContextoActual, 0] == 0)
                        {
                            ejecucionHilos[filaContextoActual, 0] = 1;
                            datosHilos[filaContextoActual, 2] = reloj;
                        }
                    }

            }
            
            terminarEjecucion = true;
            barreraFinInstr.RemoveParticipant();
            barreraCambioReloj_Ciclo.RemoveParticipant();
        }
    }
}
