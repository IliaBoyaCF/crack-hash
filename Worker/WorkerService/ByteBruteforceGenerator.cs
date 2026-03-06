using Kaos.Combinatorics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Worker.Service;

internal class ByteBruteforceGenerator
{
    private readonly byte[] _alphabet;

    public ByteBruteforceGenerator(string[] alphabet)
    {
        _alphabet = alphabet.SelectMany(s =>
        {
            if (s.Length > 1)
            {
                throw new ArgumentException("Characters with length more then 1 are not supported.");
            }
            var bytes = Encoding.UTF8.GetBytes(s);
            if (bytes.Length > 1)
            {
                throw new ArgumentException("Not a single byte in UTF-8");
            }
            return bytes;
        }).ToArray();
    }

    public IEnumerable<byte[]> GenerateForWorker(int maxLength, int workerNumber, int workerCount)
    {
        return GenerateForWorker(maxLength, workerNumber, workerCount, _alphabet);
    }

    public IEnumerable<byte[]> GenerateForWorker(int length, int workerNumber, int workerCount, byte[] alphabet)
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
                yield return row.Select(index => alphabet[index]).ToArray();
                givenWords++;
            }
            currLength++;
        }
    }
}
