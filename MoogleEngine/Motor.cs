namespace MoogleEngine;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


//Clase que es el núcleo del proyecto, realiza el proceso de la búsqueda de la consulta en los documentos y calcula los pesos de cada uno
public class Motor
{
	private List<Documento> Documentos = new List<Documento>();//lista de los documentos
	private List<string> ListadoPalabrasDocumentos = new List<string>();//lista de las palabras que aparecen en todos los documentos que se utilizará para buscar las sugerencias
	private string SugerenciaBusqueda = string.Empty;//sugerencia de la búsqueda

	//Constructor de la clase
	public Motor()
	{
		List<string> archivos;//listado de los documentos

		//inicializacion de los caminos

		string directorio = Directory.GetCurrentDirectory();//tomamos el directorio actual
		char separador = directorio.Contains('\\') ? '\\' : '/'; // separador para windows o para linux
		if (directorio.Length > 3)//si no es raiz del disco
			directorio = directorio.Substring(0, directorio.LastIndexOf(separador) + 1);//sacamos la subcadena hasta el ultimo \ para subir al directorio anterior
		Utiles.CaminoDocumentos = directorio + "Content";//inicializamos el camino de los documentos


		Utiles.PalabrasVacias = ListadoPalabrasVacias.CreaListado();//se crea la lista de palabras vacías
		archivos = Utiles.ListaArchivos(Utiles.CaminoDocumentos);//leemos los nombres de los archivos de los documentos
		for (int i = 0; i < archivos.Count(); i++)//a cada uno de ellos
			Documentos.Add(new Documento(archivos[i]));//lo adicionamos a los documentos
													   //adicionamos a la lista de palabras en los documentos las palabra que hay en cada uno de ellos
		foreach (var doc in Documentos)//para cada documento
			foreach (var palabra in doc.Terminos)//para cada palabra
				if (!ListadoPalabrasDocumentos.Contains(palabra))//si no esta ya en la lista
					ListadoPalabrasDocumentos.Add(palabra);//la adicionamos

	}

	//Calcula la distancia entre dos palabras según el algoritmo de Damerau-Levenshtein
	//es utilizado para buscar la sugerencia a una palabra que no aparezca por estar mal escrita en la consulta
	//codigo tomado de https://www.csharpstar.com/csharp-string-distance-algorithm/
	//Entrada:palabras que se quieren comparar
	//Salida:distancia entre las dos palabras
	private static int DistanciaDamerauLevenshtein(string s, string t)
	{
		var limites = new { alto = s.Length + 1, ancho = t.Length + 1 };//limites de la matriz que se va a crear

		int[,] matriz = new int[limites.alto, limites.ancho];//matriz para realizar los calculos

		//inicialización por defecto de la matriz
		for (int alto = 0; alto < limites.alto; alto++)
			matriz[alto, 0] = alto;
		for (int ancho = 0; ancho < limites.ancho; ancho++)
			matriz[0, ancho] = ancho;

		for (int alto = 1; alto < limites.alto; alto++)//para cada una de las filas de la matriz
		{
			for (int ancho = 1; ancho < limites.ancho; ancho++)//para cada una de las colunmas de la matriz
			{
				int costo = (s[alto - 1] == t[ancho - 1]) ? 0 : 1;//si coinciden las letras el costo es 0, si no 1
				int insercion = matriz[alto, ancho - 1] + 1;//calculala insercion de una letra
				int borrado = matriz[alto - 1, ancho] + 1;//calcula el borrado de una letra
				int substitucion = matriz[alto - 1, ancho - 1] + costo;//calcula la sustitucion de una letra

				int distancia = Math.Min(insercion, Math.Min(borrado, substitucion));//la distancia es el mínimo de los tres valores

				if (alto > 1 && ancho > 1 && s[alto - 1] == t[ancho - 2] && s[alto - 2] == t[ancho - 1])
					distancia = Math.Min(distancia, matriz[alto - 2, ancho - 2] + costo);

				matriz[alto, ancho] = distancia;//se almacena la distancia
			}
		}

		return matriz[limites.alto - 1, limites.ancho - 1];//se devuelve la distancia
	}

