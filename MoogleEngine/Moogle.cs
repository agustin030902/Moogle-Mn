namespace MoogleEngine;


public static class Moogle
{   public static Motor NucleoBuscador;//núcleo buscador
    static Moogle()
	{
		NucleoBuscador = new Motor();//inicializamos el núcleo
	}   

    public static SearchResult Query(string query) {
        
        return new SearchResult( NucleoBuscador.RealizarConsulta(query), NucleoBuscador.Sugerencia);//realizamos la consulta y devolvemos resultados y sugerencia
    }
}