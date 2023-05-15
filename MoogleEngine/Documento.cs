namespace MoogleEngine;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

//Clase Documento
//Esta clase es la encargada de gestionar todo lo relativo a los documentos sobre los que se realizaran las consultas
public class Documento
{	private string strCamino;//Camino en disco del documento
	private string strTexto;//Contenido completo del texto del documento
 	private List<string> lTerminos = new List<string>();//Lista de terminos que componen el documento.
	private List<string> lLemas = new List<string>();//Lista de lemas de los terminos que componen el documento.
 	
 	//Constructor de la clase
 	//Entrada: Camino en disco del documento
 	public Documento(string camino)
	{   this.strCamino = string.Empty; 
        this.strTexto = String.Empty; 

		if(camino.Length > 0){//Si camino no es vacio
			this.strCamino = camino; //Se guarda el camino del archivo
       		CargarArchivo();//Se carga el archivo del disco
			Procesar();	//Se procesa el archivo
		}
	}
 
 	public string Nombre
	{
		get{return strCamino;}
	}
 
 	public string Contenido
	{
		get{return strTexto;}
	}
 
 	public List<string> Terminos
	{
		get{return this.lTerminos;}
	}

	//Devuelve la lista de lemas que componen el documento.
	public List<string> Lemas
	{
		get { return this.lLemas; }
	}

	//Devuelve una lista de las posiciones donde aparece un termino en el documento
	//Entrada: termino del que se quiere saber las posiciones
	public List<int> PosicionesTermino(string termino)
	{	List<int> resultado = new();//Se inicializa la lista vacía
	 	
	 	for (int i=0; i<lTerminos.Count;i++)
			if( lTerminos[i] == termino)
				resultado.Add(i);
			
	 	return resultado;
	}
 
 	//Devuelve la cantidad de veces que aparece un termino en el documento
 	//Entrada: termino del que se quiere saber las veces que aparece
 	public int CuentaTermino(string termino)
 	{	
		return PosicionesTermino(termino).Count;
 	}
 
 	//Devuelve si existe un termino en el documento
 	//Entrada: termino del que se quiere saber si existe
	public bool ExisteTermino(string termino)
 	{		 
	 	return (CuentaTermino(termino)>0);
 	}
 	
 	//Devuelve si existe alguno de los terminos en el documento
 	//Entrada: arreglo de terminos que se quiere saber si alguno existe
	public bool ExistenTerminos(string[] terminos)
 	{
		foreach (string termino in terminos)
			if (CuentaTermino(termino) > 0) 
				return true;
	 	return false;
 	}
 	
 	//Devuelve la distancia mínima entre dos terminos en el documento
 	//Entrada: dos terminos de los que se quiere saber la distancia mínima entre ellos en el documento
 	public int DistanciaMinTerminos(string termino1, string termino2)
	{	int resultado = lTerminos.Count;
		
		List<int> ltermino1 = PosicionesTermino(termino1);
		List<int> ltermino2 = PosicionesTermino(termino2);
	 	if ((ltermino1.Count>0) && (ltermino2.Count>0)){//si los dos terminos existen, tienen como minimo una posición
			resultado = Math.Abs(ltermino1[0]-ltermino2[0]);//se inicializa la distancia minima como la distancia entre las primeras posiciones de los terminos
			for(int i=0; i<ltermino1.Count;i++)//se recorren todas las posiciones del primer termino
				for(int j=0; j<ltermino2.Count;j++)	//se recorren todas las posiciones del segundo termino
					if (Math.Abs(ltermino1[i]-ltermino2[j]) < resultado )//si la distancia es menor que teníamos
						resultado = Math.Abs(ltermino1[i]-ltermino2[j]);//guardamos la nueva distancia
		}
		return resultado;
	}
 	
 
	//Devuelve una lista de las posiciones donde aparece un lema en el documento
 	//Entrada: lema del que se quiere saber las posiciones
 	public List<int> PosicionesLema(string lema)
	{	List<int> resultado = new List<int>();//Se inicializa la lista vacía
	 	
	 	for (int i=0; i<lLemas.Count;i++)//Se recorren todos los lemas
			if( lLemas[i] == lema)//Si coincide con el lema del documento
				resultado.Add(i);//Se adiciona a la lista la posición del lema
			
	 	return resultado;
	}
 
 	//Devuelve la cantidad de veces que aparece un lema en el documento
 	//Entrada: lema del que se quiere saber las veces que aparece
 	public int CuentaLema(string lema)
 	{	
		return PosicionesLema(lema).Count;
 	}
 
 	
 	//Extraer del texto del documento una oración con la mayor cantidad de palabras posibles
 	//Entrada: arreglo de palabras a buscar
	public string ExtraerOracion(string[] palabras)
 	{	
	 	string[] oraciones = strTexto.Split(new char[] { '.', '¿', '?', '!', '¡' });//dividimos el texto por oraciones, teniendo en cuenta los caracteres que delimitan las mismas
			
        string[] resultado = Utiles.BuscarMayorCantidadPalabrasenTexto(palabras, oraciones);//buscamos las oraciones donde aparezcan la mayor cantidad de palabras

        return resultado[0];
 	}
 
 	//Realiza la carga del texto del documento desde el disco
 	//Salida: verdadero si lo cargo de forma satisfactoria, falso en caso contrario
 	private bool CargarArchivo()
	{	bool resultado = false;//resultado falso por defecto, carga insatisfactoria
	 	
	 	try
        {
	 		if (File.Exists(this.strCamino))//si existe el documento
        	{
	 			this.strTexto = File.ReadAllText(this.strCamino);//cargo a memoria el texto
	 			resultado = true;//carga satisfactoria
			}
	 					
		}
        catch (Exception e)//capturamos la excepción
        {
            Console.WriteLine("Error: {0}", e.ToString());//mostramos el error
        }
        finally {}
	 	return resultado;//retormanos resultado de la carga
	}
 
 	//Procesar el documento
	private void Procesar()
	{   List<string> palabras = new List<string>();//lista temporal que va a contener las palabras del texto
		string temppalabra;//temporal para conteer las palabras del texto
		
	 	palabras = Utiles.SeparaPalabras(strTexto);//crear lista con las palabras del texto
	 	foreach( string palabra in palabras ){//para cada una de las palabras
			if (!Utiles.EsPalabraVacia(palabra)){//si no es palabra vacía
				temppalabra = Utiles.NormalizarPalabra(palabra);//se normaliza la palabra
				lTerminos.Add(temppalabra);//se adiciona a los terminos
				lLemas.Add(Lematizador.Lematizar(temppalabra));//se lematiza la palabras y se adiciona a los lemas
			}
		}

	} 	
}