	//busca la sugerencia de una palabra que puede estar mal escrita en la consulta. Se toman las palabras que hay en los documentos como referencia
	//calculamos el porciento de similitud entre ellas y la que tenga mayor porciento será la sugerencia
	//Entrada:palabra a buscarle sugerencia y lista de las palabras que hay en los documentos
	//Salida: sugerencia a la palabra
	public static string BuscarSugerencia(string palabra, List<string> Palabras)
	{
		string resultado = string.Empty;//se inicia sin sugerencia a la palabra
		int distancia = int.MaxValue;//utilizada para guardad la distancia entre las palabras
		double tempporciento = 0;//temporal para utilizar en el calculo del porciento de similitud
		double porciento = 0;//porciento de similitud

		if ((palabra != null) && (Palabras != null) && (palabra.Length != 0) && (Palabras.Count != 0))
		{//si se cumplen las condiciones
			foreach (string temppalabra in Palabras)
			{//pala cada una de las palabras en la lista de palabras de los documenton
				distancia = DistanciaDamerauLevenshtein(palabra, temppalabra);//calculamos la distancia entre palabras
				tempporciento = (1.0 - ((double)distancia / (double)Math.Max(palabra.Length, temppalabra.Length)));//calculamos el porciento de similitud entre las dos palabras

				if (tempporciento > porciento)
				{//si el porciento de similitud es mayor al que teníamos
					porciento = tempporciento;//cambiamos el porciento de similitud
					resultado = temppalabra;//guardamos la palabra como resultado
				}
			}
		}
		return resultado;//palabra con mayor similitud a la incorrecta
	}




	//devuelve la sugerencia a la búsqueda
	public string Sugerencia
	{
		get { return this.SugerenciaBusqueda; }
	}

