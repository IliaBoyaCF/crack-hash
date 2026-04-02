using Worker.Abstractions;
using Contracts.ManagerToWorker;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Worker.Service;

public class Executor : IExecutor
{

    private readonly ILogger<Executor> _logger;

    private long currentTaskSize = 1L;
    private long currentTaskComputedNumber = 1L;
    public float CurrentTaskProgress { get => (float)currentTaskComputedNumber / currentTaskSize; }

    public CrackHashManagerRequest? TaskBeingExecuted { get; private set; } = null;

    public Executor(ILogger<Executor> logger)
    {
        _logger = logger;
    }

    public Task<CrackHashWorkerResponse> Execute(CrackHashManagerRequest request)
    {

        TaskBeingExecuted = request;

        var generator = new ByteBruteforceGenerator(request.Alphabet);


        currentTaskComputedNumber = 0;
        currentTaskSize = (long)(Math.Pow(request.Alphabet.Length, request.MaxLength) / request.PartCount);

        var variations = generator.GenerateForWorker(request.MaxLength, request.PartNumber, request.PartCount);

        using var md5 = MD5.Create();

        byte[] sourceHashBytes = HashToBytes(request.Hash);

        List<string> answers = [];

        _logger.LogInformation($"Ready to bruteforce for request: {request.RequestId}, {request.PartNumber}/{request.PartCount}.");

        var startTime = DateTime.Now;

        foreach (var item in variations)
        {
            var bytes = item;
            byte[] hash = md5.ComputeHash(bytes);
            bool sameHash = CompareHash(hash, sourceHashBytes);
            if (sameHash)
            {
                var word = Encoding.UTF8.GetString(item);
                _logger.LogInformation($"Found matching word '{word}' for request: {request.RequestId}, {request.PartNumber}/{request.PartCount}.");
                answers.Add(word);
            }
            Interlocked.Add(ref currentTaskComputedNumber, 1L);
        }

        var endTime = DateTime.Now;

        _logger.LogInformation($"Completed bruteforce for request: {request.RequestId}, {request.PartNumber}/{request.PartCount} for {endTime - startTime}");

        var result = new CrackHashWorkerResponse
        {
            RequestId = request.RequestId,
            PartNumber = request.PartNumber,
            Answers = answers.ToArray()
        };

        TaskBeingExecuted = null;

        return Task.FromResult(result);
    }

    private byte[] HashToBytes(string hashString)
    {

        _logger.LogInformation("Hash string '{hashString}', length={hashStirng.Length}", hashString, hashString.Length);

        const int hashSize = 16;
        byte[] hashBytes = new byte[hashSize];

        var span = hashString.AsSpan();

        for (int i = 0; i < span.Length; i += 2)
        {
            byte b = byte.Parse(new string([span[i], span[i + 1]]), System.Globalization.NumberStyles.HexNumber);
            hashBytes[i / 2] = b;
        }
        
        return hashBytes;
    }

    private bool CompareHash(byte[] hash1, byte[] hash2)
    {
        return hash1.SequenceEqual(hash2);
    }


}
