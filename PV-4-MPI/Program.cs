/*
 * Создать приложение с тремя потоками. 
 * Первый поток заполняет матрицу А, случайным образом, 
 * параллельно второй поток заполняет матрицу В, случайным образом. 
 * Матрицы А и В квадратные и имеют одинаковую размерность.
 * Третий поток находит произведение матриц АВ.
 * Потоки прекращают работу, если определитель АВ равен нулю, иначе все операции повторяются.
 */

using System.Diagnostics;

Stopwatch stopwatch = new Stopwatch();

int N = 2;

int[,] A = new int[N, N]; //двумерный массив А
int[,] B = new int[N, N]; //двумерный массив B
int[,] AB = new int[N, N]; //произведение А и В

int d = 0;
Random rnd = new Random();

int iters = 0;
stopwatch.Start();
MPI.Environment.Run(ref args, communicator =>
{
	do
	{
		iters++;
		switch (communicator.Rank)
		{
			case 0:
				fillArray(N, N, A, rnd);
				communicator.Send<int[,]>(A, 2, (int)Channel.A);
				//printArray(N, N, A, index);
				break;

			case 1:
				fillArray(N, N, B, rnd);
				communicator.Send<int[,]>(B, 2, (int)Channel.B);
				//printArray(N, N, B, index);
				break;
			default:
				break;
		}

		if (communicator.Rank == 2)
		{
			A = communicator.Receive<int[,]>(0, (int)Channel.A);
			B = communicator.Receive<int[,]>(1, (int)Channel.B);

			//printArray(N, N, A, 21);
			//printArray(N, N, B, 22);

			multiplyArray(N, N, AB, A, B);
			//printArray(N, N, AB);
			d = getDeterminant(AB, N);

			communicator.Send<int>(d, 0, (int)Channel.d);
			communicator.Send<int>(d, 1, (int)Channel.d);

			//Console.WriteLine($"d = {d} ({index})");
		} else
        {
			d = communicator.Receive<int>(2, 2);
		}

	} while (d != 0);
	stopwatch.Stop();

	if (communicator.Rank == 2)
    {
		var elapsed = $"Прошло {stopwatch.ElapsedMilliseconds} мс, количество итераций {iters}";
		printArray(N, N, A);
		printArray(N, N, B);
		printArray(N, N, AB);

		Console.WriteLine("Определитель матрицы = " + d);
		Console.WriteLine(elapsed);
	}
});

void fillArray(int row, int column, int[,] array, Random rnd)
{
	for (int i = 0; i < row; i++)   //строки массива
		for (int j = 0; j < column; j++)   //столбцы массива
			array[i,j] = rnd.Next(-100, 101);  //заполняем текущую ячейку
}

void printArray(int row, int column, int[,] array, int? rank = null)
{
	var sb = new System.Text.StringBuilder();
	for (int i = 0; i < row; ++i)
	{  // Выводим на экран строку i
		for (int j = 0; j < column; ++j)
			sb.Append($"{array[i, j]} ");
		sb.Append("\n");
	}
	sb.Append(rank != null ? $"({rank})\n" : "\n");
    Console.WriteLine(sb.ToString());
}

void clearArray(int row, int column, int[,] array)
{
	for (int i = 0; i < row; ++i)
		for (int j = 0; j < column; ++j)
			array[i,j] = 0;
}

void multiplyArray(int row, int column, int[,] AB, int[,] A, int[,] B)
{
	clearArray(row, column, AB);
	for (int rowA = 0; rowA < row; rowA++)
	{
		for (int columnB = 0; columnB < column; columnB++)
		{
			// Умножение строки А на столбец В
			for (int i = 0; i < column; i++)
			{
				AB[rowA,columnB] += A[rowA,i] * B[i,columnB];
			}
		}
	}
}

// Получение матрицы без i-й строки и j-го столбца
void getMatr(int[,] mas, int[,] p, int i, int j, int m)
{
	int ki, kj, di, dj;
	di = 0;
	for (ki = 0; ki < m - 1; ki++)
	{ // проверка индекса строки
		if (ki == i) di = 1;
		dj = 0;
		for (kj = 0; kj < m - 1; kj++)
		{ // проверка индекса столбца
			if (kj == j) dj = 1;
			p[ki,kj] = mas[ki + di,kj + dj];
		}
	}
}

int getDeterminant(int[,] mas, int m)
{
	int i, j, d, k, n;
	int[,] p = new int[N,N];
	for (i = 0; i < m; i++)
		j = 0; d = 0;
	k = 1; //(-1) в степени i
	n = m - 1;
	if (m < 1)
	{
		Console.WriteLine("Определитель вычислить невозможно!\n");
	}
	if (m == 1)
	{
		d = mas[0,0];
		return (d);
	}
	if (m == 2)
	{
		d = mas[0,0] * mas[1,1] - (mas[1,0] * mas[0,1]);
		return (d);
	}
	if (m > 2)
	{
		for (i = 0; i < m; i++)
		{
			getMatr(mas, p, i, 0, m);
			d = d + k * mas[i,0] * getDeterminant(p, n);
			k = -k;
		}
	}
	return (d);
}

enum Channel
{
	A,
	B,
	d
}