	//Calcula el peso de los documentos según modelo vectorial
	//Entrada: arreglo con la frecuencia de los terminos de la consulta y matriz con la frecuencia de los terminos de los documentos
	private double[] CalcularPesosDocumentos(int[] arrConsultaTFVector, int[,] arrDocumentosTFVector)
	{
		double[] ConTF_IDF = new double[arrConsultaTFVector.Length];
		double[] arrDocumentosIDFVector;//arreglo para el vector IDF de los documentos
		double[,] arrDocumentosTFIDFVector; ;//arreglo para la matriz TF-IDF 
		double[] arrSIMVector; ;//arreglo de los pesos de los documentos(vector similitud)
		int CantidadTerminos = arrConsultaTFVector.Length;//cantidad de terminos en la consulta
		int CantidadDocumentos = arrDocumentosTFVector.GetLength(0);//cantidad de documentos

		// inicializar arreglos
		arrSIMVector = new double[CantidadDocumentos];
		arrDocumentosIDFVector = new double[CantidadTerminos];
		arrDocumentosTFIDFVector = new double[CantidadDocumentos, CantidadTerminos];

		//calculo del vector IDF de los documentos
		for (int i = 0; i < CantidadTerminos; i++)
		{//para cada uno de los terminos de la consulta
			int frecuencia = 0;//inicializamos la frecuencia
			for (int j = 0; j < CantidadDocumentos; j++)
			{//para cada uno de los documentos
				if (arrDocumentosTFVector[j, i] > 0)//si el termino aparece en el documento
					frecuencia++;//icrementamos la frecuencia

			}
			arrDocumentosIDFVector[i] = Math.Log10((double)(CantidadDocumentos) / (double)frecuencia) +1.1; //calculamos el valor de IDF del termino en el documento 
			
		}
		//Calculo de la matriz TF-IDF	
		for (int i = 0; i < CantidadDocumentos; i++)
		{//para cada documento
			for (int j = 0; j < CantidadTerminos; j++)
			{ //para cada uno de los terminos
				arrDocumentosTFIDFVector[i, j] = (0.5+ (double)arrDocumentosTFVector[i, j]/2) * arrDocumentosIDFVector[j];//calculamos el valor de TF-IDF
				ConTF_IDF[j] = arrConsultaTFVector[j] * arrDocumentosIDFVector[j];
			}
		}
		//calculo de la similitud de los documentos con la consulta que son los pesos de cada documento
		double numerator;//valores temporales
		double denominator;
		double querydenominator;
		for (int i = 0; i < CantidadDocumentos; i++)
		{//para cada documento
			numerator = 0;//inicializamos a 0 los temporales
			denominator = 0;
			querydenominator = 0;
			for (int j = 0; j < CantidadTerminos; j++)
			{//para cada uno de los terminos
				numerator += ((float)arrDocumentosTFIDFVector[i, j] * (float)arrConsultaTFVector[j]);//sumamos valores
				denominator += Math.Pow((double)arrDocumentosTFIDFVector[i, j], 2);
				querydenominator += Math.Pow((double)ConTF_IDF[j], 2);
			}
			arrSIMVector[i] = numerator / (Math.Sqrt(denominator) * Math.Sqrt((double)querydenominator));//calculamos el peso del documento
		}


		return arrSIMVector;//devolvemos los pesos o score de los documentos
	}
	//Realoza la consulta sobre los documentos
	//Entrada: texto de la consulta
	//Salida: resultados de la consulta
	public SearchItem[] RealizarConsulta(string consulta)
	{
		Consulta ConsultaRealizada = new Consulta(consulta);//se crea objeto para la consulta
		List<Documento> DocumentosConsulta = new List<Documento>();//lista donde se almacenaran los documentos sobres los que se realizara la consulta
		bool[] documentonecesario = new bool[Documentos.Count];//arreglo para definir si un documento es necesario o no para la consulta
		string[] fraseconsulta;//arreglo donde se almacenaran los terminos de la consulta para buscarlos en el texto
		string palabra = string.Empty;
		string lema = string.Empty;
		List<string> listasinonimos = new List<string>();
		SearchItem[] resultado;//resultado a devolver


		SugerenciaBusqueda = "";//inicializamos la sugerencia de la búsqueda vacía

		fraseconsulta = new string[ConsultaRealizada.Terminos.Count];//inicializamos el arreglo
																	 //los copiamos para el arreglo
		int k = 0;
		foreach (var termino in ConsultaRealizada.Terminos)
		{
			fraseconsulta[k] = termino.Key;
			k++;
		}

		//del total de documentos, buscamos los documentos que incluiremos o no en la consulta
		//los que tengan al menos un termnio serán verdaderos, los que no tengan ninguno serán falsos
		//si un documento no tiene ninguno de los terminos y pasamos a calcular su peso, crearía columnas vacias en la
		//matriz del modelo que traería una división por 0
		for (int i = 0; i < Documentos.Count; i++)//para cada documento
			documentonecesario[i] = Documentos[i].ExistenTerminos(fraseconsulta);//vemos si existe al menos un término o no ha ninguno
																				 //procesamos lor terminos prohibidos que no deben aparecer en los documentos y los obligatorios que deben aparecer en todos
		for (int i = 0; i < Documentos.Count; i++)
		{//para cada uno de los documentos
			foreach (string termino in ConsultaRealizada.TerminosProhibidos)//para cada uno de los términos prohibidos
				if (Documentos[i].ExisteTermino(termino))//existe el termino en el documento
					documentonecesario[i] = false;//no se buscara en este documento

			foreach (string termino in ConsultaRealizada.TerminosObligatorios)//para cada uno de los términos obligatorios
				if (!Documentos[i].ExisteTermino(termino))//no existe el término en el documento
					documentonecesario[i] = false;//no se buscara en este documento
		}
		//creamos la lista de los documentos sobre los que se realizará la consulta
		for (int i = 0; i < documentonecesario.Length; i++)//para todos los documentos
			if (documentonecesario[i])//si aparece como verdadero
				DocumentosConsulta.Add(Documentos[i]);//se adiciona a la lista

		//creamos el vector TF de la consulta. la cantidad de elementos serán la cantidad de terminos de la consulta y para cada par de términos que se quiera saber la distancia se le agrega un elememnto
		int[] vectorconsulta = new int[ConsultaRealizada.Terminos.Count() + ConsultaRealizada.TerminosCercanos.Count()];//cantidad de términos consulta + cantidad de par de términos por distancia
																														//creamos matriz TF de los documentos, que va a tener una fila por cada documento y una columna por cada elemento del vector TF de la consulta
		int[,] vectoresdocumentos = new int[DocumentosConsulta.Count, ConsultaRealizada.Terminos.Count() + ConsultaRealizada.TerminosCercanos.Count()];


		//damos valores a los elementos del vector TF de la consulta
		k = 0;
		foreach (var termino in ConsultaRealizada.Terminos)
		{//para cada uno de los términos de la consulta
			vectorconsulta[k] = termino.Value;//ponemos la cantidad de veces que aparece el término
			k++;
		}
		foreach (var termino in ConsultaRealizada.TerminosCercanos)
		{//para cada uno de los pares de términos de la consulta
			vectorconsulta[k] = 1;//ponemos la distancia entre ellos en la consulta a 1
			k++;
		}

		//damos valores a los elementos de la matriz TF de los documentos
		int j = 0;
		foreach (var doc in DocumentosConsulta)
		{//para cada uno de los documentos
			k = 0;
			foreach (var termino in ConsultaRealizada.Terminos)
			{//para cada uno de los terminos de la consulta
				vectoresdocumentos[j, k] = doc.CuentaTermino(termino.Key);//ponemos la cantidad de veces que aparece el término en el documento
				k++;
			}
			foreach (var termino in ConsultaRealizada.TerminosCercanos)
			{//para cada uno de los pares de términos de la consulta
				vectoresdocumentos[j, k] = doc.DistanciaMinTerminos(termino.X, termino.Y);//ponemos la distancia mínima entre ellos en el documento
				k++;
			}
			j++;
		}

		//Relizaremos la revisión de la matriz TF de los documentos antes de pasarla al calculo de los pesos
		//si hay alguna columna en cero significa que el término no se encontró en ningún documento
		//esto haría una división por 0 que invalidaría el cálculo.
		//los pasos srrian los siguiente:
		//paso 1: ver que colunmas estan en cero y si hay alguna buscamos si la palabra esta mal escrita y nos da el sistema una sugerencia
		//calculamos la frecuencia del nuevo termino en los documentos
		//paso 2: ver que colunmas estan en cero y si hay alguna buscamos el lema de la palabra
		//calculamos la frecuencia del lema en los documentos y multiplicamos por 2 la cantidad de veces para que sea menor el peso
		//paso 3: ver que colunmas estan en cero y si hay alguna buscamos los sinónimos de la palabra
		//calculamos la frecuencia de los sinónimos en los documentos y multiplicamos por 3 la cantidad de veces para que sea menor el peso
		//paso 4: ver que colunmas estan en cero y si hay alguna la eliminados

		//valores temporales para saber en que paso esta cada termino
		int[] estadotermino = new int[ConsultaRealizada.Terminos.Count()];

		for (k = 0; k < estadotermino.Length; k++)//para cada elemento
			estadotermino[k] = 0;   //lo iniciamos en 0

		int sumacolumna = 0;    //variable para guarda la suma de la columna		
		for (k = 0; k < ConsultaRealizada.Terminos.Count; k++)
		{//para cada termino
			sumacolumna = 0;
			for (j = 0; j < vectoresdocumentos.GetLength(0); j++)
			{//sumamos los valor de la columna
				sumacolumna += vectoresdocumentos[j, k];
			}
			if (sumacolumna == 0) //si la suma es 0, lo marcamos para paso 1
				estadotermino[k] = 1;
		}

		//paso 1
		for (int i = 0; i < estadotermino.Length; i++)
		{//para cada termino
			if (estadotermino[i] == 1)
			{//si esta marcado como paso 1
				palabra = BuscarSugerencia(fraseconsulta[i], ListadoPalabrasDocumentos);//buscamos sugerencia de la palabra
				if (palabra.Length > 0)
				{//si hay sugerencia
					sumacolumna = 0;//inicializamos la suma
					j = 0;
					foreach (var doc in DocumentosConsulta)
					{//para cada documento
						vectoresdocumentos[j, i] = doc.CuentaTermino(palabra);//ponemos las veces que aparece 
						sumacolumna += vectoresdocumentos[j, i];//calculamos la suma de la columna
						j++;
					}
					if (sumacolumna == 0)//si la suma de la colunma sigue siendo 0
						estadotermino[i] = 2;//lo marcamos como paso 2
					else if (SugerenciaBusqueda.Length == 0)//guardamos la sugerencia para ser mostrada con el resultado
						SugerenciaBusqueda = palabra;
					else
						SugerenciaBusqueda = SugerenciaBusqueda + ", " + palabra;//si hay mas de una sugerencia las concatenamos
				}
				else
					estadotermino[i] = 2;// si no hay sugerencia lo marcamos para el paso 2	
			}

		}

		//paso 2

		for (int i = 0; i < estadotermino.Length; i++)
		{//para cada termino
			if (estadotermino[i] == 2)
			{//si esta marcado como paso 2
				lema = Lematizador.Lematizar(fraseconsulta[i]); //sacamos la raiz de la palabra

				if (lema.Length > 0)
				{//si hay raiz
					sumacolumna = 0;//inicializamos la suma
					j = 0;
					foreach (var doc in DocumentosConsulta)
					{//para cada documento
						vectoresdocumentos[j, i] = (doc.CuentaLema(lema) * 2);//ponemos las veces que aparece multiplicada por dos para que el peso sea menor que la palabra
						sumacolumna = sumacolumna + vectoresdocumentos[j, i];//calculamos la suma de la columna
						j++;
					}
					if (sumacolumna == 0)   //si la suma de la colunma sigue siendo 0
						estadotermino[i] = 3;//lo marcamos como paso 3
				}
				else
					estadotermino[i] = 3;//lo marcamos como paso 3
			}

		}

		//paso 3

		List<int> columnasaborrar = new List<int>();//inicializamos la lista de las columnas que vamos a borrar

		for (int i = 0; i < estadotermino.Length; i++)//para cada termino
			if (estadotermino[i] == 4)//si esta marcado como paso 3
				columnasaborrar.Add(i);//adicionamos la columna a borrar

		if (columnasaborrar.Count > 0)
		{//si hay columnas a borrar
			vectoresdocumentos = Utiles.EliminaColumnasMatriz(vectoresdocumentos, columnasaborrar);//borramos en el vector TF de la consulta estos elementos
			vectorconsulta = Utiles.EliminaElementosArreglo(vectorconsulta, columnasaborrar);//borramos en la matriz TF de los documentos estas columnas
		}

		double[] pesos = CalcularPesosDocumentos(vectorconsulta, vectoresdocumentos);//calculamos los pesos de los documentos

		SearchItem[] items = new SearchItem[DocumentosConsulta.Count];//inicializamos los resultados

		for (int i = 0; i < DocumentosConsulta.Count; i++)
		{//para cada documento
			string oracionconsulta = DocumentosConsulta[i].ExtraerOracion(fraseconsulta);//buscamos la oración donde aparezcan los terminos
																						 //marcamos las palabras en las oraciones
			foreach (string palabraamarcar in fraseconsulta)//para cada termino
				oracionconsulta = oracionconsulta.Replace(palabraamarcar, "«" + palabraamarcar + "»");//lo marcamos

			items[i] = new SearchItem(DocumentosConsulta[i].Nombre.Substring(DocumentosConsulta[i].Nombre.LastIndexOf("\\") + 1), oracionconsulta, (float)pesos[i]);//damos valores al item del resultado
		}
		resultado = items.OrderByDescending(x => x.Score).ToArray();//organizamos de mayor a menor los resultados
		return resultado;//retornamos los resultados

	}

}

