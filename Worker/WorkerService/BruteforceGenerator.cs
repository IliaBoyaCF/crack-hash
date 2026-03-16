using Kaos.Combinatorics;

namespace Worker.Service;

internal class BruteforceGenerator
{
    private readonly string[] _alphabet;

    public BruteforceGenerator(string[] alphabet)
    {
        _alphabet = alphabet;
    }

    public IEnumerable<string> GenerateForWorker(int maxLength, int workerNumber, int workerCount)
    {
        return GenerateForWorker(maxLength, workerNumber, workerCount, _alphabet);
    }

    public IEnumerable<string> GenerateForWorker(int length, int workerNumber, int workerCount, string[] alphabet)
    {
        int currLength = 1;
        while (currLength <= length)
        {
            int[] sizes = new int[currLength];
            for (int i = 0; i < currLength; i++)
            {
                sizes[i] = alphabet.Length;
            }
            long partSize = (long)Math.Pow(alphabet.Length, currLength) / workerCount + 1;
            var product = new Product(sizes, rank: partSize * (workerNumber - 1));
            long givenWords = 0;
            foreach (var row in product.GetRows())
            {
                if (givenWords >= partSize)
                {
                    break;
                }
                yield return string.Join("", row.Select(index => alphabet[index]).ToArray());
                givenWords++;
            }
            currLength++;
        }
    }
}
