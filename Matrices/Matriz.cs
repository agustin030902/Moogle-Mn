namespace Matrices;

// Clase matriz del proyecto donde ademas se puede calcular el peso de los documentos
public class Matriz
{
    private double [,] matriz_result;
    private double [,] matriz1;
    private double [,] matriz2;
    private double k; // escalar con el cual se podrian realizar operaciones entre matrices.

    public Matriz(double[,] matriz1,double[,] matriz2) // Construcctor para hacer operaciones con dos matrices 
    {
        this.matriz1 = matriz1;
        this.matriz2 = matriz2;
    }

    public Matriz(double[,] matriz1,double k)// Constructor de la clase para realizar operaciones con una matriz y un escalar 
    {
        this.matriz1 = matriz1;
        this.k = k;
    }

    //Devuelve la matriz resultante de realizar las operaciones
    public double [,] Matriz_result 
    {
        get { return this.matriz_result; }    
        set { this.matriz_result = value;}
    }

    public double[,] Sum(double[,] matriz1, double[,] matriz2)// metodo q calcula la suma de 2 matrices asumiendo q tienen misma dimension (m*n)
    {
        matriz_result= new double[matriz1.GetLength(0),matriz1.GetLength(1)];// se le da dimension a la matriz resultante
        for (int i = 0; i < matriz_result.GetLength(0); i++) // por cada una de sus filas 
        {
            for (int j = 0; j < matriz2.GetLength(0); j++)//por cada una de sus columnas
            {
                matriz_result[i,j] = matriz1[i,j] + matriz2[i,j];// se suma cada matriz en sus respectivas coodenadas (i,j) y se asignan a la misma posicion(i,j) de la matriz resultante
            }
        }
        return matriz_result;
    }
    public double[,] Multiplicar (double[,] matriz1,double[,] matriz2) // metodo q calcula la multiplicacion de 2 matrices asumiendo (m*n) dim de m1 tiene sus columnas (n) igual a su cantidad de filas q m2
    {
        matriz_result= new double[matriz1.GetLength(0),matriz2.GetLength(1)];
        for (int i = 0; i < matriz_result.GetLength(0) - 1; i++)
        {
            for (int j = 0; j < matriz_result.GetLength(1) - 1; j++)
            {
                for (int k = 0; k < matriz_result.GetLength(1); k++)
                {
                    matriz_result[i, j] += matriz1[i, k] * matriz2[k, j];
                }
            }
        }
        return matriz_result;
    }
    public double [,] Multiplicar_K (double[,] matriz1,double k)
    {
        for(int i =0;i< matriz1.GetLength(0);i++)
        {
            for(int j =0;j< matriz1.GetLength(1);j++)
            {
                matriz1[i,j] *= k;
            }
        }
        matriz_result = matriz1;
        return matriz_result;
    }

}