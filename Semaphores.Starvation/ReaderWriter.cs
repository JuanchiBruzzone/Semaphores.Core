namespace Semaphore.Core;

public class ReaderWriter
{
    private static readonly SemaphoreSlim _resourceSemaphore = new(1, 1);
    private static readonly SemaphoreSlim _readerSemaphore = new(1, 1);
    private static int _readerCount = 0;
    private static readonly int[] _vector = new int[1000];
    private static int _lecturas = 0;
    private static int _escrituras = 0;

    static async Task Main()
    {

        while (true)
        {
            Console.WriteLine("Presione ENTER para comenzar. ");
            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                break;
            }
        }

        Console.WriteLine("Presiona Enter para habilitar el token de cancelacion y salir del programa. ");
        Console.WriteLine();

        Setup();

        CancellationTokenSource cts = new();

        var tasks = new Task[20];

        for (int i = 0; i < 10; i++)
        {
            int id = i;
            tasks[i] = Task.Run(() => Write(id, cts.Token));
            tasks[i+10] = Task.Run(() => Read(id, cts.Token));
        }

        if (Console.ReadKey().Key == ConsoleKey.Enter)
        {
            cts.Cancel();
            cts.Dispose();
        }

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Todas las operaciones fueron canceladas. ");
            Console.WriteLine();
            Console.WriteLine($"Lecturas = {_lecturas}, Escrituras = {_escrituras}");
        }
    }

    public static void Shuffle<T>(Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (array[k], array[n]) = (array[n], array[k]);
        }
    }

    static void Setup()
    {
        for (int i = 0; i < _vector.Length; i++) 
        {
            _vector[i] = new Random().Next(1, int.MaxValue);
        }
        var rng = new Random();
        for (int i = 0; i < 99; i++)
        {
            Shuffle(rng, _vector);
        }
    }

    static async Task Read(int readerId, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            await _readerSemaphore.WaitAsync(ct);
            _readerCount++;
            if (_readerCount == 1)
                await _resourceSemaphore.WaitAsync(ct);
            _readerSemaphore.Release();

            Console.WriteLine($"Lector {readerId} esta leyendo. ");

            while (true)
            {
                var rng = new Random().Next(1, int.MaxValue);

                if (_vector.Contains(rng))
                {
                    Console.WriteLine($"El lector {readerId} encontro el numero {rng} en el vector. ");
                    _lecturas++;
                    break;
                }
            }

            Console.WriteLine($"Lector {readerId} termino de leer. ");

            await _readerSemaphore.WaitAsync(ct);
            _readerCount--;
            if (_readerCount == 0)
                _resourceSemaphore.Release();
            _readerSemaphore.Release();
        }  
    }

    static async Task Write(int writerId, CancellationToken ct)
    {
        while (true)
        {
            ct.ThrowIfCancellationRequested();

            await _resourceSemaphore.WaitAsync(ct);
            Console.WriteLine($"Escritor {writerId} esta escribiendo. ");

            var rng = new Random().Next(1, _vector.Length - 1);
            var num = new Random().Next(1, int.MaxValue);
            _vector[rng] = num;
            _escrituras++;
            Console.WriteLine($"El escritor {writerId} escribio el numero {num} en la posicion del vector {rng}. ");

            Console.WriteLine($"Escritor {writerId} termino de escribir. ");
            _resourceSemaphore.Release();
        }    
    }
}